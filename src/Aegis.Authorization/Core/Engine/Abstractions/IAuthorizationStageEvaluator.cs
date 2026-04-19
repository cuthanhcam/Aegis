using Aegis.Authorization.Core.Models;

namespace Aegis.Authorization.Core.Engine.Abstractions
{
    /// <summary>
    /// Contract for one stage in the authorization decision pipeline.
    /// </summary>
    internal interface IAuthorizationStageEvaluator
    {
        /// <summary>
        /// Evaluates the stage and returns a terminal decision or <see langword="null"/> to continue.
        /// </summary>
        Task<DecisionResult?> EvaluateAsync(
            CheckRequest request,
            bool includeTrace,
            ICollection<TraceStep> trace,
            CancellationToken cancellationToken);
    }
}
