using Aegis.Application.DomainEvents;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Domain.Entities;
using Aegis.Domain.Repositories;

namespace Aegis.Application.Services
{
    public sealed class AuthorizationModelAppService : IAuthorizationModelAppService
    {
        private readonly IStoreRegistry _storeRegistry;
        private readonly IAuthorizationModelRegistry _authorizationModelRegistry;
        private readonly IAuthorizationModelRepository? _authorizationModelRepository;
        private readonly IDomainEventDispatcher? _domainEventDispatcher;

        public AuthorizationModelAppService(IStoreRegistry storeRegistry, IAuthorizationModelRegistry authorizationModelRegistry)
        {
            _storeRegistry = storeRegistry;
            _authorizationModelRegistry = authorizationModelRegistry;
            _authorizationModelRepository = authorizationModelRegistry as IAuthorizationModelRepository;
            _domainEventDispatcher = null;
        }

        public AuthorizationModelAppService(
            IStoreRegistry storeRegistry,
            IAuthorizationModelRegistry authorizationModelRegistry,
            IAuthorizationModelRepository authorizationModelRepository,
            IDomainEventDispatcher domainEventDispatcher)
            : this(storeRegistry, authorizationModelRegistry)
        {
            _authorizationModelRepository = authorizationModelRepository;
            _domainEventDispatcher = domainEventDispatcher;
        }

        public async Task<AuthorizationModelDto> CreateAsync(
            string storeId,
            CreateAuthorizationModelRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (_authorizationModelRepository is null)
            {
                if (string.IsNullOrWhiteSpace(request.SchemaVersion) || string.IsNullOrWhiteSpace(request.Model))
                {
                    throw new ArgumentException("schemaVersion and model are required.");
                }

                await EnsureStoreExists(storeId, cancellationToken);
                return await _authorizationModelRegistry.CreateAsync(storeId, request.SchemaVersion, request.Model, cancellationToken);
            }

            await EnsureStoreExists(storeId, cancellationToken);
            var authorizationModel = AuthorizationModel.Create(storeId, request.SchemaVersion, request.Model);
            await _authorizationModelRepository.AddAsync(authorizationModel, cancellationToken);
            await _domainEventDispatcher.DispatchAndClearAsync(authorizationModel, cancellationToken);
            return ToDto(authorizationModel);
        }

        public async Task<IReadOnlyList<AuthorizationModelDto>> ListAsync(string storeId, CancellationToken cancellationToken = default)
        {
            await EnsureStoreExists(storeId, cancellationToken);

            if (_authorizationModelRepository is not null)
            {
                var authorizationModels = await _authorizationModelRepository.ListByStoreAsync(storeId, cancellationToken);
                return authorizationModels.Select(ToDto).ToList();
            }

            return await _authorizationModelRegistry.ListAsync(storeId, cancellationToken);
        }

        public async Task<AuthorizationModelDto?> GetLatestAsync(string storeId, CancellationToken cancellationToken = default)
        {
            await EnsureStoreExists(storeId, cancellationToken);

            if (_authorizationModelRepository is not null)
            {
                var authorizationModel = await _authorizationModelRepository.GetLatestByStoreAsync(storeId, cancellationToken);
                return authorizationModel is null ? null : ToDto(authorizationModel);
            }

            return await _authorizationModelRegistry.GetLatestAsync(storeId, cancellationToken);
        }

        public async Task<AuthorizationModelDto?> GetByIdAsync(
            string storeId,
            string authorizationModelId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(authorizationModelId))
            {
                throw new ArgumentException("authorizationModelId is required.");
            }

            await EnsureStoreExists(storeId, cancellationToken);

            if (_authorizationModelRepository is not null)
            {
                var authorizationModel = await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
                return authorizationModel is null ? null : ToDto(authorizationModel);
            }

            return await _authorizationModelRegistry.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
        }

        public async Task<AuthorizationModelDto?> UpdateAsync(
            string storeId,
            string authorizationModelId,
            CreateAuthorizationModelRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(authorizationModelId))
            {
                throw new ArgumentException("authorizationModelId is required.");
            }

            if (_authorizationModelRepository is null)
            {
                if (string.IsNullOrWhiteSpace(request.SchemaVersion) || string.IsNullOrWhiteSpace(request.Model))
                {
                    throw new ArgumentException("schemaVersion and model are required.");
                }

                await EnsureStoreExists(storeId, cancellationToken);
                return await _authorizationModelRegistry.UpdateAsync(storeId, authorizationModelId, request.SchemaVersion, request.Model, cancellationToken);
            }

            await EnsureStoreExists(storeId, cancellationToken);
            var existing = await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            if (existing is null)
            {
                return null;
            }

            existing.UpdateDefinition(request.SchemaVersion, request.Model);
            var updated = await _authorizationModelRepository.UpdateAsync(existing, cancellationToken);
            await _domainEventDispatcher.DispatchAndClearAsync(existing, cancellationToken);
            return updated is null ? null : ToDto(updated);
        }

        public async Task<bool> DeleteAsync(
            string storeId,
            string authorizationModelId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(authorizationModelId))
            {
                throw new ArgumentException("authorizationModelId is required.");
            }

            await EnsureStoreExists(storeId, cancellationToken);

            if (_authorizationModelRepository is not null)
            {
                var existing = await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
                if (existing is null)
                {
                    return false;
                }

                existing.MarkDeleted();
                var deleted = await _authorizationModelRepository.DeleteAsync(existing, cancellationToken);
                if (deleted)
                {
                    await _domainEventDispatcher.DispatchAndClearAsync(existing, cancellationToken);
                }

                return deleted;
            }

            return await _authorizationModelRegistry.DeleteAsync(storeId, authorizationModelId, cancellationToken);
        }

        private async Task EnsureStoreExists(string storeId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                throw new ArgumentException("storeId is required.");
            }

            var store = await _storeRegistry.GetAsync(storeId, cancellationToken);
            if (store is null)
            {
                throw new ArgumentException("Store not found.");
            }
        }

        private static AuthorizationModelDto ToDto(AuthorizationModel authorizationModel)
        {
            return new AuthorizationModelDto(
                authorizationModel.Id,
                authorizationModel.StoreId,
                authorizationModel.SchemaVersion,
                authorizationModel.Model,
                authorizationModel.CreatedAt);
        }
    }
}
