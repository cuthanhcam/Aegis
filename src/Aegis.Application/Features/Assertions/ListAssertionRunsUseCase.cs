using Aegis.Application.Interfaces;
using Aegis.Contracts.Compatibility;

namespace Aegis.Application.Features.Assertions;

public sealed class ListAssertionRunsUseCase
{
    private const int DefaultHistoryLimit = 25;
    private readonly AssertionScopeGuard _scopeGuard;
    private readonly IAssertionRunStore _assertionRunStore;

    public ListAssertionRunsUseCase(AssertionScopeGuard scopeGuard, IAssertionRunStore assertionRunStore)
    {
        _scopeGuard = scopeGuard;
        _assertionRunStore = assertionRunStore;
    }

    public async Task<AegisAssertionRunListResponseDto> ExecuteAsync(
        string storeId,
        string authorizationModelId,
        CancellationToken cancellationToken = default)
    {
        await _scopeGuard.EnsureStoreAsync(storeId, cancellationToken);
        await _scopeGuard.EnsureModelAsync(storeId, authorizationModelId, cancellationToken);
        var runs = await _assertionRunStore.ListByModelAsync(
            storeId,
            authorizationModelId,
            DefaultHistoryLimit,
            cancellationToken);
        return new AegisAssertionRunListResponseDto(runs);
    }
}
