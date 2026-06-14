using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Compatibility;
using Aegis.Authorization.Core.Parsing;

namespace Aegis.Application.Features.Query
{
    public sealed class ResolveQueryModelContextUseCase
    {
        private readonly IStoreRegistry _storeRegistry;
        private readonly IAuthorizationModelRegistry _authorizationModelRegistry;

        public ResolveQueryModelContextUseCase(
            IStoreRegistry storeRegistry,
            IAuthorizationModelRegistry authorizationModelRegistry)
        {
            _storeRegistry = storeRegistry;
            _authorizationModelRegistry = authorizationModelRegistry;
        }

        public async Task<QueryModelContext> ExecuteAsync(
            string storeId,
            string? authorizationModelId,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(storeId, storeId, authorizationModelId, cancellationToken);
        }

        public async Task<QueryModelContext> ExecuteAsync(
            string tenantId,
            string storeId,
            string? authorizationModelId,
            CancellationToken cancellationToken = default)
        {
            await EnsureStoreExistsAsync(tenantId, storeId, cancellationToken);

            AuthorizationModelDto? model;
            if (string.IsNullOrWhiteSpace(authorizationModelId))
            {
                model = await _authorizationModelRegistry.GetLatestAsync(storeId, cancellationToken);
            }
            else
            {
                model = await _authorizationModelRegistry.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
                if (model is null)
                {
                    throw new CompatibilityApiException(
                        400,
                        "authorization_model_not_found",
                        $"Authorization Model '{authorizationModelId}' not found");
                }
            }

            if (model is null)
            {
                throw new CompatibilityApiException(
                    400,
                    "latest_authorization_model_not_found",
                    $"No authorization models found for store '{storeId}'");
            }

            return new QueryModelContext(model.Id, ParseRules(model.Model), BuildRelationIndex(model.Model));
        }

        public void ValidateTypeAndRelationExists(
            string typeName,
            string relation,
            IReadOnlyDictionary<string, HashSet<string>> relationIndex)
        {
            if (!relationIndex.TryGetValue(typeName, out var relations))
            {
                throw new CompatibilityApiException(400, "type_not_found", $"type '{typeName}' not found");
            }

            if (!relations.Contains(relation))
            {
                throw new CompatibilityApiException(400, "relation_not_found", $"relation '{typeName}#{relation}' not found");
            }
        }

        private async Task EnsureStoreExistsAsync(string tenantId, string storeId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                throw new ArgumentException("storeId is required.");
            }

            var store = await _storeRegistry.GetForTenantAsync(tenantId, storeId, cancellationToken);
            if (store is null)
            {
                throw new CompatibilityApiException(404, "store_id_not_found", "Store ID not found.");
            }
        }

        private static Dictionary<(string TypeName, string Relation), IReadOnlyList<QueryRewriteTerm>> ParseRules(string? model)
        {
            var map = new Dictionary<(string TypeName, string Relation), IReadOnlyList<QueryRewriteTerm>>();
            if (string.IsNullOrWhiteSpace(model))
            {
                return map;
            }

            var lines = model.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string? currentType = null;

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.StartsWith("type ", StringComparison.OrdinalIgnoreCase))
                {
                    currentType = line[5..].Trim();
                    continue;
                }

                if (currentType is null || !line.StartsWith("define ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var def = line[7..];
                var idx = def.IndexOf(':');
                if (idx <= 0)
                {
                    continue;
                }

                var relation = def[..idx].Trim();
                var expr = def[(idx + 1)..].Trim();

                map[(currentType, relation)] = RewriteExpressionParser.Parse(expr)
                    .Select(x => new QueryRewriteTerm(x.Includes, x.ExcludeClauses))
                    .ToList();
            }

            return map;
        }

        private static IReadOnlyDictionary<string, HashSet<string>> BuildRelationIndex(string model)
        {
            var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var lines = model.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string? currentType = null;

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.StartsWith("type ", StringComparison.OrdinalIgnoreCase))
                {
                    currentType = line[5..].Trim();
                    if (!result.ContainsKey(currentType))
                    {
                        result[currentType] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    }

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

                result[currentType].Add(def[..separatorIndex].Trim());
            }

            return result;
        }
    }

    public sealed record QueryRewriteTerm(
            IReadOnlyList<string> Includes,
            IReadOnlyList<IReadOnlyList<string>> ExcludeClauses);

    public sealed record QueryModelContext(
        string AuthorizationModelId,
        Dictionary<(string TypeName, string Relation), IReadOnlyList<QueryRewriteTerm>> Rules,
        IReadOnlyDictionary<string, HashSet<string>> RelationIndex);
}
