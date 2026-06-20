using Aegis.Application.DomainEvents;
using Aegis.Application.Interfaces;
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
            var validation = await ValidateAsync(new ValidateAuthorizationModelRequestDto(request.SchemaVersion, request.Model), cancellationToken);
            ThrowIfInvalid(validation);

            if (_authorizationModelRepository is null)
            {
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
            var validation = await ValidateAsync(new ValidateAuthorizationModelRequestDto(request.SchemaVersion, request.Model), cancellationToken);
            ThrowIfInvalid(validation);

            if (string.IsNullOrWhiteSpace(authorizationModelId))
            {
                throw new ArgumentException("authorizationModelId is required.");
            }

            if (_authorizationModelRepository is null)
            {
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
                authorizationModel.CreatedAt);
        }

        private static void ThrowIfInvalid(AuthorizationModelValidationResultDto validation)
        {
            if (validation.Valid)
            {
                return;
            }

            throw new ArgumentException(string.Join(" ", validation.Errors.Select(error => error.Message)));
        }
    }
}
