using Aegis.Application.DomainEvents;
using Aegis.Application.Features.AuthorizationModels;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
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
        private readonly CreateAuthorizationModelUseCase _createAuthorizationModelUseCase;
        private readonly UpdateAuthorizationModelUseCase _updateAuthorizationModelUseCase;
        private readonly DeleteAuthorizationModelUseCase _deleteAuthorizationModelUseCase;
        private readonly PublishAuthorizationModelUseCase _publishAuthorizationModelUseCase;
        private readonly RollbackAuthorizationModelUseCase _rollbackAuthorizationModelUseCase;

        public AuthorizationModelAppService(IStoreRegistry storeRegistry, IAuthorizationModelRegistry authorizationModelRegistry)
        {
            _storeRegistry = storeRegistry;
            _authorizationModelRegistry = authorizationModelRegistry;
            _authorizationModelRepository = authorizationModelRegistry as IAuthorizationModelRepository;
            _domainEventDispatcher = null;
            _validator = new AuthorizationModelValidator();
            _createAuthorizationModelUseCase = CreateAuthorizationModelUseCase.CreateCompatibility(
                _storeRegistry,
                _authorizationModelRegistry,
                _authorizationModelRepository,
                _domainEventDispatcher,
                _validator);
            _updateAuthorizationModelUseCase = UpdateAuthorizationModelUseCase.CreateCompatibility(
                _storeRegistry,
                _authorizationModelRegistry,
                _authorizationModelRepository,
                _domainEventDispatcher,
                _validator);
            _deleteAuthorizationModelUseCase = DeleteAuthorizationModelUseCase.CreateCompatibility(
                _storeRegistry,
                _authorizationModelRegistry,
                _authorizationModelRepository,
                _domainEventDispatcher);
            _publishAuthorizationModelUseCase = PublishAuthorizationModelUseCase.CreateCompatibility(
                _storeRegistry,
                _authorizationModelRegistry,
                _authorizationModelRepository,
                _validator);
            _rollbackAuthorizationModelUseCase = RollbackAuthorizationModelUseCase.CreateCompatibility(
                _storeRegistry,
                _authorizationModelRegistry,
                _authorizationModelRepository,
                _validator,
                _auditStore);
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
            _createAuthorizationModelUseCase = CreateAuthorizationModelUseCase.CreateCompatibility(
                _storeRegistry,
                _authorizationModelRegistry,
                _authorizationModelRepository,
                _domainEventDispatcher,
                _validator);
            _updateAuthorizationModelUseCase = UpdateAuthorizationModelUseCase.CreateCompatibility(
                _storeRegistry,
                _authorizationModelRegistry,
                _authorizationModelRepository,
                _domainEventDispatcher,
                _validator);
            _deleteAuthorizationModelUseCase = DeleteAuthorizationModelUseCase.CreateCompatibility(
                _storeRegistry,
                _authorizationModelRegistry,
                _authorizationModelRepository,
                _domainEventDispatcher);
            _publishAuthorizationModelUseCase = PublishAuthorizationModelUseCase.CreateCompatibility(
                _storeRegistry,
                _authorizationModelRegistry,
                _authorizationModelRepository,
                _validator);
            _rollbackAuthorizationModelUseCase = RollbackAuthorizationModelUseCase.CreateCompatibility(
                _storeRegistry,
                _authorizationModelRegistry,
                _authorizationModelRepository,
                _validator,
                _auditStore);
        }

        public async Task<AuthorizationModelDto> CreateAsync(
            string storeId,
            CreateAuthorizationModelRequestDto request,
            CancellationToken cancellationToken = default)
        {
            return await _createAuthorizationModelUseCase.ExecuteAsync(storeId, request, cancellationToken);
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
            return await _createAuthorizationModelUseCase.ExecuteIdempotentAsync(
                storeId,
                request,
                tenantId,
                actorId,
                idempotencyKey,
                requestFingerprint,
                cancellationToken);
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
            return await _publishAuthorizationModelUseCase.ExecuteAsync(
                storeId,
                authorizationModelId,
                expectedRevision,
                cancellationToken);
        }

        public async Task<RollbackAuthorizationModelResponseDto?> RollbackAsync(
            string storeId,
            string authorizationModelId,
            long expectedRevision,
            CancellationToken cancellationToken = default)
        {
            return await _rollbackAuthorizationModelUseCase.ExecuteAsync(
                storeId,
                authorizationModelId,
                expectedRevision,
                cancellationToken);
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
            return await _updateAuthorizationModelUseCase.ExecuteAsync(
                storeId,
                authorizationModelId,
                request,
                expectedRevision,
                cancellationToken);
        }

        public async Task<bool> DeleteAsync(
            string storeId,
            string authorizationModelId,
            long expectedRevision,
            CancellationToken cancellationToken = default)
        {
            return await _deleteAuthorizationModelUseCase.ExecuteAsync(
                storeId,
                authorizationModelId,
                expectedRevision,
                cancellationToken);
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
