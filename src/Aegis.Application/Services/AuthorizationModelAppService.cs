using Aegis.Application.DomainEvents;
using Aegis.Application.Contracts;
using Aegis.Application.Features.AuthorizationModels;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
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
        private readonly IAuditStore? _auditStore;
        private readonly AuthorizationModelValidator _validator;

        public AuthorizationModelAppService(IStoreRegistry storeRegistry, IAuthorizationModelRegistry authorizationModelRegistry)
        {
            _storeRegistry = storeRegistry;
            _authorizationModelRegistry = authorizationModelRegistry;
            _authorizationModelRepository = authorizationModelRegistry as IAuthorizationModelRepository;
            _domainEventDispatcher = null;
            _validator = new AuthorizationModelValidator();
        }

        public AuthorizationModelAppService(
            IStoreRegistry storeRegistry,
            IAuthorizationModelRegistry authorizationModelRegistry,
            IAuthorizationModelRepository authorizationModelRepository,
            IDomainEventDispatcher domainEventDispatcher,
            IAuditStore? auditStore = null)
            : this(storeRegistry, authorizationModelRegistry, authorizationModelRepository, domainEventDispatcher, new AuthorizationModelValidator(), auditStore)
        {
        }

        public AuthorizationModelAppService(
            IStoreRegistry storeRegistry,
            IAuthorizationModelRegistry authorizationModelRegistry,
            IAuthorizationModelRepository authorizationModelRepository,
            IDomainEventDispatcher domainEventDispatcher,
            AuthorizationModelValidator validator,
            IAuditStore? auditStore = null)
            : this(storeRegistry, authorizationModelRegistry)
        {
            _authorizationModelRepository = authorizationModelRepository;
            _domainEventDispatcher = domainEventDispatcher;
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _auditStore = auditStore;
        }

        public async Task<AuthorizationModelDto> CreateAsync(
            string storeId,
            CreateAuthorizationModelRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var validation = await ValidateAsync(new ValidateAuthorizationModelRequestDto(request.SchemaVersion, request.Model), cancellationToken);
            ThrowIfInvalid(validation);

            if (_authorizationModelRepository is null)
            {
                await EnsureStoreExists(storeId, cancellationToken);
                return await _authorizationModelRegistry.CreateAsync(storeId, request.SchemaVersion, request.Model, cancellationToken);
            }

            await EnsureStoreExists(storeId, cancellationToken);
            var authorizationModel = AuthorizationModel.Create(storeId, request.SchemaVersion, request.Model);
            authorizationModel.MarkValidated();
            await _authorizationModelRepository.AddAsync(authorizationModel, cancellationToken);
            await _domainEventDispatcher.DispatchAndClearAsync(authorizationModel, cancellationToken);
            return ToDto(authorizationModel);
        }

        public async Task<AuthorizationModelDto> CreateIdempotentAsync(
            string storeId,
            CreateAuthorizationModelRequestDto request,
            string tenantId,
            string actorId,
            string idempotencyKey,
            string requestFingerprint,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)
                || string.IsNullOrWhiteSpace(actorId)
                || string.IsNullOrWhiteSpace(idempotencyKey)
                || string.IsNullOrWhiteSpace(requestFingerprint)
                || requestFingerprint.Length != 64
                || requestFingerprint.Any(character => !char.IsAsciiHexDigit(character)))
            {
                throw new ArgumentException("A valid tenant, actor, idempotency key, and SHA-256 request fingerprint are required.");
            }

            var validation = await ValidateAsync(new ValidateAuthorizationModelRequestDto(request.SchemaVersion, request.Model), cancellationToken);
            ThrowIfInvalid(validation);
            await EnsureStoreExists(storeId, cancellationToken);

            if (_authorizationModelRepository is null)
            {
                throw new NotSupportedException("Durable idempotency requires an authorization-model repository.");
            }

            var authorizationModel = AuthorizationModel.Create(storeId, request.SchemaVersion, request.Model);
            authorizationModel.MarkValidated();
            var mutation = new IdempotentMutation(
                tenantId,
                actorId,
                "authorization-model.create",
                idempotencyKey,
                requestFingerprint,
                DateTimeOffset.UtcNow.AddHours(24));
            var result = await _authorizationModelRepository.AddIdempotentAsync(authorizationModel, mutation, cancellationToken);
            if (result.Created)
            {
                await _domainEventDispatcher.DispatchAndClearAsync(authorizationModel, cancellationToken);
            }

            return ToDto(result.AuthorizationModel);
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

        public async Task<PublishAuthorizationModelResponseDto?> PublishAsync(
            string storeId,
            string authorizationModelId,
            long expectedRevision,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(authorizationModelId))
            {
                throw new ArgumentException("authorizationModelId is required.");
            }

            await EnsureStoreExists(storeId, cancellationToken);
            var model = await GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            if (model is null)
            {
                return null;
            }

            if (model.Revision != expectedRevision)
            {
                throw new ConcurrencyConflictException("The authorization model lifecycle changed before publish started.");
            }

            var validation = await ValidateAsync(new ValidateAuthorizationModelRequestDto(model.SchemaVersion, model.Model), cancellationToken);
            ThrowIfInvalid(validation);

            if (_authorizationModelRepository is not null)
            {
                var updated = await _authorizationModelRepository.PublishAsync(storeId, authorizationModelId, expectedRevision, cancellationToken);
                var published = updated.FirstOrDefault(x => x.Id.Equals(authorizationModelId, StringComparison.OrdinalIgnoreCase));
                if (published is null)
                {
                    var stillExists = await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
                    if (stillExists is not null)
                    {
                        throw new ConcurrencyConflictException("The authorization model lifecycle changed before publish completed.");
                    }
                }
                return published is null ? null : new PublishAuthorizationModelResponseDto(ToDto(published), published.Id, published.SchemaVersion);
            }

            var currentPublished = await _authorizationModelRegistry.GetPublishedAsync(storeId, cancellationToken);
            if (currentPublished is not null && !currentPublished.Id.Equals(authorizationModelId, StringComparison.OrdinalIgnoreCase))
            {
                await _authorizationModelRegistry.UpdateStateAsync(
                    storeId,
                    currentPublished.Id,
                    AuthorizationModelLifecycleStates.Archived,
                    currentPublished.PublishedAt,
                    DateTimeOffset.UtcNow,
                    authorizationModelId,
                    cancellationToken);
            }

            var publishedDto = await _authorizationModelRegistry.UpdateStateAsync(
                storeId,
                authorizationModelId,
                AuthorizationModelLifecycleStates.Published,
                DateTimeOffset.UtcNow,
                null,
                null,
                cancellationToken);

            return publishedDto is null ? null : new PublishAuthorizationModelResponseDto(publishedDto, publishedDto.Id, publishedDto.SchemaVersion);
        }

        public async Task<RollbackAuthorizationModelResponseDto?> RollbackAsync(
            string storeId,
            string authorizationModelId,
            long expectedRevision,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(authorizationModelId))
            {
                throw new ArgumentException("authorizationModelId is required.");
            }

            await EnsureStoreExists(storeId, cancellationToken);
            var currentPublished = await _authorizationModelRegistry.GetPublishedAsync(storeId, cancellationToken);
            var target = await GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            if (target is null)
            {
                return null;
            }

            if (target.Revision != expectedRevision)
            {
                throw new ConcurrencyConflictException("The authorization model lifecycle changed before rollback started.");
            }

            var validation = await ValidateAsync(new ValidateAuthorizationModelRequestDto(target.SchemaVersion, target.Model), cancellationToken);
            ThrowIfInvalid(validation);

            AuthorizationModelDto? active;
            if (_authorizationModelRepository is not null)
            {
                var rolledBack = await _authorizationModelRepository.RollbackAsync(storeId, authorizationModelId, expectedRevision, cancellationToken);
                if (rolledBack is null)
                {
                    var stillExists = await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
                    if (stillExists is not null)
                    {
                        throw new ConcurrencyConflictException("The authorization model lifecycle changed before rollback completed.");
                    }
                }
                active = rolledBack is null ? null : ToDto(rolledBack);
            }
            else
            {
                if (currentPublished is not null && !currentPublished.Id.Equals(authorizationModelId, StringComparison.OrdinalIgnoreCase))
                {
                    await _authorizationModelRegistry.UpdateStateAsync(
                        storeId,
                        currentPublished.Id,
                        AuthorizationModelLifecycleStates.Archived,
                        currentPublished.PublishedAt,
                        DateTimeOffset.UtcNow,
                        authorizationModelId,
                        cancellationToken);
                }

                active = await _authorizationModelRegistry.UpdateStateAsync(
                    storeId,
                    authorizationModelId,
                    AuthorizationModelLifecycleStates.Published,
                    DateTimeOffset.UtcNow,
                    null,
                    null,
                    cancellationToken);
            }

            if (active is null)
            {
                return null;
            }

            if (_auditStore is not null)
            {
                await _auditStore.WriteAsync(
                    new AuditEvent(storeId, "model.rollback", "system", "rollback", authorizationModelId, "Allow", "MODEL_ROLLED_BACK", DateTimeOffset.UtcNow, storeId),
                    cancellationToken);
            }

            return new RollbackAuthorizationModelResponseDto(active, active.Id, currentPublished?.Id ?? string.Empty);
        }

        public async Task<AuthorizationModelDiffDto?> DiffAsync(
            string storeId,
            string leftAuthorizationModelId,
            string rightAuthorizationModelId,
            CancellationToken cancellationToken = default)
        {
            await EnsureStoreExists(storeId, cancellationToken);
            var left = await GetByIdAsync(storeId, leftAuthorizationModelId, cancellationToken);
            var right = await GetByIdAsync(storeId, rightAuthorizationModelId, cancellationToken);
            if (left is null || right is null)
            {
                return null;
            }

            var leftIndex = ParseModelIndex(left.Model);
            var rightIndex = ParseModelIndex(right.Model);
            var leftTypes = leftIndex.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var rightTypes = rightIndex.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var addedTypes = rightTypes.Except(leftTypes, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
            var removedTypes = leftTypes.Except(rightTypes, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
            var changedTypes = leftTypes.Intersect(rightTypes, StringComparer.OrdinalIgnoreCase)
                .Where(type => !RelationsEqual(leftIndex[type], rightIndex[type]))
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var addedRelations = new List<AuthorizationModelRelationDiffDto>();
            var removedRelations = new List<AuthorizationModelRelationDiffDto>();
            var changedRelations = new List<AuthorizationModelRelationChangeDto>();

            foreach (var type in leftTypes.Union(rightTypes, StringComparer.OrdinalIgnoreCase))
            {
                leftIndex.TryGetValue(type, out var leftRelations);
                rightIndex.TryGetValue(type, out var rightRelations);
                leftRelations ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                rightRelations ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var relation in rightRelations.Keys.Except(leftRelations.Keys, StringComparer.OrdinalIgnoreCase))
                {
                    addedRelations.Add(new AuthorizationModelRelationDiffDto(type, relation, rightRelations[relation]));
                }

                foreach (var relation in leftRelations.Keys.Except(rightRelations.Keys, StringComparer.OrdinalIgnoreCase))
                {
                    removedRelations.Add(new AuthorizationModelRelationDiffDto(type, relation, leftRelations[relation]));
                }

                foreach (var relation in leftRelations.Keys.Intersect(rightRelations.Keys, StringComparer.OrdinalIgnoreCase)
                             .Where(relation => !string.Equals(leftRelations[relation], rightRelations[relation], StringComparison.Ordinal)))
                {
                    changedRelations.Add(new AuthorizationModelRelationChangeDto(type, relation, leftRelations[relation], rightRelations[relation]));
                }
            }

            var hints = new List<string>();
            hints.AddRange(removedTypes.Select(type => $"Removing type '{type}' may break existing tuples and checks."));
            hints.AddRange(removedRelations.Select(relation => $"Removing relation '{relation.Type}#{relation.Relation}' may break tuple writes and checks."));
            hints.AddRange(changedRelations.Select(relation => $"Changing relation '{relation.Type}#{relation.Relation}' can alter authorization decisions."));

            return new AuthorizationModelDiffDto(
                left.Id,
                right.Id,
                addedTypes,
                removedTypes,
                changedTypes,
                addedRelations.OrderBy(x => x.Type).ThenBy(x => x.Relation).ToList(),
                removedRelations.OrderBy(x => x.Type).ThenBy(x => x.Relation).ToList(),
                changedRelations.OrderBy(x => x.Type).ThenBy(x => x.Relation).ToList(),
                hints);
        }

        public async Task<AuthorizationModelDto?> UpdateAsync(
            string storeId,
            string authorizationModelId,
            CreateAuthorizationModelRequestDto request,
            long expectedRevision,
            CancellationToken cancellationToken = default)
        {
            var validation = await ValidateAsync(new ValidateAuthorizationModelRequestDto(request.SchemaVersion, request.Model), cancellationToken);
            ThrowIfInvalid(validation);

            if (string.IsNullOrWhiteSpace(authorizationModelId))
            {
                throw new ArgumentException("authorizationModelId is required.");
            }

            if (_authorizationModelRepository is null)
            {
                await EnsureStoreExists(storeId, cancellationToken);
                var current = await _authorizationModelRegistry.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
                if (current is not null && current.Revision != expectedRevision)
                {
                    throw new ConcurrencyConflictException("The authorization model was modified by another request.");
                }

                return await _authorizationModelRegistry.UpdateAsync(storeId, authorizationModelId, request.SchemaVersion, request.Model, cancellationToken);
            }

            await EnsureStoreExists(storeId, cancellationToken);
            var existing = await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            if (existing is null)
            {
                return null;
            }

            existing.UpdateDefinition(request.SchemaVersion, request.Model);
            existing.MarkValidated();
            var updated = await _authorizationModelRepository.UpdateAsync(existing, expectedRevision, cancellationToken);
            if (updated is null)
            {
                var stillExists = await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
                if (stillExists is not null)
                {
                    throw new ConcurrencyConflictException("The authorization model was modified by another request.");
                }

                return null;
            }

            await _domainEventDispatcher.DispatchAndClearAsync(existing, cancellationToken);
            return ToDto(updated);
        }

        public async Task<bool> DeleteAsync(
            string storeId,
            string authorizationModelId,
            long expectedRevision,
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
                var deleted = await _authorizationModelRepository.DeleteAsync(existing, expectedRevision, cancellationToken);
                if (!deleted)
                {
                    var stillExists = await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
                    if (stillExists is not null)
                    {
                        throw new ConcurrencyConflictException("The authorization model was modified by another request.");
                    }
                }
                if (deleted)
                {
                    await _domainEventDispatcher.DispatchAndClearAsync(existing, cancellationToken);
                }

                return deleted;
            }

            var current = await _authorizationModelRegistry.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            if (current is not null && current.Revision != expectedRevision)
            {
                throw new ConcurrencyConflictException("The authorization model was modified by another request.");
            }

            return await _authorizationModelRegistry.DeleteAsync(storeId, authorizationModelId, cancellationToken);
        }

        public Task<AuthorizationModelValidationResultDto> ValidateAsync(
            ValidateAuthorizationModelRequestDto request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_validator.Validate(request, cancellationToken));
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
                authorizationModel.CreatedAt,
                authorizationModel.State,
                authorizationModel.PublishedAt,
                authorizationModel.ArchivedAt,
                authorizationModel.SupersededBy,
                authorizationModel.Revision);
        }

        private static void ThrowIfInvalid(AuthorizationModelValidationResultDto validation)
        {
            if (validation.Valid)
            {
                return;
            }

            throw new ArgumentException(string.Join(" ", validation.Errors.Select(error => error.Message)));
        }

        private static IReadOnlyDictionary<string, Dictionary<string, string>> ParseModelIndex(string model)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            string? currentType = null;
            foreach (var raw in model.Replace("\r", string.Empty).Split('\n'))
            {
                var line = raw.Trim();
                if (line.StartsWith("type ", StringComparison.OrdinalIgnoreCase))
                {
                    currentType = line[5..].Trim();
                    result.TryAdd(currentType, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                    continue;
                }

                if (currentType is null || !line.StartsWith("define ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var def = line[7..];
                var separatorIndex = def.IndexOf(':');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                result[currentType][def[..separatorIndex].Trim()] = def[(separatorIndex + 1)..].Trim();
            }

            return result;
        }

        private static bool RelationsEqual(
            IReadOnlyDictionary<string, string> left,
            IReadOnlyDictionary<string, string> right)
        {
            return left.Count == right.Count
                && left.All(pair => right.TryGetValue(pair.Key, out var value) && string.Equals(pair.Value, value, StringComparison.Ordinal));
        }
    }
}
