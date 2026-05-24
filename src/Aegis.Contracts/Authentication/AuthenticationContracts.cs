using System.ComponentModel.DataAnnotations;

namespace Aegis.Contracts.Authentication
{
    /// <summary>
    /// Request payload for signing in with username and password.
    /// </summary>
    public sealed record LoginRequestDto(
        [Required, MinLength(3)] string Username,
        [Required, MinLength(6)] string Password);

    /// <summary>
    /// Request payload for renewing a session using a refresh token.
    /// </summary>
    public sealed record RefreshRequestDto(string? RefreshToken = null);

    /// <summary>
    /// Response returned after successful authentication or refresh.
    /// </summary>
    public sealed record LoginResponseDto(string AccessToken, string? RefreshToken, int ExpiresIn);

    /// <summary>
    /// Profile information returned to authenticated clients.
    /// </summary>
    public sealed record UserProfileDto(
        string Subject,
        string Username,
        string TenantId,
        IReadOnlyList<string> Roles,
        DateTimeOffset? ExpiresAt);
}
