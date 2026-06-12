using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;

namespace Aegis.Authorization.Core.Engine.Rewrite
{
    /// <summary>
    /// Evaluates rewrite terms and tokens against relationship data.
    /// Supports batch querying to reduce database round-trips.
    /// </summary>
    internal sealed class RewriteEvaluator
    {
        private readonly IRelationshipStore _relationshipStore;
        private readonly Func<CheckRequest, bool, ICollection<TraceStep>, ISet<string>, int, CancellationToken, Task<bool>> _isAllowedByRebacAsync;
        private readonly Aegis.Authorization.Core.Metrics.IAuthorizationMetrics? _metrics;

        public RewriteEvaluator(
            IRelationshipStore relationshipStore,
            Func<CheckRequest, bool, ICollection<TraceStep>, ISet<string>, int, CancellationToken, Task<bool>> isAllowedByRebacAsync,
            Aegis.Authorization.Core.Metrics.IAuthorizationMetrics? metrics = null)
        {
            _relationshipStore = relationshipStore;
            _isAllowedByRebacAsync = isAllowedByRebacAsync;
            _metrics = metrics;
        }

        /// <summary>
        /// Represents a query specification for batch execution.
        /// </summary>
        private sealed record QuerySpec(
            int TokenIndex,
            string Token,
            Subject? Subject,
            string? Relation,
            ObjectRef? Object,
            RelationshipEffect? Effect);

        /// <summary>
        /// Evaluates one rewrite term including include tokens and exclude clauses.
        /// </summary>
        public async Task<bool> EvaluateTermAsync(
            CheckRequest request,
            RewriteTerm term,
            bool includeTrace,
            ICollection<TraceStep> trace,
            ISet<string> visited,
            int depth,
            CancellationToken cancellationToken)
        {
            foreach (var include in term.Includes)
            {
                if (!await EvaluateTokenAsync(request, include, includeTrace, trace, visited, depth, cancellationToken))
                {
                    return false;
                }
            }

            foreach (var excludeClause in term.ExcludeClauses)
            {
                if (await EvaluateClauseAsync(request, excludeClause, includeTrace, trace, visited, depth, cancellationToken))
                {
                    AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("REBAC_REWRITE", "EXCLUDED", string.Join(" and ", excludeClause)));
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> EvaluateTokenAsync(
            CheckRequest request,
            string token,
            bool includeTrace,
            ICollection<TraceStep> trace,
            ISet<string> visited,
            int depth,
            CancellationToken cancellationToken)
        {
            if (string.Equals(token, "this", StringComparison.OrdinalIgnoreCase))
            {
                var direct = await AuthorizationStageSupport.QueryWithContextAsync(
                    _relationshipStore,
                    request,
                    request.Subject,
                    request.Relation,
                    request.Object,
                    RelationshipEffect.Allow,
                    cancellationToken,
                    _metrics);

                if (direct.Count > 0)
                {
                    AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("REBAC_REWRITE", "MATCHED_THIS", AuthorizationStageSupport.Tuple(request)));
                    return true;
                }

                return false;
            }

            if (RewriteSupport.TryParseConditionedToken(token, out var baseToken, out var conditionName))
            {
                if (!RewriteSupport.EvaluateCondition(conditionName, request.Context))
                {
                    AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("REBAC_REWRITE", "CONDITION_NOT_MET", conditionName));
                    return false;
                }

                return await EvaluateTokenAsync(request, baseToken, includeTrace, trace, visited, depth, cancellationToken);
            }

            if (RewriteSupport.TryParseTupleToUsersetToken(token, out var computedRelation, out var tuplesetRelation))
            {
                var tuplesetCandidates = await AuthorizationStageSupport.QueryWithContextAsync(
                    _relationshipStore,
                    request,
                    null,
                    tuplesetRelation,
                    request.Object,
                    RelationshipEffect.Allow,
                    cancellationToken,
                    _metrics);

                foreach (var tuple in tuplesetCandidates)
                {
                    var nested = BuildNestedRequest(
                        request,
                        computedRelation,
                        new ObjectRef(tuple.Subject.Value));
                    if (await _isAllowedByRebacAsync(nested, includeTrace, trace, visited, depth + 1, cancellationToken))
                    {
                        AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("REBAC_REWRITE", "MATCHED_TUPLESET", tuplesetRelation));
                        return true;
                    }
                }

                return false;
            }

            if (token.Contains('#'))
            {
                if (!RewriteSupport.TryParseUsersetToken(token, out var expectedType, out var marker))
                {
                    return false;
                }

                var usersetCandidates = await AuthorizationStageSupport.QueryWithContextAsync(
                    _relationshipStore,
                    request,
                    null,
                    request.Relation,
                    request.Object,
                    RelationshipEffect.Allow,
                    cancellationToken,
                    _metrics);

                foreach (var tuple in usersetCandidates)
                {
                    if (!RewriteSupport.TryParseUsersetRef(tuple.Subject.Value, out var usersetObject, out var usersetRelation))
                    {
                        continue;
                    }

                    if (!string.Equals(usersetRelation, marker, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(expectedType)
                        && !string.Equals(RewriteSupport.GetTypeName(usersetObject), expectedType, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var nested = BuildNestedRequest(
                        request,
                        marker,
                        new ObjectRef(usersetObject));
                    if (await _isAllowedByRebacAsync(nested, includeTrace, trace, visited, depth + 1, cancellationToken))
                    {
                        AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("REBAC_REWRITE", "MATCHED_USERSET", tuple.Subject.Value));
                        return true;
                    }
                }

                return false;
            }

            if (RewriteSupport.IsTypeToken(token)
                && RewriteSupport.SubjectMatchesTypeToken(request.Subject.Value, token))
            {
                var direct = await AuthorizationStageSupport.QueryWithContextAsync(
                    _relationshipStore,
                    request,
                    request.Subject,
                    request.Relation,
                    request.Object,
                    RelationshipEffect.Allow,
                    cancellationToken,
                    _metrics);

                return direct.Count > 0;
            }

            var nestedRequest = BuildNestedRequest(
                request,
                token,
                request.Object);
            if (await _isAllowedByRebacAsync(nestedRequest, includeTrace, trace, visited, depth + 1, cancellationToken))
            {
                AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("REBAC_REWRITE", "MATCHED_COMPUTED", token));
                return true;
            }

            return false;
        }

