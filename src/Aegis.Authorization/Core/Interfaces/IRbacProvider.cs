using Aegis.Authorization.Core.Models;

namespace Aegis.Authorization.Core.Interfaces
{
    /// <summary>
    /// RBAC permission provider used as fallback when ReBAC does not match.
    /// </summary>
    public interface IRbacProvider
    {
        /// <summary>
        /// Returns whether the request is granted by RBAC policy.
        /// </summary>
        Task<bool> HasPermissionAsync(
            CheckRequest request,
            CancellationToken cancellationToken = default);
    }
}
