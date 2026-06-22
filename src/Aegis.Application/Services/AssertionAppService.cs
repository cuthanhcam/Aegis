using Aegis.Application.Features.Permissions;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Aegis.Contracts.Permissions;
using Aegis.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Aegis.Application.Services
{
    public sealed class AssertionAppService : IAssertionAppService
    {
        private static readonly ConcurrentDictionary<string, IReadOnlyList<AegisCompatAssertionDto>> AssertionsByModel = new(StringComparer.OrdinalIgnoreCase);
        private const int MaxAssertionsPerModel = 100;
        private readonly IStoreRegistry _storeRegistry;
        private readonly IAuthorizationModelRegistry _authorizationModelRegistry;
        private readonly CheckPermissionUseCase? _checkPermissionUseCase;
        private readonly IAssertionRunStore? _assertionRunStore;
        private readonly IAuditStore? _auditStore;

        public AssertionAppService(IStoreRegistry storeRegistry, IAuthorizationModelRegistry authorizationModelRegistry)
        {
            _storeRegistry = storeRegistry;
            _authorizationModelRegistry = authorizationModelRegistry;
        }

        [ActivatorUtilitiesConstructor]
        public AssertionAppService(
            IStoreRegistry storeRegistry,
            IAuthorizationModelRegistry authorizationModelRegistry,
            CheckPermissionUseCase checkPermissionUseCase,
            IAssertionRunStore assertionRunStore,
            IAuditStore? auditStore = null)
            : this(storeRegistry, authorizationModelRegistry)
        {
            _checkPermissionUseCase = checkPermissionUseCase;
            _assertionRunStore = assertionRunStore;
            _auditStore = auditStore;
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

            var key = BuildKey(storeId, authorizationModelId);
            AssertionsByModel.TryGetValue(key, out var assertions);
            return new AegisCompatReadAssertionsResponseDto(authorizationModelId, assertions ?? []);
        }

        public async Task WriteAsync(
            string storeId,
            string authorizationModelId,
            AegisCompatWriteAssertionsRequestDto request,
            CancellationToken cancellationToken = default)
        {
            await EnsureStoreExists(storeId, cancellationToken);
            if (string.IsNullOrWhiteSpace(authorizationModelId))
            {
                throw new CompatibilityApiException(400, "validation_error", "authorization_model_id is required.");
            }

            var model = await EnsureModelExists(storeId, authorizationModelId, cancellationToken);

            if (request.Assertions is null)
            {
                throw new CompatibilityApiException(400, "validation_error", "assertions are required.");
            }

            if (request.Assertions.Count > MaxAssertionsPerModel)
            {
                throw new CompatibilityApiException(
                    400,
                    "assertions_too_many_items",
                    $"assertions exceeds max allowed items of {MaxAssertionsPerModel}.");
            }

            var relationIndex = BuildRelationIndex(model.Model);

            foreach (var assertion in request.Assertions)
            {
                ValidateAssertion(assertion, relationIndex);
            }

            AssertionsByModel[BuildKey(storeId, authorizationModelId)] = request.Assertions.ToList();
        }

        public async Task<AegisAssertionRunRecordDto> RunAsync(
            string storeId,
            string authorizationModelId,
            CancellationToken cancellationToken = default)
        {
            await EnsureStoreExists(storeId, cancellationToken);
            if (_checkPermissionUseCase is null)
            {
                throw new InvalidOperationException("Assertion runner is unavailable because permission checks are not registered.");
            }

            var model = await EnsureModelExists(storeId, authorizationModelId, cancellationToken);
            AssertionsByModel.TryGetValue(BuildKey(storeId, authorizationModelId), out var assertions);
            assertions ??= [];
            var startedAt = DateTimeOffset.UtcNow;
            var results = new List<AegisAssertionRunResultItemDto>();

            foreach (var assertion in assertions)
            {
                var response = await _checkPermissionUseCase.ExecuteAsync(
                    storeId,
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

            if (_assertionRunStore is null)
            {
                throw new InvalidOperationException("Assertion run history is unavailable because an assertion run store is not registered.");
            }

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
            var runs = _assertionRunStore is null
                ? []
                : await _assertionRunStore.ListByModelAsync(storeId, authorizationModelId, 25, cancellationToken);

            return new AegisAssertionRunListResponseDto(runs);
        }

        public async Task<AegisAssertionRunRecordDto?> GetRunAsync(
            string storeId,
            string runId,
            CancellationToken cancellationToken = default)
        {
            await EnsureStoreExists(storeId, cancellationToken);
            return _assertionRunStore is null
                ? null
                : await _assertionRunStore.GetAsync(storeId, runId, cancellationToken);
        }

        public async Task<AegisGenerateAssertionsFromAuditResponseDto> GenerateFromAuditAsync(
            string storeId,
            string authorizationModelId,
            AegisGenerateAssertionsFromAuditRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var store = await EnsureStoreExists(storeId, cancellationToken);
            var model = await EnsureModelExists(storeId, authorizationModelId, cancellationToken);

            if (_auditStore is null)
            {
                throw new InvalidOperationException("Assertion generation is unavailable because an audit store is not registered.");
            }

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
            var relationIndex = BuildRelationIndex(model.Model);
            var assertions = events
                .Where(IsCheckAuditEvent)
                .OrderByDescending(x => x.CreatedAt)
                .Select(ToAssertion)
                .Where(x => IsValidGeneratedAssertion(x, relationIndex))
                .GroupBy(x => $"{x.TupleKey.User}:{x.TupleKey.Relation}:{x.TupleKey.Object}:{x.Expectation}", StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .Take(limit)
                .ToList();

            if (request.Append && assertions.Count > 0)
            {
                var existing = await ReadAsync(storeId, authorizationModelId, cancellationToken);
                var combined = existing.Assertions
                    .Concat(assertions)
                    .GroupBy(x => $"{x.TupleKey.User}:{x.TupleKey.Relation}:{x.TupleKey.Object}:{x.Expectation}", StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();

                await WriteAsync(storeId, authorizationModelId, new AegisCompatWriteAssertionsRequestDto(combined), cancellationToken);
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

            foreach (var key in AssertionsByModel.Keys
                         .Where(x => x.StartsWith($"{storeId}:", StringComparison.OrdinalIgnoreCase))
                         .ToArray())
            {
                AssertionsByModel.TryRemove(key, out _);
            }

            if (_assertionRunStore is not null)
            {
                await _assertionRunStore.PurgeStoreAsync(storeId, cancellationToken);
            }
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

        private static void ValidateAssertion(
            AegisCompatAssertionDto assertion,
            IReadOnlyDictionary<string, HashSet<string>> relationIndex)
        {
            if (!SubjectId.TryCreate(assertion.TupleKey.User, out _)
                || !RelationName.TryCreate(assertion.TupleKey.Relation, out _)
                || !ObjectId.TryCreate(assertion.TupleKey.Object, out _))
            {
                throw new CompatibilityApiException(400, "validation_error", "Invalid assertion tuple_key format.");
            }

            ValidateTypeAndRelation(assertion.TupleKey.Object, assertion.TupleKey.Relation, relationIndex);

            var contextual = assertion.ContextualTuples?.TupleKeys;
            if (contextual is null)
            {
                return;
            }

            foreach (var tuple in contextual)
            {
                if (!SubjectId.TryCreate(tuple.User, out _)
                    || !RelationName.TryCreate(tuple.Relation, out _)
                    || !ObjectId.TryCreate(tuple.Object, out _))
                {
                    throw new CompatibilityApiException(400, "validation_error", "Invalid assertion contextual tuple format.");
                }

                ValidateTypeAndRelation(tuple.Object, tuple.Relation, relationIndex);
            }
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

        private static IReadOnlyDictionary<string, HashSet<string>> BuildRelationIndex(string model)
        {
            var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var lines = model.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string? currentType = null;

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.StartsWith("type ", StringComparison.OrdinalIgnoreCase))
                {
                    currentType = line[5..].Trim();
                    if (!result.ContainsKey(currentType))
                    {
                        result[currentType] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    }

                    continue;
                }

                if (currentType is null || !line.StartsWith("define ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var def = line[7..];
                var separatorIndex = def.IndexOf(':');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                result[currentType].Add(def[..separatorIndex].Trim());
            }

            return result;
        }

        private static void ValidateTypeAndRelation(
            string objectRef,
            string relation,
            IReadOnlyDictionary<string, HashSet<string>> relationIndex)
        {
            var typeSeparator = objectRef.IndexOf(':');
            var typeName = typeSeparator > 0 ? objectRef[..typeSeparator] : objectRef;

            if (!relationIndex.TryGetValue(typeName, out var relations))
            {
                throw new CompatibilityApiException(400, "type_not_found", $"type '{typeName}' not found");
            }

            if (!relations.Contains(relation))
            {
                throw new CompatibilityApiException(
                    400,
                    "relation_not_found",
                    $"relation '{typeName}#{relation}' not found");
            }
        }

        private static string BuildKey(string storeId, string authorizationModelId)
            => $"{storeId}:{authorizationModelId}";

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

        private static bool IsValidGeneratedAssertion(
            AegisCompatAssertionDto assertion,
            IReadOnlyDictionary<string, HashSet<string>> relationIndex)
        {
            try
            {
                ValidateAssertion(assertion, relationIndex);
                return true;
            }
            catch (CompatibilityApiException)
            {
                return false;
            }
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