        private async Task<bool> EvaluateClauseAsync(
            CheckRequest request,
            IReadOnlyList<string> clause,
            bool includeTrace,
            ICollection<TraceStep> trace,
            ISet<string> visited,
            int depth,
            CancellationToken cancellationToken)
        {
            // Collect queries for tokens that need database lookups (tupleset and userset)
            var querySpecs = new List<QuerySpec>();
            var tuplesetTokens = new List<(int index, string token, string computedRelation, string tuplesetRelation)>();
            var usersetTokens = new List<(int index, string token, string marker, string? expectedType)>();

            for (int i = 0; i < clause.Count; i++)
            {
                var token = clause[i];

                // Collect tupleset queries
                if (RewriteSupport.TryParseTupleToUsersetToken(token, out var computedRelation, out var tuplesetRelation))
                {
                    querySpecs.Add(new QuerySpec(
                        i,
                        token,
                        null,
                        tuplesetRelation,
                        request.Object,
                        RelationshipEffect.Allow));
                    tuplesetTokens.Add((i, token, computedRelation, tuplesetRelation));
                    continue;
                }

                // Collect userset queries
                if (token.Contains('#'))
                {
                    if (RewriteSupport.TryParseUsersetToken(token, out var expectedType, out var marker))
                    {
                        querySpecs.Add(new QuerySpec(
                            i,
                            token,
                            null,
                            request.Relation,
                            request.Object,
                            RelationshipEffect.Allow));
                        usersetTokens.Add((i, token, marker, expectedType));
                    }
                }
            }

            // Execute all queries in one batch
            IReadOnlyList<IReadOnlyList<RelationshipTuple>>? batchResults = null;
            if (querySpecs.Count > 0)
            {
                var queryList = new List<(Subject?, string?, ObjectRef?, RelationshipEffect?)>(querySpecs.Count);
                foreach (var qs in querySpecs)
                {
                    queryList.Add((qs.Subject, qs.Relation, qs.Object, qs.Effect));
                }

                batchResults = await _relationshipStore.QueryMultipleAsync(
                    request.TenantId,
                    queryList,
                    cancellationToken,
                    request.EffectiveStoreId);
                _metrics?.IncrementDbQuery();
                if (batchResults is not null)
                {
                    var totalResults = batchResults.Sum(r => r.Count);
                    _metrics?.AddDbResultCount(totalResults);
                }
            }

            // Evaluate all tokens in clause order
            for (int i = 0; i < clause.Count; i++)
            {
                var token = clause[i];

                // Check if this token has batched results
                var batchedResultIndex = querySpecs.FindIndex(qs => qs.TokenIndex == i);
                if (batchedResultIndex >= 0 && batchResults is not null)
                {
                    var candidates = batchResults[batchedResultIndex];

                    // Handle tupleset token
                    if (tuplesetTokens.Any(t => t.index == i))
                    {
                        var (_, _, computedRelation, tuplesetRelation) = tuplesetTokens.First(t => t.index == i);
                        var found = false;
                        foreach (var tuple in candidates)
                        {
                            var nested = BuildNestedRequest(
                                request,
                                computedRelation,
                                new ObjectRef(tuple.Subject.Value));
                            if (await _isAllowedByRebacAsync(nested, includeTrace, trace, visited, depth + 1, cancellationToken))
                            {
                                AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("REBAC_REWRITE", "MATCHED_TUPLESET", tuplesetRelation));
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            return false;
                        }
                        continue;
                    }

                    // Handle userset token
                    if (usersetTokens.Any(t => t.index == i))
                    {
                        var (_, _, marker, expectedType) = usersetTokens.First(t => t.index == i);
                        var found = false;
                        foreach (var tuple in candidates)
                        {
                            if (!RewriteSupport.TryParseUsersetRef(tuple.Subject.Value, out var usersetObject, out var usersetRelation))
                            {
                                continue;
                            }

                            if (!string.Equals(usersetRelation, marker, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            if (!string.IsNullOrWhiteSpace(expectedType)
                                && !string.Equals(RewriteSupport.GetTypeName(usersetObject), expectedType, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            var nested = BuildNestedRequest(
                                request,
                                marker,
                                new ObjectRef(usersetObject));
                            if (await _isAllowedByRebacAsync(nested, includeTrace, trace, visited, depth + 1, cancellationToken))
                            {
                                AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("REBAC_REWRITE", "MATCHED_USERSET", tuple.Subject.Value));
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            return false;
                        }
                        continue;
                    }
                }

                // Evaluate non-batched tokens individually
                if (!await EvaluateTokenAsync(request, token, includeTrace, trace, visited, depth, cancellationToken))
                {
                    return false;
                }
            }

            return true;
        }

        private static CheckRequest BuildNestedRequest(
            CheckRequest request,
            string relation,
            ObjectRef obj)
        {
            return new CheckRequest(
                request.TenantId,
                request.Subject,
                relation,
                obj,
                request.ContextualTuples,
                request.Consistency,
                request.AuthorizationModelId,
                request.Context,
                request.StoreId);
        }
    }
}
