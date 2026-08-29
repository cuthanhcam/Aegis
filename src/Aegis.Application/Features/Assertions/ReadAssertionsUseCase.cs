using Aegis.Application.Interfaces;
using Aegis.Contracts.Compatibility;

namespace Aegis.Application.Features.Assertions;

public sealed class ReadAssertionsUseCase
{
    private readonly AssertionScopeGuard _scopeGuard;
    private readonly IAssertionRepository _assertionRepository;

    public ReadAssertionsUseCase(AssertionScopeGuard scopeGuard, IAssertionRepository assertionRepository)
    {
        _scopeGuard = scopeGuard;
        _assertionRepository = assertionRepository;
    }

    public async Task<AegisCompatReadAssertionsResponseDto> ExecuteAsync(
        string storeId,
        string authorizationModelId,
        CancellationToken cancellationToken = default)
    {
        await _scopeGuard.EnsureStoreAsync(storeId, cancellationToken);
        await _scopeGuard.EnsureModelAsync(storeId, authorizationModelId, cancellationToken);
        var snapshot = await _assertionRepository.ReadAsync(storeId, authorizationModelId, cancellationToken);
        return new AegisCompatReadAssertionsResponseDto(authorizationModelId, snapshot.Assertions);
    }
}
