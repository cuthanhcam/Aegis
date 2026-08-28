using Aegis.Application.DomainEvents;
using Aegis.Application.Contracts;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Authorization.Core.Parsing;
using Aegis.Contracts.Administration;
using Aegis.Domain.Entities;
using Aegis.Domain.Repositories;
using System.Text.RegularExpressions;

namespace Aegis.Application.Services
{
    public sealed class AuthorizationModelAppService : IAuthorizationModelAppService
    {
        private static readonly Regex TypeRegex = new(@"^\s*type\s+([A-Za-z][A-Za-z0-9_]*)\s*$", RegexOptions.Compiled);
        private static readonly Regex DefineRegex = new(@"^\s*define\s+([A-Za-z][A-Za-z0-9_]*)\s*:\s*(.+)$", RegexOptions.Compiled);
        private readonly IStoreRegistry _storeRegistry;
        private readonly IAuthorizationModelRegistry _authorizationModelRegistry;
        private readonly IAuthorizationModelRepository? _authorizationModelRepository;
        private readonly IDomainEventDispatcher? _domainEventDispatcher;
        private readonly IAuditStore? _auditStore;

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
            IDomainEventDispatcher domainEventDispatcher,
            IAuditStore? auditStore = null)
            : this(storeRegistry, authorizationModelRegistry)
        {
            _authorizationModelRepository = authorizationModelRepository;
            _domainEventDispatcher = domainEventDispatcher;
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
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(request);

            var errors = new List<AuthorizationModelValidationIssueDto>();
            var warnings = new List<AuthorizationModelValidationIssueDto>();
            var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var relationsByType = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var currentType = string.Empty;
            var relationCount = 0;
            var directRelationCount = 0;
            var hasUnion = false;
            var hasIntersection = false;
            var hasExclusion = false;
            var hasTupleToUserset = false;

            if (string.IsNullOrWhiteSpace(request.SchemaVersion))
            {
                errors.Add(new AuthorizationModelValidationIssueDto("SCHEMA_VERSION_REQUIRED", "schemaVersion is required."));
            }
            else if (!request.SchemaVersion.Trim().Equals("1.1", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add(new AuthorizationModelValidationIssueDto("SCHEMA_VERSION_UNRECOGNIZED", "Aegis currently validates against schema version 1.1 semantics."));
            }

            if (string.IsNullOrWhiteSpace(request.Model))
            {
                errors.Add(new AuthorizationModelValidationIssueDto("MODEL_REQUIRED", "model is required."));
            }
            else
            {
                var lines = request.Model.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
                for (var index = 0; index < lines.Length; index++)
                {
                    var lineNumber = index + 1;
                    var line = lines[index];
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (trimmed.Equals("model", StringComparison.OrdinalIgnoreCase)
                        || trimmed.Equals("relations", StringComparison.OrdinalIgnoreCase)
                        || trimmed.StartsWith("schema ", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var typeMatch = TypeRegex.Match(line);
                    if (typeMatch.Success)
                    {
                        currentType = typeMatch.Groups[1].Value;
                        if (!types.Add(currentType))
                        {
                            errors.Add(new AuthorizationModelValidationIssueDto("DUPLICATE_TYPE", $"Type '{currentType}' is defined more than once.", lineNumber));
                        }

                        relationsByType.TryAdd(currentType, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                        continue;
                    }

                    var defineMatch = DefineRegex.Match(line);
                    if (defineMatch.Success)
                    {
                        if (string.IsNullOrWhiteSpace(currentType))
                        {
                            errors.Add(new AuthorizationModelValidationIssueDto("RELATION_OUTSIDE_TYPE", "Relation definitions must appear inside a type block.", lineNumber));
                            continue;
                        }

                        var relation = defineMatch.Groups[1].Value;
                        var expression = defineMatch.Groups[2].Value.Trim();
                        if (!relationsByType[currentType].Add(relation))
                        {
                            errors.Add(new AuthorizationModelValidationIssueDto("DUPLICATE_RELATION", $"Relation '{currentType}#{relation}' is defined more than once.", lineNumber));
                        }

                        if (string.IsNullOrWhiteSpace(expression))
                        {
                            errors.Add(new AuthorizationModelValidationIssueDto("EMPTY_RELATION_EXPRESSION", $"Relation '{currentType}#{relation}' has an empty rewrite expression.", lineNumber));
                            continue;
                        }

                        relationCount++;
                        directRelationCount += expression.StartsWith("[", StringComparison.Ordinal) ? 1 : 0;
                        hasUnion |= Regex.IsMatch(expression, @"\bor\b", RegexOptions.IgnoreCase);
                        hasIntersection |= Regex.IsMatch(expression, @"\band\b", RegexOptions.IgnoreCase);
                        hasExclusion |= Regex.IsMatch(expression, @"\bbut\s+not\b", RegexOptions.IgnoreCase);
                        hasTupleToUserset |= Regex.IsMatch(expression, @"\bfrom\b", RegexOptions.IgnoreCase);

                        try
                        {
                            _ = RewriteExpressionParser.Parse(expression);
                        }
                        catch (Exception ex)
                        {
                            errors.Add(new AuthorizationModelValidationIssueDto("INVALID_REWRITE_EXPRESSION", ex.Message, lineNumber));
                        }

                        continue;
                    }

                    warnings.Add(new AuthorizationModelValidationIssueDto("UNRECOGNIZED_MODEL_LINE", $"Line was ignored by the validator: '{trimmed}'.", lineNumber));
                }
            }

            if (types.Count == 0)
            {
                errors.Add(new AuthorizationModelValidationIssueDto("TYPE_REQUIRED", "At least one type definition is required."));
            }

            if (relationCount == 0)
            {
                warnings.Add(new AuthorizationModelValidationIssueDto("RELATION_RECOMMENDED", "Add at least one relation before using this model for authorization checks."));
            }

            if (directRelationCount == 0)
            {
                warnings.Add(new AuthorizationModelValidationIssueDto("DIRECT_RELATION_RECOMMENDED", "At least one direct assignable relation is recommended for tuple writes."));
            }

            var summary = new AuthorizationModelValidationSummaryDto(
                types.Count,
                relationCount,
                directRelationCount,
                hasUnion,
                hasIntersection,
                hasExclusion,
                hasTupleToUserset);

            return Task.FromResult(new AuthorizationModelValidationResultDto(errors.Count == 0, summary, errors, warnings));
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
