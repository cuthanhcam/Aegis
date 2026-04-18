using Aegis.Contracts.Authentication;

namespace Aegis.Application.Interfaces
{
    /// <summary>
    /// Authentication session boundary used by the application layer.
    /// </summary>
    public interface IAuthSessionService
    {
        /// <summary>
        /// Authenticates a user and returns a session token pair.
        /// </summary>
        Task<LoginResponseDto?> LoginAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exchanges a refresh token for a new session token pair.
        /// </summary>
        Task<LoginResponseDto?> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes a single refresh token.
        /// </summary>
        Task<bool> RevokeAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes all sessions for a subject inside a tenant.
        /// </summary>
        Task<int> RevokeAllAsync(
            string tenantId,
            string subject,
            CancellationToken cancellationToken = default);
    }
}
