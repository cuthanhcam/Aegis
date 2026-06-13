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

        Task UpsertRoleInStoreAsync(
            string tenantId,
            string storeId,
            string roleName,
            string? description,
            CancellationToken cancellationToken = default)
        {
            return UpsertRoleAsync(tenantId, roleName, description, cancellationToken);
        }

        /// <summary>
        /// Retrieves all roles for a tenant.
        /// </summary>
        Task<IReadOnlyList<RoleDto>> GetRolesAsync(
            string tenantId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RoleDto>> GetRolesInStoreAsync(
            string tenantId,
            string storeId,
            CancellationToken cancellationToken = default)
        {
            return GetRolesAsync(tenantId, cancellationToken);
        }

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

        Task UpsertPermissionInStoreAsync(
            string tenantId,
            string storeId,
            string relation,
            string obj,
            string? conditionName = null,
            CancellationToken cancellationToken = default)
        {
            return UpsertPermissionAsync(tenantId, relation, obj, conditionName, cancellationToken);
        }

        /// <summary>
        /// Retrieves all permissions for a tenant.
        /// </summary>
        Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(
            string tenantId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PermissionDto>> GetPermissionsInStoreAsync(
            string tenantId,
            string storeId,
            CancellationToken cancellationToken = default)
        {
            return GetPermissionsAsync(tenantId, cancellationToken);
        }

        /// <summary>
        /// Retrieves one permission by relation/object for a tenant.
        /// </summary>
        Task<PermissionDto?> GetPermissionAsync(
            string tenantId,
            string relation,
            string obj,
            CancellationToken cancellationToken = default);

        Task<PermissionDto?> GetPermissionInStoreAsync(
            string tenantId,
            string storeId,
            string relation,
            string obj,
            CancellationToken cancellationToken = default)
        {
            return GetPermissionAsync(tenantId, relation, obj, cancellationToken);
        }

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

        Task AssignPermissionToRoleInStoreAsync(
            string tenantId,
            string storeId,
            string roleName,
            string relation,
            string obj,
            string? conditionName = null,
            CancellationToken cancellationToken = default)
        {
            return AssignPermissionToRoleAsync(tenantId, roleName, relation, obj, conditionName, cancellationToken);
        }

        // User-Role Assignments
        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        Task AssignRoleToUserAsync(
            string tenantId,
            string userId,
            string roleName,
            CancellationToken cancellationToken = default);

        Task AssignRoleToUserInStoreAsync(
            string tenantId,
            string storeId,
            string userId,
            string roleName,
            CancellationToken cancellationToken = default)
        {
            return AssignRoleToUserAsync(tenantId, userId, roleName, cancellationToken);
        }

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

        Task<UserRolesDto> GetUserRolesInStoreAsync(
            string tenantId,
            string storeId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            return GetUserRolesAsync(tenantId, userId, cancellationToken);
        }
    }
}
