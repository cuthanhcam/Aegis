using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Domain.Entities;
using Aegis.Domain.Repositories;

namespace Aegis.Application.Services
{
    public sealed class StoreAppService : IStoreAppService
    {
        private readonly IStoreRegistry _storeRegistry;
        private readonly IStoreRepository _storeRepository;
        private readonly IStoreDeletionRepository _storeDeletionRepository;

        public StoreAppService(
            IStoreRegistry storeRegistry,
            IStoreRepository storeRepository,
            IStoreDeletionRepository storeDeletionRepository)
        {
            _storeRegistry = storeRegistry;
            _storeRepository = storeRepository;
            _storeDeletionRepository = storeDeletionRepository;
        }

        public Task<IReadOnlyList<StoreDto>> ListAsync(CancellationToken cancellationToken = default)
        {
            return _storeRegistry.ListAsync(cancellationToken);
        }

        public Task<IReadOnlyList<StoreDto>> ListAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return _storeRegistry.ListForTenantAsync(tenantId, cancellationToken);
            }

            return _storeRegistry.ListAsync(cancellationToken);
        }

        public async Task<StoreDto?> GetByIdAsync(string storeId, CancellationToken cancellationToken = default)
        {
            return await GetByIdAsync(string.Empty, storeId, cancellationToken);
        }

        public async Task<StoreDto?> GetByIdAsync(string tenantId, string storeId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                throw new ArgumentException("storeId is required.");
            }

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return await _storeRegistry.GetForTenantAsync(tenantId, storeId, cancellationToken);
            }

            var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken);
            return store is null ? null : ToDto(store);
        }

        public async Task<bool> DeleteAsync(string storeId, CancellationToken cancellationToken = default)
        {
            return await DeleteAsync(string.Empty, storeId, cancellationToken);
        }

        public async Task<bool> DeleteAsync(string tenantId, string storeId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                throw new ArgumentException("storeId is required.");
            }

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                var store = await _storeRegistry.GetForTenantAsync(tenantId, storeId, cancellationToken);
                if (store is null)
                {
                    return false;
                }

                return await _storeDeletionRepository.DeleteAsync(tenantId, storeId, cancellationToken);
            }

            var existing = await _storeRegistry.GetAsync(storeId, cancellationToken);
            if (existing is null)
            {
                return false;
            }

            return await _storeDeletionRepository.DeleteAsync(existing.TenantId ?? string.Empty, storeId, cancellationToken);
        }

        private static StoreDto ToDto(Store store)
        {
            return new StoreDto(store.Id, store.Name, store.CreatedAt, store.UpdatedAt, null, null);
        }

    }
}
