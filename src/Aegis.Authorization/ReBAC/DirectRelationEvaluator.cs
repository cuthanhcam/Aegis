using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;

namespace Aegis.Authorization.ReBAC
{
    /// <summary>
    /// Evaluates direct ReBAC tuples before rewrite expansion.
    /// </summary>
    public sealed class DirectRelationEvaluator
    {
        private readonly IRelationshipStore _relationshipStore;

        public DirectRelationEvaluator(IRelationshipStore relationshipStore)
        {
            _relationshipStore = relationshipStore;
        }

        /// <summary>
        /// Evaluates direct deny and allow tuples and returns terminal/non-terminal outcome.
        /// </summary>
        public async Task<DirectRelationEvaluationResult> EvaluateAsync(
            CheckRequest request,
            CancellationToken cancellationToken = default)
        {
            var denied = await QueryWithContextAsync(
                request,
                request.Subject,
                request.Relation,
                request.Object,
                RelationshipEffect.Deny,
                cancellationToken);

            if (denied.Count > 0)
            {
                return DirectRelationEvaluationResult.CreateDenied("DENY_EXPLICIT");
            }

            var allowed = await QueryWithContextAsync(
                request,
                request.Subject,
                request.Relation,
                request.Object,
                RelationshipEffect.Allow,
                cancellationToken);

            if (allowed.Count > 0)
            {
                return DirectRelationEvaluationResult.CreateAllowed("ALLOW_REBAC_DIRECT");
            }

            return DirectRelationEvaluationResult.NoMatch();
        }

        private async Task<IReadOnlyList<RelationshipTuple>> QueryWithContextAsync(
            CheckRequest request,
            Subject? subject,
            string? relation,
            ObjectRef? obj,
            RelationshipEffect? effect,
            CancellationToken cancellationToken)
        {
            var persisted = await _relationshipStore.QueryAsync(request.TenantId, subject, relation, obj, effect, cancellationToken);
            if (request.ContextualTuples is null || request.ContextualTuples.Count == 0)
            {
                return persisted;
            }

            var contextual = request.ContextualTuples
                .Where(x => subject is null || x.Subject == subject)
                .Where(x => relation is null || x.Relation.Equals(relation, StringComparison.OrdinalIgnoreCase))
                .Where(x => obj is null || x.Object == obj)
                .Where(x => effect is null || x.Effect == effect)
                .ToList();

            if (contextual.Count == 0)
            {
                return persisted;
            }

            return persisted.Concat(contextual)
                .GroupBy(x => $"{x.Subject.Value}|{x.Relation}|{x.Object.Value}|{x.Effect}", StringComparer.OrdinalIgnoreCase)
                .Select(x => x.OrderByDescending(y => y.CreatedAt).First())
                .ToList();
        }
    }

    /// <summary>
    /// Result for direct relationship evaluation with terminal state metadata.
    /// </summary>
    /// <param name="IsTerminal">True when evaluation produced final allow/deny.</param>
    /// <param name="Allowed">Final decision when terminal; otherwise false.</param>
    /// <param name="ReasonCode">Reason code aligned with decision pipeline semantics.</param>
    public sealed record DirectRelationEvaluationResult(
        bool IsTerminal,
        bool Allowed,
        string ReasonCode)
    {
        public static DirectRelationEvaluationResult CreateAllowed(string reasonCode)
        {
            return new DirectRelationEvaluationResult(true, true, reasonCode);
        }

        public static DirectRelationEvaluationResult CreateDenied(string reasonCode)
        {
            return new DirectRelationEvaluationResult(true, false, reasonCode);
        }

        public static DirectRelationEvaluationResult NoMatch()
        {
            return new DirectRelationEvaluationResult(false, false, "NOT_MATCHED");
        }
    }
}
