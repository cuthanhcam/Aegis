using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Aegis.Domain.Entities;
using Aegis.Domain.Repositories;
using System.Collections.Concurrent;

namespace Aegis.Infrastructure.Persistence
{
    public sealed class InMemoryStoreRegistry : IStoreRegistry, IAuthorizationModelRegistry, IStoreRepository, IAuthorizationModelRepository
    {
        private readonly ConcurrentDictionary<string, StoreDto> _stores = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, AuthorizationModelDto> _models = new(StringComparer.OrdinalIgnoreCase);

        public Task<StoreDto> CreateAsync(string name, CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var id = NewUlidLikeId();
            var store = new StoreDto(id, name, now, now, null, null);
            _stores[id] = store;
            return Task.FromResult(store);
        }

        public Task<IReadOnlyList<StoreDto>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<StoreDto>>(_stores.Values.OrderByDescending(x => x.CreatedAt).ToList());
        }

        public Task<StoreDto?> GetAsync(string storeId, CancellationToken cancellationToken = default)
        {
            _stores.TryGetValue(storeId, out var store);
            return Task.FromResult(store);
        }

        public Task<bool> DeleteAsync(string storeId, CancellationToken cancellationToken = default)
        {
            var removed = _stores.TryRemove(storeId, out _);
            if (!removed)
            {
                return Task.FromResult(false);
            }

            foreach (var key in _models.Keys.Where(x => x.StartsWith($"{storeId}:", StringComparison.OrdinalIgnoreCase)).ToArray())
            {
                _models.TryRemove(key, out _);
            }

            return Task.FromResult(true);
        }

        Task IStoreRepository.AddAsync(Store store, CancellationToken cancellationToken)
        {
            _stores[store.Id] = new StoreDto(store.Id, store.Name, store.CreatedAt, store.UpdatedAt, null, null);
            return Task.CompletedTask;
        }

        Task<Store?> IStoreRepository.GetByIdAsync(string storeId, CancellationToken cancellationToken)
        {
            _stores.TryGetValue(storeId, out var dto);
            var store = dto is null ? null : Store.Rehydrate(dto.Id, dto.Name, dto.CreatedAt, dto.UpdatedAt);
            return Task.FromResult(store);
        }

        Task<IReadOnlyList<Store>> IStoreRepository.ListAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<Store> stores = _stores.Values
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => Store.Rehydrate(x.Id, x.Name, x.CreatedAt, x.UpdatedAt))
                .ToList();

            return Task.FromResult(stores);
        }

        Task<bool> IStoreRepository.DeleteAsync(Store store, CancellationToken cancellationToken)
        {
            return DeleteAsync(store.Id, cancellationToken);
        }

        public Task<AuthorizationModelDto> CreateAsync(string storeId, string schemaVersion, string model, CancellationToken cancellationToken = default)
        {
            var dto = new AuthorizationModelDto(NewUlidLikeId(), storeId, schemaVersion, model, DateTimeOffset.UtcNow);
            _models[$"{storeId}:{dto.Id}"] = dto;
            return Task.FromResult(dto);
        }

        public Task<IReadOnlyList<AuthorizationModelDto>> ListAsync(string storeId, CancellationToken cancellationToken = default)
        {
            var data = _models.Values
                .Where(x => x.StoreId == storeId)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<AuthorizationModelDto>>(data);
        }

        public Task<AuthorizationModelDto?> GetLatestAsync(string storeId, CancellationToken cancellationToken = default)
        {
            var latest = _models.Values
                .Where(x => x.StoreId == storeId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            return Task.FromResult(latest);
        }

        public Task<AuthorizationModelDto?> GetByIdAsync(string storeId, string authorizationModelId, CancellationToken cancellationToken = default)
        {
            _models.TryGetValue($"{storeId}:{authorizationModelId}", out var model);
            return Task.FromResult(model);
        }

        public Task<AuthorizationModelDto?> UpdateAsync(
            string storeId,
            string authorizationModelId,
            string schemaVersion,
            string model,
            CancellationToken cancellationToken = default)
        {
            var key = $"{storeId}:{authorizationModelId}";
            if (!_models.TryGetValue(key, out var current))
            {
                return Task.FromResult<AuthorizationModelDto?>(null);
            }

            var updated = current with
            {
                SchemaVersion = schemaVersion,
                Model = model,
            };

            _models[key] = updated;
            return Task.FromResult<AuthorizationModelDto?>(updated);
        }

        public Task<bool> DeleteAsync(string storeId, string authorizationModelId, CancellationToken cancellationToken = default)
        {
            var deleted = _models.TryRemove($"{storeId}:{authorizationModelId}", out _);
            return Task.FromResult(deleted);
        }

        Task IAuthorizationModelRepository.AddAsync(AuthorizationModel authorizationModel, CancellationToken cancellationToken)
        {
            var dto = new AuthorizationModelDto(
                authorizationModel.Id,
                authorizationModel.StoreId,
                authorizationModel.SchemaVersion,
                authorizationModel.Model,
                authorizationModel.CreatedAt);

            _models[$"{authorizationModel.StoreId}:{authorizationModel.Id}"] = dto;
            return Task.CompletedTask;
        }

        Task<IReadOnlyList<AuthorizationModel>> IAuthorizationModelRepository.ListByStoreAsync(string storeId, CancellationToken cancellationToken)
        {
            IReadOnlyList<AuthorizationModel> models = _models.Values
                .Where(x => x.StoreId == storeId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => AuthorizationModel.Rehydrate(x.Id, x.StoreId, x.SchemaVersion, x.Model, x.CreatedAt))
                .ToList();

            return Task.FromResult(models);
        }

        Task<AuthorizationModel?> IAuthorizationModelRepository.GetLatestByStoreAsync(string storeId, CancellationToken cancellationToken)
        {
            var latest = _models.Values
                .Where(x => x.StoreId == storeId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            var model = latest is null
                ? null
                : AuthorizationModel.Rehydrate(latest.Id, latest.StoreId, latest.SchemaVersion, latest.Model, latest.CreatedAt);

            return Task.FromResult(model);
        }

        Task<AuthorizationModel?> IAuthorizationModelRepository.GetByIdAsync(string storeId, string authorizationModelId, CancellationToken cancellationToken)
        {
            _models.TryGetValue($"{storeId}:{authorizationModelId}", out var dto);
            var model = dto is null
                ? null
                : AuthorizationModel.Rehydrate(dto.Id, dto.StoreId, dto.SchemaVersion, dto.Model, dto.CreatedAt);

            return Task.FromResult(model);
        }

        Task<AuthorizationModel?> IAuthorizationModelRepository.UpdateAsync(AuthorizationModel authorizationModel, CancellationToken cancellationToken)
        {
            var key = $"{authorizationModel.StoreId}:{authorizationModel.Id}";
            if (!_models.TryGetValue(key, out var current))
            {
                return Task.FromResult<AuthorizationModel?>(null);
            }

            var updated = current with
            {
                SchemaVersion = authorizationModel.SchemaVersion,
                Model = authorizationModel.Model,
            };

            _models[key] = updated;
            return Task.FromResult<AuthorizationModel?>(
                AuthorizationModel.Rehydrate(updated.Id, updated.StoreId, updated.SchemaVersion, updated.Model, updated.CreatedAt));
        }

        Task<bool> IAuthorizationModelRepository.DeleteAsync(AuthorizationModel authorizationModel, CancellationToken cancellationToken)
        {
            return DeleteAsync(authorizationModel.StoreId, authorizationModel.Id, cancellationToken);
        }

        private static string NewUlidLikeId()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("+", "A").Replace("/", "B");
        }
    }
}
