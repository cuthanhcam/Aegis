using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;

namespace Aegis.Authorization.Core.Engine.Rewrite
{
    /// <summary>
    /// Evaluates rewrite terms and tokens against relationship data.
    /// </summary>
    internal sealed class RewriteEvaluator
    {
        private readonly IRelationshipStore _relationshipStore;
        private readonly Func<CheckRequest, bool, ICollection<TraceStep>, ISet<string>, int, CancellationToken, Task<bool>> _isAllowedByRebacAsync;

        public RewriteEvaluator(
            IRelationshipStore relationshipStore,
            Func<CheckRequest, bool, ICollection<TraceStep>, ISet<string>, int, CancellationToken, Task<bool>> isAllowedByRebacAsync)
        {
            _relationshipStore = relationshipStore;
            _isAllowedByRebacAsync = isAllowedByRebacAsync;
        }

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
                    cancellationToken);

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
                    cancellationToken);

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
                    cancellationToken);

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
                    cancellationToken);

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
            foreach (var token in clause)
            {
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
                request.Context);
        }
    }
}
