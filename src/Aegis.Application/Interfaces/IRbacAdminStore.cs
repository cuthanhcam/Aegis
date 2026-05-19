using Aegis.Contracts.Administration;

namespace Aegis.Application.Interfaces
{
    /// <summary>
    /// Application boundary for RBAC administration persistence operations.
    /// </summary>
    public interface IRbacAdminStore
    {
        // Roles
        /// <summary>
        /// Creates or updates a role for a tenant.
        /// </summary>
        Task UpsertRoleAsync(
            string tenantId,
            string roleName,
            string? description,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all roles for a tenant.
        /// </summary>
        Task<IReadOnlyList<RoleDto>> GetRolesAsync(
            string tenantId,
            CancellationToken cancellationToken = default);

        // Permissions
        /// <summary>
        /// Creates or updates a permission for a tenant.
        /// </summary>
        Task UpsertPermissionAsync(
            string tenantId,
            string relation,
            string obj,
            string? conditionName = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all permissions for a tenant.
        /// </summary>
        Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(
            string tenantId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves one permission by relation/object for a tenant.
        /// </summary>
        Task<PermissionDto?> GetPermissionAsync(
            string tenantId,
            string relation,
            string obj,
            CancellationToken cancellationToken = default);

        // Role-Permission Assignments
        /// <summary>
        /// Assigns a permission to a role.
        /// </summary>
        Task AssignPermissionToRoleAsync(
            string tenantId,
            string roleName,
            string relation,
            string obj,
            string? conditionName = null,
            CancellationToken cancellationToken = default);

        // User-Role Assignments
        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        Task AssignRoleToUserAsync(
            string tenantId,
            string userId,
            string roleName,
            CancellationToken cancellationToken = default);

        // User
        /// <summary>
        /// Creates a new user and returns created profile data.
        /// </summary>
        Task<UserDto> CreateUserAsync(
            string tenantId,
            string userId,
            string? email,
            string? displayName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all users for a tenant.
        /// </summary>
        Task<IReadOnlyList<UserDto>> GetUsersAsync(
            string tenantId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a user by identifier.
        /// </summary>
        Task<UserDto?> GetUserAsync(
            string tenantId,
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates mutable user fields.
        /// </summary>
        Task<bool> UpdateUserAsync(
            string tenantId,
            string userId,
            string? email,
            string? displayName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a user and related assignments.
        /// </summary>
        Task<bool> DeleteUserAsync(
            string tenantId,
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves role assignments of a user.
        /// </summary>
        Task<UserRolesDto> GetUserRolesAsync(
            string tenantId,
            string userId,
            CancellationToken cancellationToken = default);
    }
}
