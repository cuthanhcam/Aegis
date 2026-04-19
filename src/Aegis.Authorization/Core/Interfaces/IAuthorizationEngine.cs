using Aegis.Authorization.Core.Models;

namespace Aegis.Authorization.Core.Interfaces
{
    /// <summary>
    /// Core authorization decision engine contract.
    /// </summary>
    public interface IAuthorizationEngine
    {
        /// <summary>
        /// Evaluates one authorization check request and returns the final decision.
        /// </summary>
        Task<DecisionResult> CheckAsync(
            CheckRequest request,
            bool includeTrace = false,
            CancellationToken cancellationToken = default);
    }
}
