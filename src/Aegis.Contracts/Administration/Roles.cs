namespace Aegis.Contracts.Administration
{
    /// <summary>
    /// Request payload for creating or upserting a role.
    /// </summary>
    public sealed record CreateRoleRequestDto(
        string Name,
        string? Description = null);

    /// <summary>
    /// Read model representing a role.
    /// </summary>
    public sealed record RoleDto(
        string Name,
        string? Description = null);

    /// <summary>
    /// Request payload for assigning a role to a user.
    /// </summary>
    public sealed record AssignRoleToUserRequestDto(string RoleName);
}
