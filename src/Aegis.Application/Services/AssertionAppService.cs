using Aegis.Application.Features.Assertions;
using Aegis.Application.Features.Permissions;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Aegis.Contracts.Permissions;

namespace Aegis.Application.Services
{
    public sealed class AssertionAppService : IAssertionAppService
    {
        private const int MaxAssertionsPerModel = WriteAssertionsUseCase.MaximumAssertionsPerModel;
        private readonly IStoreRegistry _storeRegistry;
        private readonly IAuthorizationModelRegistry _authorizationModelRegistry;
        private readonly CheckPermissionUseCase _checkPermissionUseCase;
        private readonly IAssertionRepository _assertionRepository;
        private readonly IAssertionRunStore _assertionRunStore;
        private readonly IAuditStore _auditStore;
        private readonly AssertionValidator _assertionValidator;

        public AssertionAppService(
            IStoreRegistry storeRegistry,
            IAuthorizationModelRegistry authorizationModelRegistry,
            CheckPermissionUseCase checkPermissionUseCase,
            IAssertionRepository assertionRepository,
            IAssertionRunStore assertionRunStore,
            IAuditStore auditStore,
            AssertionValidator assertionValidator)
        {
            _storeRegistry = storeRegistry;
            _authorizationModelRegistry = authorizationModelRegistry;
            _checkPermissionUseCase = checkPermissionUseCase;
            _assertionRepository = assertionRepository;
            _assertionRunStore = assertionRunStore;
            _auditStore = auditStore;
            _assertionValidator = assertionValidator;
        }

        public async Task<AegisCompatReadAssertionsResponseDto> ReadAsync(
            string storeId,
            string authorizationModelId,
            CancellationToken cancellationToken = default)
        {
            await EnsureStoreExists(storeId, cancellationToken);
            if (string.IsNullOrWhiteSpace(authorizationModelId))
            {
                throw new CompatibilityApiException(400, "validation_error", "authorization_model_id is required.");
            }

            await EnsureModelExists(storeId, authorizationModelId, cancellationToken);

            var snapshot = await _assertionRepository.ReadAsync(storeId, authorizationModelId, cancellationToken);
            return new AegisCompatReadAssertionsResponseDto(authorizationModelId, snapshot.Assertions);
        }

        public async Task<AegisAssertionRunRecordDto> RunAsync(
            string storeId,
            string authorizationModelId,
            CancellationToken cancellationToken = default)
        {
            var store = await EnsureStoreExists(storeId, cancellationToken);
            var model = await EnsureModelExists(storeId, authorizationModelId, cancellationToken);
            var tenantId = string.IsNullOrWhiteSpace(store.TenantId) ? storeId : store.TenantId;
            var assertionSet = await _assertionRepository.ReadAsync(storeId, authorizationModelId, cancellationToken);
            var startedAt = DateTimeOffset.UtcNow;
            var results = new List<AegisAssertionRunResultItemDto>();

            foreach (var assertion in assertionSet.Assertions)
            {
                var response = await _checkPermissionUseCase.ExecuteAsync(
                    tenantId,
                    new CheckRequestDto(
                        assertion.TupleKey.User,
                        assertion.TupleKey.Relation,
                        assertion.TupleKey.Object,
                        assertion.ContextualTuples?.TupleKeys.Select(tuple => new ContextualTupleDto(tuple.User, tuple.Relation, tuple.Object)).ToList(),
                        null,
                        model.Id),
                    includeTrace: true,
                    cancellationToken,
                    storeId);

                var passed = response.Allowed == assertion.Expectation;
                results.Add(new AegisAssertionRunResultItemDto(
                    assertion.TupleKey,
                    assertion.Expectation,
                    response.Allowed,
                    passed,
                    response.Decision,
                    response.ReasonCode,
                    response.Trace is { Count: > 0 } ? NewUlidLikeId() : null));
            }

            var summary = new AegisAssertionRunSummaryDto(results.Count, results.Count(x => x.Passed), results.Count(x => !x.Passed));
            var record = new AegisAssertionRunRecordDto(
                NewUlidLikeId(),
                storeId,
                authorizationModelId,
                startedAt,
                DateTimeOffset.UtcNow,
                summary,
                results);

            await _assertionRunStore.SaveAsync(record, cancellationToken);
            return record;
        }

        public async Task<AegisAssertionRunListResponseDto> ListRunsAsync(
            string storeId,
            string authorizationModelId,
            CancellationToken cancellationToken = default)
        {
            await EnsureStoreExists(storeId, cancellationToken);
            await EnsureModelExists(storeId, authorizationModelId, cancellationToken);
            var runs = await _assertionRunStore.ListByModelAsync(storeId, authorizationModelId, 25, cancellationToken);

            return new AegisAssertionRunListResponseDto(runs);
        }

