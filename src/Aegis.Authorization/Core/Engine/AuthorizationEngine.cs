using Aegis.Authorization.Core.Engine.Abstractions;
using Aegis.Authorization.Core.Engine.Evaluators;
using Aegis.Authorization.Core.Engine.Rewrite;
using Aegis.Authorization.Caching;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Authorization.Core.Parsing;
using Aegis.Authorization.Core.Metrics;

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
        // Parsed model cache (LRU with TTL) to avoid external dependencies.
        private readonly ParsedModelCache _parsedModelCache;

        // Per-request memoization store (async-local to scope to the logical call context)
        private static readonly System.Threading.AsyncLocal<IDictionary<string, bool>?> _memo = new();
        private readonly Aegis.Authorization.Core.Metrics.IAuthorizationMetrics? _metrics;

        /// <summary>
        /// Creates an authorization engine with required relationship and RBAC providers.
        /// </summary>
        public AuthorizationEngine(
            IRelationshipStore relationshipStore,
            IRbacProvider rbacProvider,
            IAuthorizationMetrics? metrics = null,
            IAuthorizationModelProvider? authorizationModelProvider = null,
            AuthorizationCache? authorizationCache = null,
            AuthorizationEngineOptions? options = null)
        {
            _relationshipStore = relationshipStore;
            _authorizationModelProvider = authorizationModelProvider;
            _authorizationCache = authorizationCache;
            _metrics = metrics;
            _rewriteEvaluator = new RewriteEvaluator(relationshipStore, IsAllowedByRebacAsync, _metrics);
            _maxDepth = options?.MaxDepth ?? 8;
            // Configure parsed model LRU cache
            var ttl = TimeSpan.FromSeconds(options?.ParsedModelCacheTtlSeconds ?? 300);
            var sizeLimit = options?.ParsedModelCacheSizeLimit ?? 1024;
            _parsedModelCache = new ParsedModelCache(sizeLimit, ttl, _metrics);
            _stageEvaluators =
            [
                new DenyPolicyStageEvaluator(relationshipStore, _metrics),
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

            // Initialize per-request memoization store
            _memo.Value = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            var trace = new List<TraceStep>();

            var result = await EvaluatePipelineStagesAsync(request, includeTrace, trace, cancellationToken);
            if (result is not null)
            {
                _authorizationCache?.Set(request, includeTrace, result);
                _memo.Value = null;
                return result;
            }

            _memo.Value = null;
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
            // Memoization: return cached result when available for identical requests within the same evaluation
            var memo = _memo.Value;
            if (memo is not null)
            {
                var memoKey = BuildMemoKey(request);
                if (memo.TryGetValue(memoKey, out var cached))
                {
                    AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("REBAC_REWRITE", "MEMO_HIT", memoKey));
                    _metrics?.IncrementMemoHit();
                    return cached;
                }
                _metrics?.IncrementMemoMiss();
            }
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
                    var res = direct.Count > 0;
                    if (memo is not null)
                    {
                        memo[BuildMemoKey(request)] = res;
                    }
                    return res;
                }

                var rules = ParseRulesCached(latestModel);
                var objectType = RewriteSupport.GetTypeName(request.Object.Value);
                if (!rules.TryGetValue((objectType, request.Relation), out var terms))
                {
                    var res = direct.Count > 0;
                    if (memo is not null)
                    {
                        memo[BuildMemoKey(request)] = res;
                    }
                    return res;
                }

                foreach (var term in terms)
                {
                    if (await _rewriteEvaluator.EvaluateTermAsync(request, term, includeTrace, trace, visited, depth, cancellationToken))
                    {
                        AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("REBAC_REWRITE", "MATCHED_TERM", request.Relation));
                        if (memo is not null)
                        {
                            memo[BuildMemoKey(request)] = true;
                        }
                        return true;
                    }
                }

                if (memo is not null)
                {
                    memo[BuildMemoKey(request)] = false;
                }

                return false;
            }
            finally
            {
                visited.Remove(visitKey);
            }
        }

        private Dictionary<(string TypeName, string Relation), IReadOnlyList<RewriteTerm>> ParseRulesCached(string model)
        {
            if (_parsedModelCache.TryGet(model, out var cached))
            {
                return cached;
            }

            var map = ParseRules(model);
            _parsedModelCache.Set(model, map);
            return map;
        }

        private static string BuildMemoKey(CheckRequest request)
        {
            var builder = new System.Text.StringBuilder(256);
            builder.Append(request.TenantId).Append('|')
                .Append(request.Subject.Value).Append('|')
                .Append(request.Relation).Append('|')
                .Append(request.Object.Value).Append('|')
                .Append(request.AuthorizationModelId ?? string.Empty);

            if (request.ContextualTuples is not null && request.ContextualTuples.Count > 0)
            {
                foreach (var t in request.ContextualTuples.OrderBy(x => x.Subject.Value, StringComparer.Ordinal))
                {
                    builder.Append("|ct:").Append(t.Subject.Value).Append(',').Append(t.Relation).Append(',').Append(t.Object.Value).Append(',').Append(t.Effect);
                }
            }

            if (request.Context is not null && request.Context.Count > 0)
            {
                foreach (var p in request.Context.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    builder.Append("|ctx:").Append(p.Key).Append('=').Append(p.Value.GetRawText());
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Lightweight thread-safe LRU cache for parsed models with TTL.
        /// </summary>
        private sealed class ParsedModelCache
        {
            private readonly int _capacity;
            private readonly TimeSpan _ttl;
            private readonly Dictionary<string, (Dictionary<(string TypeName, string Relation), IReadOnlyList<RewriteTerm>> Value, DateTimeOffset ExpiresAt)> _map = new(StringComparer.Ordinal);
            private readonly LinkedList<string> _lru = new();
            private readonly object _sync = new();
            private readonly IAuthorizationMetrics? _metrics;

            public ParsedModelCache(int capacity, TimeSpan ttl, IAuthorizationMetrics? metrics = null)
            {
                _capacity = Math.Max(1, capacity);
                _ttl = ttl;
                _metrics = metrics;
            }

            public bool TryGet(string key, out Dictionary<(string TypeName, string Relation), IReadOnlyList<RewriteTerm>> value)
            {
                lock (_sync)
                {
                    if (_map.TryGetValue(key, out var entry))
                    {
                        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
                        {
                            // expired
                            _map.Remove(key);
                            _lru.Remove(key);
                            value = null!;
                            _metrics?.IncrementParseCacheMiss();
                            return false;
                        }

                        // refresh LRU
                        _lru.Remove(key);
                        _lru.AddFirst(key);
                        value = entry.Value;
                        _metrics?.IncrementParseCacheHit();
                        return true;
                    }

                    value = null!;
                    _metrics?.IncrementParseCacheMiss();
                    return false;
                }
            }

            public void Set(string key, Dictionary<(string TypeName, string Relation), IReadOnlyList<RewriteTerm>> value)
            {
                lock (_sync)
                {
                    if (_map.ContainsKey(key))
                    {
                        _map[key] = (value, DateTimeOffset.UtcNow.Add(_ttl));
                        _lru.Remove(key);
                        _lru.AddFirst(key);
                        return;
                    }

                    // Evict if needed
                    while (_map.Count >= _capacity && _lru.Count > 0)
                    {
                        var last = _lru.Last!.Value;
                        _lru.RemoveLast();
                        _map.Remove(last);
                    }

                    _map[key] = (value, DateTimeOffset.UtcNow.Add(_ttl));
                    _lru.AddFirst(key);
                    _metrics?.IncrementParseCacheMiss();
                }
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
                cancellationToken,
                _metrics);
        }
    }
}
