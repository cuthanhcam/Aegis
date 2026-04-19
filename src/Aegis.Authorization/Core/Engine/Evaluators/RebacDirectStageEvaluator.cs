using Aegis.Authorization.Core.Engine.Abstractions;
using Aegis.Authorization.Core.Models;

namespace Aegis.Authorization.Core.Engine.Evaluators
{
    /// <summary>
    /// Stage that evaluates ReBAC direct/rewrite paths.
    /// </summary>
    internal sealed class RebacDirectStageEvaluator : IAuthorizationStageEvaluator
    {
        private readonly Func<CheckRequest, bool, ICollection<TraceStep>, ISet<string>, int, CancellationToken, Task<bool>> _isAllowedByRebac;

        public RebacDirectStageEvaluator(
            Func<CheckRequest, bool, ICollection<TraceStep>, ISet<string>, int, CancellationToken, Task<bool>> isAllowedByRebac)
        {
            _isAllowedByRebac = isAllowedByRebac;
        }

        /// <summary>
        /// Evaluates the request using the delegated ReBAC evaluator.
        /// </summary>
        public async Task<DecisionResult?> EvaluateAsync(
            CheckRequest request,
            bool includeTrace,
            ICollection<TraceStep> trace,
            CancellationToken cancellationToken)
        {
            var allowed = await _isAllowedByRebac(
                request,
                includeTrace,
                trace,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                0,
                cancellationToken);

            if (allowed)
            {
                AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("REBAC_DIRECT", "MATCHED", AuthorizationStageSupport.Tuple(request)));
                AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("FINAL", "ALLOW"));
                return new DecisionResult(true, "ALLOW", "ALLOW_REBAC_DIRECT", trace.ToList());
            }

            AuthorizationStageSupport.AddTrace(trace, includeTrace, new TraceStep("REBAC_DIRECT", "NOT_MATCHED"));
            return null;
        }
    }
}
