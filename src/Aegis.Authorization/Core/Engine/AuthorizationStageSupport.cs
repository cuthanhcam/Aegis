using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;

namespace Aegis.Authorization.Core.Engine
{
    /// <summary>
    /// Shared helpers for stage evaluation and tuple querying.
    /// </summary>
    internal static class AuthorizationStageSupport
    {
        /// <summary>
        /// Adds a trace step only when tracing is enabled.
        /// </summary>
        public static void AddTrace(ICollection<TraceStep> trace, bool includeTrace, TraceStep step)
        {
            if (includeTrace)
            {
                trace.Add(step);
            }
        }

        /// <summary>
        /// Formats a check tuple string for diagnostic tracing.
        /// </summary>
        public static string Tuple(CheckRequest request)
        {
            return $"({request.Subject.Value}, {request.Relation}, {request.Object.Value})";
        }

        /// <summary>
        /// Queries persisted tuples and merges contextual tuples from the request.
        /// </summary>
        public static async Task<IReadOnlyList<RelationshipTuple>> QueryWithContextAsync(
            IRelationshipStore relationshipStore,
            CheckRequest request,
            Subject? subject,
            string? relation,
            ObjectRef? obj,
            RelationshipEffect? effect,
            CancellationToken cancellationToken,
            Aegis.Authorization.Core.Metrics.IAuthorizationMetrics? metrics = null)
        {
            var persisted = await relationshipStore.QueryAsync(request.TenantId, subject, relation, obj, effect, cancellationToken);
            metrics?.IncrementDbQuery();
            metrics?.AddDbResultCount(persisted.Count);
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
}
