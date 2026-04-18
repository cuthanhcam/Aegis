namespace Aegis.Contracts.Administration
{
    /// <summary>
    /// Request payload for creating a user.
    /// </summary>
    public sealed record CreateUserRequestDto(
        string UserId,
        string? Email = null,
        string? DisplayName = null);

    /// <summary>
    /// Request payload for updating mutable user fields.
    /// </summary>
    public sealed record UpdateUserRequestDto(
        string? Email = null,
        string? DisplayName = null);

    /// <summary>
    /// Read model representing a user profile.
    /// </summary>
    public sealed record UserDto(
        string UserId,
        DateTimeOffset CreatedAt,
        string? Email = null,
        string? DisplayName = null);

    /// <summary>
    /// Read model containing role names for a user.
    /// </summary>
    public sealed record UserRolesDto(
        string UserId,
        IReadOnlyList<string> Roles);
}