        public async Task<AegisAssertionRunRecordDto?> GetRunAsync(
            string storeId,
            string runId,
            CancellationToken cancellationToken = default)
        {
            await EnsureStoreExists(storeId, cancellationToken);
            return await _assertionRunStore.GetAsync(storeId, runId, cancellationToken);
        }

        public async Task<AegisGenerateAssertionsFromAuditResponseDto> GenerateFromAuditAsync(
            string storeId,
            string authorizationModelId,
            AegisGenerateAssertionsFromAuditRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var store = await EnsureStoreExists(storeId, cancellationToken);
            var model = await EnsureModelExists(storeId, authorizationModelId, cancellationToken);

            var limit = request.Limit ?? 25;
            if (limit <= 0 || limit > MaxAssertionsPerModel)
            {
                throw new CompatibilityApiException(
                    400,
                    "validation_error",
                    $"limit must be between 1 and {MaxAssertionsPerModel}.");
            }

            var decision = NormalizeAuditDecision(request.Decision);
            var tenantId = string.IsNullOrWhiteSpace(store.TenantId) ? storeId : store.TenantId;
            var events = await _auditStore.QueryAsync(tenantId, action: null, decision, storeId, cancellationToken);
            var relationIndex = _assertionValidator.BuildRelationIndex(model.Model);
            var assertions = events
                .Where(IsCheckAuditEvent)
                .OrderByDescending(x => x.CreatedAt)
                .Select(ToAssertion)
                .Where(x => _assertionValidator.IsValid(x, relationIndex))
                .GroupBy(x => $"{x.TupleKey.User}:{x.TupleKey.Relation}:{x.TupleKey.Object}:{x.Expectation}", StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .Take(limit)
                .ToList();

            if (request.Append && assertions.Count > 0)
            {
                try
                {
                    await _assertionRepository.AppendDistinctAsync(
                        storeId,
                        authorizationModelId,
                        assertions,
                        MaxAssertionsPerModel,
                        cancellationToken);
                }
                catch (AssertionSetCapacityExceededException exception)
                {
                    throw new CompatibilityApiException(
                        400,
                        "assertions_too_many_items",
                        exception.Message);
                }
            }

            return new AegisGenerateAssertionsFromAuditResponseDto(
                authorizationModelId,
                assertions.Count,
                request.Append,
                assertions);
        }

        public async Task PurgeStoreAsync(string storeId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                throw new ArgumentException("storeId is required.");
            }

            await _assertionRepository.PurgeStoreAsync(storeId, cancellationToken);
            await _assertionRunStore.PurgeStoreAsync(storeId, cancellationToken);
        }

        private async Task<StoreDto> EnsureStoreExists(string storeId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                throw new CompatibilityApiException(400, "validation_error", "store_id is required.");
            }

            var store = await _storeRegistry.GetAsync(storeId, cancellationToken);
            if (store is null)
            {
                throw new CompatibilityApiException(404, "store_id_not_found", "Store ID not found.");
            }

            return store;
        }

        private async Task<AuthorizationModelDto> EnsureModelExists(string storeId, string authorizationModelId, CancellationToken cancellationToken)
        {
            var model = await _authorizationModelRegistry.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            if (model is null)
            {
                throw new CompatibilityApiException(
                    400,
                    "authorization_model_not_found",
                    $"Authorization Model '{authorizationModelId}' not found");
            }

            return model;
        }

        private static bool IsCheckAuditEvent(AuditEvent auditEvent)
        {
            return auditEvent.Action.Equals("check", StringComparison.OrdinalIgnoreCase)
                || auditEvent.Action.Equals("explain", StringComparison.OrdinalIgnoreCase);
        }

        private static AegisCompatAssertionDto ToAssertion(AuditEvent auditEvent)
        {
            return new AegisCompatAssertionDto(
                new AegisCompatTupleKeyDto(auditEvent.Subject, auditEvent.Relation, auditEvent.Object),
                auditEvent.Decision.Equals("Allow", StringComparison.OrdinalIgnoreCase),
                ContextualTuples: null);
        }

        private static string? NormalizeAuditDecision(string? decision)
        {
            if (string.IsNullOrWhiteSpace(decision))
            {
                return null;
            }

            if (decision.Equals("allow", StringComparison.OrdinalIgnoreCase)
                || decision.Equals("allowed", StringComparison.OrdinalIgnoreCase)
                || decision.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return "Allow";
            }

            if (decision.Equals("deny", StringComparison.OrdinalIgnoreCase)
                || decision.Equals("denied", StringComparison.OrdinalIgnoreCase)
                || decision.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return "Deny";
            }

            throw new CompatibilityApiException(400, "validation_error", "decision must be Allow or Deny.");
        }

        private static string NewUlidLikeId()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("+", "A").Replace("/", "B");
        }
    }
}
