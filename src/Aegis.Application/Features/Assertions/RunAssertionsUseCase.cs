using Aegis.Application.Features.Permissions;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Aegis.Contracts.Permissions;

namespace Aegis.Application.Features.Assertions;

public sealed class RunAssertionsUseCase
{
    private readonly IStoreRegistry _storeRegistry;
    private readonly IAuthorizationModelRegistry _authorizationModelRegistry;
    private readonly IAssertionRepository _assertionRepository;
    private readonly CheckPermissionUseCase _checkPermissionUseCase;
    private readonly IAssertionRunStore _assertionRunStore;

    public RunAssertionsUseCase(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRegistry authorizationModelRegistry,
        IAssertionRepository assertionRepository,
        CheckPermissionUseCase checkPermissionUseCase,
        IAssertionRunStore assertionRunStore)
    {
        _storeRegistry = storeRegistry;
        _authorizationModelRegistry = authorizationModelRegistry;
        _assertionRepository = assertionRepository;
        _checkPermissionUseCase = checkPermissionUseCase;
        _assertionRunStore = assertionRunStore;
    }

    public async Task<AegisAssertionRunRecordDto> ExecuteAsync(
        string storeId,
        string authorizationModelId,
        CancellationToken cancellationToken = default)
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

        var model = await _authorizationModelRegistry.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
        if (model is null)
        {
            throw new CompatibilityApiException(
                400,
                "authorization_model_not_found",
                $"Authorization Model '{authorizationModelId}' not found");
        }

        var assertionSet = await _assertionRepository.ReadAsync(storeId, authorizationModelId, cancellationToken);
        var tenantId = string.IsNullOrWhiteSpace(store.TenantId) ? storeId : store.TenantId;
        var startedAt = DateTimeOffset.UtcNow;
        var results = new List<AegisAssertionRunResultItemDto>(assertionSet.Assertions.Count);

        foreach (var assertion in assertionSet.Assertions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await _checkPermissionUseCase.ExecuteAsync(
                tenantId,
                new CheckRequestDto(
                    assertion.TupleKey.User,
                    assertion.TupleKey.Relation,
                    assertion.TupleKey.Object,
                    assertion.ContextualTuples?.TupleKeys
                        .Select(tuple => new ContextualTupleDto(tuple.User, tuple.Relation, tuple.Object))
                        .ToList(),
                    null,
                    model.Id),
                includeTrace: true,
                cancellationToken,
                storeId);

            results.Add(new AegisAssertionRunResultItemDto(
                assertion.TupleKey,
                assertion.Expectation,
                response.Allowed,
                response.Allowed == assertion.Expectation,
                response.Decision,
                response.ReasonCode,
                response.Trace is { Count: > 0 } ? NewUlidLikeId() : null));
        }

        var record = new AegisAssertionRunRecordDto(
            NewUlidLikeId(),
            storeId,
            authorizationModelId,
            startedAt,
            DateTimeOffset.UtcNow,
            new AegisAssertionRunSummaryDto(
                results.Count,
                results.Count(result => result.Passed),
                results.Count(result => !result.Passed)),
            results);
        await _assertionRunStore.SaveAsync(record, cancellationToken);
        return record;
    }

    private static string NewUlidLikeId()
        => Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("=", string.Empty)
            .Replace("+", "A")
            .Replace("/", "B");
}
