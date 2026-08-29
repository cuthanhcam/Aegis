using Aegis.Application.Interfaces;
using Aegis.Infrastructure.Authorization;

namespace Aegis.Infrastructure.Persistence;

public sealed class InMemoryStoreDeletionRepository : IStoreDeletionRepository
{
    private readonly InMemoryStoreRegistry _storeRegistry;
    private readonly InMemoryRelationshipStore _relationshipStore;
    private readonly InMemoryRbacStore _rbacStore;
    private readonly InMemoryAssertionRepository _assertionRepository;
    private readonly InMemoryAssertionRunStore _assertionRunStore;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public InMemoryStoreDeletionRepository(
        InMemoryStoreRegistry storeRegistry,
        InMemoryRelationshipStore relationshipStore,
        InMemoryRbacStore rbacStore,
        InMemoryAssertionRepository assertionRepository,
        InMemoryAssertionRunStore assertionRunStore)
    {
        _storeRegistry = storeRegistry;
        _relationshipStore = relationshipStore;
        _rbacStore = rbacStore;
        _assertionRepository = assertionRepository;
        _assertionRunStore = assertionRunStore;
    }

    public async Task<bool> DeleteAsync(
        string tenantId,
        string storeId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (await _storeRegistry.GetForTenantAsync(tenantId, storeId, cancellationToken) is null)
            {
                return false;
            }

            await _assertionRepository.PurgeStoreAsync(storeId, cancellationToken);
            await _assertionRunStore.PurgeStoreAsync(storeId, cancellationToken);
            await _relationshipStore.PurgeStoreAsync(tenantId, storeId, cancellationToken);
            await _rbacStore.PurgeStoreAsync(tenantId, storeId, cancellationToken);
            return await _storeRegistry.DeleteForTenantAsync(tenantId, storeId, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}
