using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;

namespace Aegis.Application.Features.Assertions;

public sealed class GenerateAssertionsFromAuditUseCase
{
    private readonly IStoreRegistry _storeRegistry;
    private readonly IAuthorizationModelRegistry _authorizationModelRegistry;
    private readonly IAssertionRepository _assertionRepository;
    private readonly IAuditStore _auditStore;
    private readonly AssertionValidator _assertionValidator;

    public GenerateAssertionsFromAuditUseCase(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRegistry authorizationModelRegistry,
        IAssertionRepository assertionRepository,
        IAuditStore auditStore,
        AssertionValidator assertionValidator)
    {
        _storeRegistry = storeRegistry;
        _authorizationModelRegistry = authorizationModelRegistry;
        _assertionRepository = assertionRepository;
        _auditStore = auditStore;
        _assertionValidator = assertionValidator;
    }

    public async Task<AegisGenerateAssertionsFromAuditResponseDto> ExecuteAsync(
        string storeId,
        string authorizationModelId,
        AegisGenerateAssertionsFromAuditRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(storeId))
        {
            throw new CompatibilityApiException(400, "validation_error", "store_id is required.");
        }

        var store = await _storeRegistry.GetAsync(storeId, cancellationToken);
        if (store is null)
        {
            throw new CompatibilityApiException(404, "store_id_not_found", "Store ID not found.");
        }

        var model = await _authorizationModelRegistry.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
        if (model is null)
        {
            throw new CompatibilityApiException(
                400,
                "authorization_model_not_found",
                $"Authorization Model '{authorizationModelId}' not found");
        }

        var limit = request.Limit ?? 25;
        if (limit <= 0 || limit > WriteAssertionsUseCase.MaximumAssertionsPerModel)
        {
            throw new CompatibilityApiException(
                400,
                "validation_error",
                $"limit must be between 1 and {WriteAssertionsUseCase.MaximumAssertionsPerModel}.");
        }

        var decision = NormalizeAuditDecision(request.Decision);
        var tenantId = string.IsNullOrWhiteSpace(store.TenantId) ? storeId : store.TenantId;
        var events = await _auditStore.QueryAsync(tenantId, action: null, decision, storeId, cancellationToken);
        var relationIndex = _assertionValidator.BuildRelationIndex(model.Model);
        var assertions = events
            .Where(IsCheckAuditEvent)
            .OrderByDescending(auditEvent => auditEvent.CreatedAt)
            .Select(ToAssertion)
            .Where(assertion => _assertionValidator.IsValid(assertion, relationIndex))
            .GroupBy(
                assertion => $"{assertion.TupleKey.User}:{assertion.TupleKey.Relation}:{assertion.TupleKey.Object}:{assertion.Expectation}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
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
                    WriteAssertionsUseCase.MaximumAssertionsPerModel,
                    cancellationToken);
            }
            catch (AssertionSetCapacityExceededException exception)
            {
                throw new CompatibilityApiException(400, "assertions_too_many_items", exception.Message);
            }
        }

        return new AegisGenerateAssertionsFromAuditResponseDto(
            authorizationModelId,
            assertions.Count,
            request.Append,
            assertions);
    }

    private static bool IsCheckAuditEvent(AuditEvent auditEvent)
        => auditEvent.Action.Equals("check", StringComparison.OrdinalIgnoreCase)
            || auditEvent.Action.Equals("explain", StringComparison.OrdinalIgnoreCase);

    private static AegisCompatAssertionDto ToAssertion(AuditEvent auditEvent)
        => new(
            new AegisCompatTupleKeyDto(auditEvent.Subject, auditEvent.Relation, auditEvent.Object),
            auditEvent.Decision.Equals("Allow", StringComparison.OrdinalIgnoreCase),
            ContextualTuples: null);

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
}
