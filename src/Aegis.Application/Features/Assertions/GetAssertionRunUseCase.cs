using Aegis.Application.Interfaces;
using Aegis.Contracts.Compatibility;

namespace Aegis.Application.Features.Assertions;

public sealed class GetAssertionRunUseCase
{
    private readonly AssertionScopeGuard _scopeGuard;
    private readonly IAssertionRunStore _assertionRunStore;

    public GetAssertionRunUseCase(AssertionScopeGuard scopeGuard, IAssertionRunStore assertionRunStore)
    {
        _scopeGuard = scopeGuard;
        _assertionRunStore = assertionRunStore;
    }

    public async Task<AegisAssertionRunRecordDto?> ExecuteAsync(
        string storeId,
        string runId,
        CancellationToken cancellationToken = default)
    {
        await _scopeGuard.EnsureStoreAsync(storeId, cancellationToken);
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new CompatibilityApiException(400, "validation_error", "run_id is required.");
        }

        return await _assertionRunStore.GetAsync(storeId, runId, cancellationToken);
    }
}
