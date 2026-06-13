using Aegis.Application.DomainEvents;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Domain.Entities;
using Aegis.Domain.Repositories;

namespace Aegis.Application.Services
{
    public sealed class StoreAppService : IStoreAppService
    {
        private readonly IStoreRegistry _storeRegistry;
        private readonly IRelationshipStore _relationshipStore;
        private readonly IStoreRepository? _storeRepository;
        private readonly AssertionAppService? _assertionAppService;
        private readonly IDomainEventDispatcher? _domainEventDispatcher;

        public StoreAppService(IStoreRegistry storeRegistry, IRelationshipStore relationshipStore)
        {
            _storeRegistry = storeRegistry;
            _relationshipStore = relationshipStore;
            _storeRepository = storeRegistry as IStoreRepository;
            _domainEventDispatcher = null;
        }

        public StoreAppService(
            IStoreRegistry storeRegistry,
            IRelationshipStore relationshipStore,
            IStoreRepository storeRepository,
            AssertionAppService assertionAppService,
            IDomainEventDispatcher domainEventDispatcher)
            : this(storeRegistry, relationshipStore)
        {
            _storeRepository = storeRepository;
            _assertionAppService = assertionAppService;
            _domainEventDispatcher = domainEventDispatcher;
        }

        public Task<StoreDto> CreateAsync(CreateStoreRequestDto request, CancellationToken cancellationToken = default)
        {
            return CreateAsync(string.Empty, request, cancellationToken);
        }

        public Task<StoreDto> CreateAsync(string tenantId, CreateStoreRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    throw new ArgumentException("Store name is required.");
                }

                return _storeRegistry.CreateForTenantAsync(tenantId, request.Name, cancellationToken);
            }

            if (_storeRepository is null)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    throw new ArgumentException("Store name is required.");
                }

                return _storeRegistry.CreateAsync(request.Name, cancellationToken);
            }

            var store = Store.Create(request.Name);
            return CreateWithDomainAsync(store, cancellationToken);
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

            if (_storeRepository is not null)
            {
                var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken);
                return store is null ? null : ToDto(store);
            }

            return await _storeRegistry.GetAsync(storeId, cancellationToken);
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
                if (_assertionAppService is not null)
                {
                    await _assertionAppService.PurgeStoreAsync(storeId, cancellationToken);
                }

                return await _storeRegistry.DeleteForTenantAsync(tenantId, storeId, cancellationToken);
            }

            if (_assertionAppService is not null)
            {
                await _assertionAppService.PurgeStoreAsync(storeId, cancellationToken);
            }

            if (_storeRepository is not null)
            {
                var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken);
                if (store is null)
                {
                    return false;
                }

                store.MarkDeleted();
                var deleted = await _storeRepository.DeleteAsync(store, cancellationToken);
                if (deleted)
                {
                    await _domainEventDispatcher.DispatchAndClearAsync(store, cancellationToken);
                }

                return deleted;
            }

            return await _storeRegistry.DeleteAsync(storeId, cancellationToken);
        }

        private async Task<StoreDto> CreateWithDomainAsync(Store store, CancellationToken cancellationToken)
        {
            if (_storeRepository is null)
            {
                throw new InvalidOperationException("Domain store repository is not configured.");
            }

            await _storeRepository.AddAsync(store, cancellationToken);
            await _domainEventDispatcher.DispatchAndClearAsync(store, cancellationToken);
            return ToDto(store);
        }

        private static StoreDto ToDto(Store store)
        {
            return new StoreDto(store.Id, store.Name, store.CreatedAt, store.UpdatedAt, null, null);
        }

        private async Task<IReadOnlyList<StoreDto>> ListWithDomainAsync(CancellationToken cancellationToken)
        {
            if (_storeRepository is null)
            {
                throw new InvalidOperationException("Domain store repository is not configured.");
            }

            var stores = await _storeRepository.ListAsync(cancellationToken);
            return stores.Select(ToDto).ToList();
        }
    }
}
