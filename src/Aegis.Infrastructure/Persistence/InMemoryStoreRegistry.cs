using Aegis.Application.Interfaces;
using Aegis.Application.Contracts;
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
        private readonly Dictionary<string, InMemoryIdempotencyRecord> _idempotencyRecords = new(StringComparer.Ordinal);
        private readonly object _authorizationModelsGate = new();

        public Task<StoreDto> CreateAsync(string name, CancellationToken cancellationToken = default)
        {
            return CreateForTenantAsync(name, name, cancellationToken);
        }

        public Task<StoreDto> CreateForTenantAsync(string tenantId, string name, CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var id = NewUlidLikeId();
            var store = new StoreDto(id, name, now, now, null, null, tenantId);
            _stores[id] = store;
            return Task.FromResult(store);
        }

        public Task<IReadOnlyList<StoreDto>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<StoreDto>>(_stores.Values.OrderByDescending(x => x.CreatedAt).ToList());
        }

        public Task<IReadOnlyList<StoreDto>> ListForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<StoreDto> stores = _stores.Values
                .Where(x => string.Equals(x.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
            return Task.FromResult(stores);
        }

        public Task<StoreDto?> GetAsync(string storeId, CancellationToken cancellationToken = default)
        {
            _stores.TryGetValue(storeId, out var store);
            return Task.FromResult(store);
        }

        public Task<StoreDto?> GetForTenantAsync(string tenantId, string storeId, CancellationToken cancellationToken = default)
        {
            _stores.TryGetValue(storeId, out var store);
            if (store is not null && !string.Equals(store.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
            {
                store = null;
            }

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

        public async Task<bool> DeleteForTenantAsync(string tenantId, string storeId, CancellationToken cancellationToken = default)
        {
            var store = await GetForTenantAsync(tenantId, storeId, cancellationToken);
            return store is not null && await DeleteAsync(storeId, cancellationToken);
        }

        Task IStoreRepository.AddAsync(Store store, CancellationToken cancellationToken)
        {
            _stores[store.Id] = new StoreDto(store.Id, store.Name, store.CreatedAt, store.UpdatedAt, null, null, store.Id);
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
                .OrderByDescending(x => x.State == AuthorizationModelLifecycleStates.Published)
                .ThenByDescending(x => x.PublishedAt ?? x.CreatedAt)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            return Task.FromResult(latest);
        }

        public Task<AuthorizationModelDto?> GetPublishedAsync(string storeId, CancellationToken cancellationToken = default)
        {
            var published = _models.Values
                .Where(x => x.StoreId == storeId && x.State == AuthorizationModelLifecycleStates.Published)
                .OrderByDescending(x => x.PublishedAt ?? x.CreatedAt)
                .FirstOrDefault();

            return Task.FromResult(published);
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
                State = current.State == AuthorizationModelLifecycleStates.Published
                    ? AuthorizationModelLifecycleStates.Deprecated
                    : AuthorizationModelLifecycleStates.Draft,
                PublishedAt = current.State == AuthorizationModelLifecycleStates.Published ? current.PublishedAt : null,
                ArchivedAt = null,
                SupersededBy = null,
                Revision = current.Revision + 1,
            };

            _models[key] = updated;
            return Task.FromResult<AuthorizationModelDto?>(updated);
        }

        public Task<AuthorizationModelDto?> UpdateStateAsync(
            string storeId,
            string authorizationModelId,
            string state,
            DateTimeOffset? publishedAt,
            DateTimeOffset? archivedAt,
            string? supersededBy,
            CancellationToken cancellationToken = default)
        {
            var key = $"{storeId}:{authorizationModelId}";
            if (!_models.TryGetValue(key, out var current))
            {
                return Task.FromResult<AuthorizationModelDto?>(null);
            }

            var updated = current with
            {
                State = state,
                PublishedAt = publishedAt,
                ArchivedAt = archivedAt,
                SupersededBy = supersededBy,
                Revision = current.Revision + 1,
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
                authorizationModel.CreatedAt,
                authorizationModel.State,
                authorizationModel.PublishedAt,
                authorizationModel.ArchivedAt,
                authorizationModel.SupersededBy,
                authorizationModel.Revision);

            _models[$"{authorizationModel.StoreId}:{authorizationModel.Id}"] = dto;
            return Task.CompletedTask;
        }

        Task<IdempotentAuthorizationModelAddResult> IAuthorizationModelRepository.AddIdempotentAsync(
            AuthorizationModel authorizationModel,
            IdempotentMutation mutation,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scopeKey = $"{mutation.TenantId}\n{mutation.ActorId}\n{authorizationModel.StoreId}\n{mutation.Operation}\n{mutation.Key}";
            lock (_authorizationModelsGate)
            {
                if (_idempotencyRecords.TryGetValue(scopeKey, out var existing))
                {
                    if (existing.ExpiresAt <= DateTimeOffset.UtcNow)
                    {
                        _idempotencyRecords.Remove(scopeKey);
                    }
                    else
                    {
                        if (!string.Equals(existing.RequestFingerprint, mutation.RequestFingerprint, StringComparison.Ordinal))
                        {
                            throw new IdempotencyConflictException("Idempotency-Key was already used with a different request payload.");
                        }

                        return Task.FromResult(new IdempotentAuthorizationModelAddResult(ToAggregate(existing.Response), false));
                    }
                }

                var dto = new AuthorizationModelDto(
                    authorizationModel.Id,
                    authorizationModel.StoreId,
                    authorizationModel.SchemaVersion,
                    authorizationModel.Model,
                    authorizationModel.CreatedAt,
                    authorizationModel.State,
                    authorizationModel.PublishedAt,
                    authorizationModel.ArchivedAt,
                    authorizationModel.SupersededBy,
                    authorizationModel.Revision);
                _models[$"{authorizationModel.StoreId}:{authorizationModel.Id}"] = dto;
                _idempotencyRecords[scopeKey] = new InMemoryIdempotencyRecord(
                    mutation.RequestFingerprint,
                    dto,
                    mutation.ExpiresAt);
                return Task.FromResult(new IdempotentAuthorizationModelAddResult(authorizationModel, true));
            }
        }

        Task<IReadOnlyList<AuthorizationModel>> IAuthorizationModelRepository.ListByStoreAsync(string storeId, CancellationToken cancellationToken)
        {
            IReadOnlyList<AuthorizationModel> models = _models.Values
                .Where(x => x.StoreId == storeId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(ToAggregate)
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
                : ToAggregate(latest);

            return Task.FromResult(model);
        }

        Task<AuthorizationModel?> IAuthorizationModelRepository.GetPublishedByStoreAsync(string storeId, CancellationToken cancellationToken)
        {
            return GetPublishedAsync(storeId, cancellationToken)
                .ContinueWith(task => task.Result is null ? null : ToAggregate(task.Result), cancellationToken);
        }

        Task<AuthorizationModel?> IAuthorizationModelRepository.GetByIdAsync(string storeId, string authorizationModelId, CancellationToken cancellationToken)
        {
            _models.TryGetValue($"{storeId}:{authorizationModelId}", out var dto);
            var model = dto is null
                ? null
                : ToAggregate(dto);

            return Task.FromResult(model);
        }

        Task<AuthorizationModel?> IAuthorizationModelRepository.UpdateAsync(AuthorizationModel authorizationModel, long expectedRevision, CancellationToken cancellationToken)
        {
            var key = $"{authorizationModel.StoreId}:{authorizationModel.Id}";
            if (!_models.TryGetValue(key, out var current) || current.Revision != expectedRevision)
            {
                return Task.FromResult<AuthorizationModel?>(null);
            }

            var updated = current with
            {
                SchemaVersion = authorizationModel.SchemaVersion,
                Model = authorizationModel.Model,
                State = authorizationModel.State,
                PublishedAt = authorizationModel.PublishedAt,
                ArchivedAt = authorizationModel.ArchivedAt,
                SupersededBy = authorizationModel.SupersededBy,
                Revision = current.Revision + 1,
            };

            return Task.FromResult<AuthorizationModel?>(
                _models.TryUpdate(key, updated, current) ? ToAggregate(updated) : null);
        }

        Task<IReadOnlyList<AuthorizationModel>> IAuthorizationModelRepository.PublishAsync(
            string storeId,
            string authorizationModelId,
            long expectedRevision,
            CancellationToken cancellationToken)
        {
            lock (_authorizationModelsGate)
            {
                var targetKey = $"{storeId}:{authorizationModelId}";
                if (!_models.TryGetValue(targetKey, out var target) || target.Revision != expectedRevision)
                {
                    return Task.FromResult<IReadOnlyList<AuthorizationModel>>([]);
                }

                var now = DateTimeOffset.UtcNow;
                var updated = new List<AuthorizationModel>();
                foreach (var item in _models.Values.Where(x => x.StoreId == storeId).ToList())
                {
                    var key = $"{item.StoreId}:{item.Id}";
                    AuthorizationModelDto next;
                    if (item.Id.Equals(authorizationModelId, StringComparison.OrdinalIgnoreCase))
                    {
                        next = item with
                        {
                            State = AuthorizationModelLifecycleStates.Published,
                            PublishedAt = now,
                            ArchivedAt = null,
                            SupersededBy = null,
                            Revision = item.Revision + 1,
                        };
                    }
                    else if (item.State == AuthorizationModelLifecycleStates.Published)
                    {
                        next = item with
                        {
                            State = AuthorizationModelLifecycleStates.Archived,
                            ArchivedAt = now,
                            SupersededBy = authorizationModelId,
                            Revision = item.Revision + 1,
                        };
                    }
                    else
                    {
                        continue;
                    }

                    _models[key] = next;
                    updated.Add(ToAggregate(next));
                }

                return Task.FromResult<IReadOnlyList<AuthorizationModel>>(updated);
            }
        }

        async Task<AuthorizationModel?> IAuthorizationModelRepository.RollbackAsync(
            string storeId,
            string authorizationModelId,
            long expectedRevision,
            CancellationToken cancellationToken)
        {
            var updated = await ((IAuthorizationModelRepository)this)
                .PublishAsync(storeId, authorizationModelId, expectedRevision, cancellationToken);
            return updated.FirstOrDefault(x => x.Id.Equals(authorizationModelId, StringComparison.OrdinalIgnoreCase));
        }

        Task<bool> IAuthorizationModelRepository.DeleteAsync(AuthorizationModel authorizationModel, long expectedRevision, CancellationToken cancellationToken)
        {
            var key = $"{authorizationModel.StoreId}:{authorizationModel.Id}";
            if (!_models.TryGetValue(key, out var current) || current.Revision != expectedRevision)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(_models.TryRemove(new KeyValuePair<string, AuthorizationModelDto>(key, current)));
        }

        private static string NewUlidLikeId()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("+", "A").Replace("/", "B");
        }

        private static AuthorizationModel ToAggregate(AuthorizationModelDto dto)
        {
            return AuthorizationModel.Rehydrate(
                dto.Id,
                dto.StoreId,
                dto.SchemaVersion,
                dto.Model,
                dto.CreatedAt,
                dto.State,
                dto.PublishedAt,
                dto.ArchivedAt,
                dto.SupersededBy,
                dto.Revision);
        }

        private sealed record InMemoryIdempotencyRecord(
            string RequestFingerprint,
            AuthorizationModelDto Response,
            DateTimeOffset ExpiresAt);
    }
}
