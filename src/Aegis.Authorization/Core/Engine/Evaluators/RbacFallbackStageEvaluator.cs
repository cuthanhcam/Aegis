using Aegis.Authorization.Core.Engine.Abstractions;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;

namespace Aegis.Authorization.Core.Engine.Evaluators
{
    /// <summary>
    /// Final stage that falls back to RBAC permission checks.
    /// </summary>
    internal sealed class RbacFallbackStageEvaluator : IAuthorizationStageEvaluator
    {
        private readonly IRbacProvider _rbacProvider;

        public RbacFallbackStageEvaluator(IRbacProvider rbacProvider)
        {
            _rbacProvider = rbacProvider;
        }

        /// <summary>
        /// Evaluates RBAC fallback and produces a terminal allow/deny decision.
        /// </summary>
        public async Task<DecisionResult?> EvaluateAsync(
            CheckRequest request,
            bool includeTrace,
            ICollection<TraceStep> trace,
            CancellationToken cancellationToken)
        {
            if (await _rbacProvider.HasPermissionAsync(request, cancellationToken))
            {
                AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("RBAC_FALLBACK", "MATCHED"));
                AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("FINAL", "ALLOW"));
                return new DecisionResult(true, "ALLOW", "ALLOW_RBAC", trace.ToList());
            }

            AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("RBAC_FALLBACK", "NOT_MATCHED"));
            AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("FINAL", "DENY"));
            return new DecisionResult(false, "DENY", "DENY_NOT_FOUND", trace.ToList());
        }
    }
}
