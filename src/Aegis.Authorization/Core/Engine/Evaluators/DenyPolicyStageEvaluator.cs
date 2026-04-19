using Aegis.Authorization.Core.Engine.Abstractions;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;

namespace Aegis.Authorization.Core.Engine.Evaluators
{
    /// <summary>
    /// Stage that checks explicit deny tuples before all other checks.
    /// </summary>
    internal sealed class DenyPolicyStageEvaluator : IAuthorizationStageEvaluator
    {
        private readonly IRelationshipStore _relationshipStore;

        public DenyPolicyStageEvaluator(IRelationshipStore relationshipStore)
        {
            _relationshipStore = relationshipStore;
        }

        /// <summary>
        /// Evaluates explicit deny policy for the current tuple.
        /// </summary>
        public async Task<DecisionResult?> EvaluateAsync(
            CheckRequest request,
            bool includeTrace,
            ICollection<TraceStep> trace,
            CancellationToken cancellationToken)
        {
            var denied = await AuthorizationStageSupport.QueryWithContextAsync(
                _relationshipStore,
                request,
                request.Subject,
                request.Relation,
                request.Object,
                RelationshipEffect.Deny,
                cancellationToken);

            if (denied.Count > 0)
            {
                AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("DENY_POLICY", "MATCHED", AuthorizationStageSupport.Tuple(request)));
                AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("FINAL", "DENY"));
                return new DecisionResult(false, "DENY", "DENY_EXPLICIT", trace.ToList());
            }

            AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("DENY_POLICY", "NOT_MATCHED"));
            return null;
        }
    }
}
