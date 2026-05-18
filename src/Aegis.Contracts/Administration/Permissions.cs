namespace Aegis.Contracts.Administration
{
    /// <summary>
    /// Request payload for creating or upserting a permission.
    /// </summary>
    public sealed record CreatePermissionRequestDto(
        string Relation,
        string Object,
        string? ConditionName = null);

    /// <summary>
    /// Read model representing a permission entry.
    /// </summary>
    public sealed record PermissionDto(
        string Relation,
        string Object,
        string? ConditionName = null);

    /// <summary>
    /// Request payload for assigning a permission to a role.
    /// </summary>
    public sealed record AssignPermissionToRoleRequestDto(
        string RoleName,
        string Relation,
        string Object,
        string? ConditionName = null);
}
