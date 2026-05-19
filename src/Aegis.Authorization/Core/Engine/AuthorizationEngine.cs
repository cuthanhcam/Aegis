using Aegis.Authorization.Core.Engine.Abstractions;
using Aegis.Authorization.Core.Engine.Evaluators;
using Aegis.Authorization.Core.Engine.Rewrite;
using Aegis.Authorization.Caching;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Authorization.Core.Parsing;

namespace Aegis.Authorization.Core.Engine
{
    /// <summary>
    /// Default authorization engine that evaluates deny policy, ReBAC, and RBAC fallback stages.
    /// </summary>
    public sealed class AuthorizationEngine : IAuthorizationEngine
    {
        private readonly IRelationshipStore _relationshipStore;
        private readonly IAuthorizationModelProvider? _authorizationModelProvider;
        private readonly AuthorizationCache? _authorizationCache;
        private readonly RewriteEvaluator _rewriteEvaluator;
        private readonly IReadOnlyList<IAuthorizationStageEvaluator> _stageEvaluators;
        private readonly int _maxDepth;

        /// <summary>
        /// Creates an authorization engine with required relationship and RBAC providers.
        /// </summary>
        public AuthorizationEngine(
            IRelationshipStore relationshipStore,
            IRbacProvider rbacProvider,
            IAuthorizationModelProvider? authorizationModelProvider = null,
            AuthorizationCache? authorizationCache = null,
            AuthorizationEngineOptions? options = null)
        {
            _relationshipStore = relationshipStore;
            _authorizationModelProvider = authorizationModelProvider;
            _authorizationCache = authorizationCache;
            _rewriteEvaluator = new RewriteEvaluator(relationshipStore, IsAllowedByRebacAsync);
            _maxDepth = options?.MaxDepth ?? 8;
            _stageEvaluators =
            [
                new DenyPolicyStageEvaluator(relationshipStore),
                new RebacDirectStageEvaluator(IsAllowedByRebacAsync),
                new RbacFallbackStageEvaluator(rbacProvider)
            ];
        }

        /// <summary>
        /// Runs the authorization evaluation pipeline and returns a final decision.
        /// </summary>
        public async Task<DecisionResult> CheckAsync(CheckRequest request, bool includeTrace = false, CancellationToken cancellationToken = default)
        {
            if (_authorizationCache is not null
                && _authorizationCache.TryGet(request, includeTrace, out var cachedResult))
            {
                return cachedResult;
            }

            var trace = new List<TraceStep>();

            var result = await EvaluatePipelineStagesAsync(request, includeTrace, trace, cancellationToken);
            if (result is not null)
            {
                _authorizationCache?.Set(request, includeTrace, result);
                return result;
            }

            throw new InvalidOperationException("Authorization pipeline returned no decision.");
        }

        private async Task<DecisionResult?> EvaluatePipelineStagesAsync(
            CheckRequest request,
            bool includeTrace,
            ICollection<TraceStep> trace,
            CancellationToken cancellationToken)
        {
            foreach (var evaluator in _stageEvaluators)
            {
                var result = await evaluator.EvaluateAsync(request, includeTrace, trace, cancellationToken);
                if (result is not null)
                {
                    return result;
                }
            }

            return null;
        }

        private async Task<bool> IsAllowedByRebacAsync(
            CheckRequest request,
            bool includeTrace,
            ICollection<TraceStep> trace,
            ISet<string> visited,
            int depth,
            CancellationToken cancellationToken)
        {
            if (depth > _maxDepth)
            {
                AuthorizationStageSupport.AddTrace(
                    trace,
                    includeTrace,
                    new TraceStep("REBAC_REWRITE", "MAX_DEPTH_REACHED", $"depth={depth} max={_maxDepth}"));
                return false;
            }

            var visitKey = $"{request.Subject.Value}|{request.Relation}|{request.Object.Value}";
            if (!visited.Add(visitKey))
            {
                AuthorizationStageSupport.AddTrace(
                    trace,
                    includeTrace,
                    new TraceStep("REBAC_REWRITE", "CYCLE_DETECTED", $"node={visitKey} visited={string.Join("=>", visited)} depth={depth}"));
                return false;
            }

            try
            {
                var direct = await QueryWithContextAsync(
                    request,
                    request.Subject,
                    request.Relation,
                    request.Object,
                    RelationshipEffect.Allow,
                    cancellationToken);

                string? latestModel = null;
                if (_authorizationModelProvider is not null)
                {
                    latestModel = string.IsNullOrWhiteSpace(request.AuthorizationModelId)
                        ? await _authorizationModelProvider.GetLatestModelAsync(request.TenantId, cancellationToken)
                        : await _authorizationModelProvider.GetModelAsync(request.TenantId, request.AuthorizationModelId, cancellationToken);
                }

                if (string.IsNullOrWhiteSpace(latestModel))
                {
                    return direct.Count > 0;
                }

                var rules = ParseRules(latestModel);
                var objectType = RewriteSupport.GetTypeName(request.Object.Value);
                if (!rules.TryGetValue((objectType, request.Relation), out var terms))
                {
                    return direct.Count > 0;
                }

                foreach (var term in terms)
                {
                    if (await _rewriteEvaluator.EvaluateTermAsync(request, term, includeTrace, trace, visited, depth, cancellationToken))
                    {
                        AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("REBAC_REWRITE", "MATCHED_TERM", request.Relation));
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                visited.Remove(visitKey);
            }
        }

        private static Dictionary<(string TypeName, string Relation), IReadOnlyList<RewriteTerm>> ParseRules(string model)
        {
            var map = new Dictionary<(string TypeName, string Relation), IReadOnlyList<RewriteTerm>>();
            var lines = model.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string? currentType = null;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
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
                var colon = def.IndexOf(':');
                if (colon <= 0)
                {
                    continue;
                }

                var relation = def[..colon].Trim();
                var expr = def[(colon + 1)..].Trim();
                map[(currentType, relation)] = RewriteExpressionParser.Parse(expr)
                    .Select(x => new RewriteTerm(x.Includes, x.ExcludeClauses))
                    .ToList();
            }

            return map;
        }

        private async Task<IReadOnlyList<RelationshipTuple>> QueryWithContextAsync(
            CheckRequest request,
            Subject? subject,
            string? relation,
            ObjectRef? obj,
            RelationshipEffect? effect,
            CancellationToken cancellationToken)
        {
            return await AuthorizationStageSupport.QueryWithContextAsync(
                _relationshipStore,
                request,
                subject,
                relation,
                obj,
                effect,
                cancellationToken);
        }
    }
}
