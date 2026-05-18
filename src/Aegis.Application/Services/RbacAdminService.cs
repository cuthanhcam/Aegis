using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Domain.ValueObjects;

namespace Aegis.Application.Services
{
    public sealed class RbacAdminService : IRbacAdminService
    {
        private readonly IRbacAdminStore _rbacAdminStore;

        public RbacAdminService(IRbacAdminStore rbacAdminStore)
        {
            _rbacAdminStore = rbacAdminStore;
        }

        public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return await _rbacAdminStore.GetRolesAsync(tenantId, cancellationToken);
        }

        public Task CreateRoleAsync(string tenantId, CreateRoleRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Role name is required.");
            }

            return _rbacAdminStore.UpsertRoleAsync(tenantId, request.Name, request.Description, cancellationToken);
        }

        public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return await _rbacAdminStore.GetPermissionsAsync(tenantId, cancellationToken);
        }

        public Task CreatePermissionAsync(string tenantId, CreatePermissionRequestDto request, CancellationToken cancellationToken = default)
        {
            ValidatePermission(request.Relation, request.Object);
            return _rbacAdminStore.UpsertPermissionAsync(tenantId, request.Relation, request.Object, request.ConditionName, cancellationToken);
        }

        public Task AssignPermissionToRoleAsync(string tenantId, AssignPermissionToRoleRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.RoleName))
            {
                throw new ArgumentException("Role name is required.");
            }

            ValidatePermission(request.Relation, request.Object);
            return _rbacAdminStore.AssignPermissionToRoleAsync(tenantId, request.RoleName, request.Relation, request.Object, request.ConditionName, cancellationToken);
        }

        public Task AssignRoleToUserAsync(string tenantId, string userId, AssignRoleToUserRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!ObjectId.TryCreate(userId, out _) || !userId.StartsWith("user:", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("userId must be in user:<id> format.");
            }

            if (string.IsNullOrWhiteSpace(request.RoleName))
            {
                throw new ArgumentException("Role name is required.");
            }

            return _rbacAdminStore.AssignRoleToUserAsync(tenantId, userId, request.RoleName, cancellationToken);
        }

        public async Task<IReadOnlyList<UserDto>> GetUsersAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return await _rbacAdminStore.GetUsersAsync(tenantId, cancellationToken);
        }

        public async Task<UserDto> CreateUserAsync(string tenantId, CreateUserRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                throw new ArgumentException("User ID is required.");
            }

            return await _rbacAdminStore.CreateUserAsync(
                tenantId,
                request.UserId,
                request.Email,
                request.DisplayName,
                cancellationToken);
        }

        public async Task<UserDto?> UpdateUserAsync(string tenantId, string userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID is required.");
            }

            var updated = await _rbacAdminStore.UpdateUserAsync(tenantId, userId, request.Email, request.DisplayName, cancellationToken);
            if (!updated)
                return null;

            return await _rbacAdminStore.GetUserAsync(tenantId, userId, cancellationToken);
        }

        public Task<bool> DeleteUserAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID is required.");
            }

            return _rbacAdminStore.DeleteUserAsync(tenantId, userId, cancellationToken);
        }

        public async Task<UserRolesDto> GetUserRolesAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID is required.");
            }

            return await _rbacAdminStore.GetUserRolesAsync(tenantId, userId, cancellationToken);
        }

        private static void ValidatePermission(string relation, string objectRef)
        {
            if (!RelationName.TryCreate(relation, out _) || !ObjectId.TryCreate(objectRef, out _))
            {
                throw new ArgumentException("Permission requires non-empty relation and object in <type>:<id> format.");
            }
        }
    }
}
