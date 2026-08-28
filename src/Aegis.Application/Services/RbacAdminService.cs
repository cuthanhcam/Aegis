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

        public async Task<IReadOnlyList<RoleDto>> GetRolesInStoreAsync(string tenantId, string storeId, CancellationToken cancellationToken = default)
        {
            return await _rbacAdminStore.GetRolesInStoreAsync(tenantId, storeId, cancellationToken);
        }

        public Task CreateRoleAsync(string tenantId, CreateRoleRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Role name is required.");
            }

            return _rbacAdminStore.UpsertRoleAsync(tenantId, request.Name, request.Description, cancellationToken);
        }

        public Task CreateRoleInStoreAsync(string tenantId, string storeId, CreateRoleRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Role name is required.");
            }

            return _rbacAdminStore.UpsertRoleInStoreAsync(tenantId, storeId, request.Name, request.Description, cancellationToken);
        }

        public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return await _rbacAdminStore.GetPermissionsAsync(tenantId, cancellationToken);
        }

        public async Task<IReadOnlyList<PermissionDto>> GetPermissionsInStoreAsync(string tenantId, string storeId, CancellationToken cancellationToken = default)
        {
            return await _rbacAdminStore.GetPermissionsInStoreAsync(tenantId, storeId, cancellationToken);
        }

        public async Task<PermissionDto?> GetPermissionAsync(string tenantId, string relation, string obj, CancellationToken cancellationToken = default)
        {
            ValidatePermission(relation, obj);
            return await _rbacAdminStore.GetPermissionAsync(tenantId, relation, obj, cancellationToken);
        }

        public async Task<PermissionDto?> GetPermissionInStoreAsync(string tenantId, string storeId, string relation, string obj, CancellationToken cancellationToken = default)
        {
            ValidatePermission(relation, obj);
            return await _rbacAdminStore.GetPermissionInStoreAsync(tenantId, storeId, relation, obj, cancellationToken);
        }

        public Task CreatePermissionAsync(string tenantId, CreatePermissionRequestDto request, CancellationToken cancellationToken = default)
        {
            ValidatePermission(request.Relation, request.Object);
            return _rbacAdminStore.UpsertPermissionAsync(tenantId, request.Relation, request.Object, request.ConditionName, cancellationToken);
        }

        public Task CreatePermissionInStoreAsync(string tenantId, string storeId, CreatePermissionRequestDto request, CancellationToken cancellationToken = default)
        {
            ValidatePermission(request.Relation, request.Object);
            return _rbacAdminStore.UpsertPermissionInStoreAsync(tenantId, storeId, request.Relation, request.Object, request.ConditionName, cancellationToken);
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

        public Task AssignPermissionToRoleInStoreAsync(string tenantId, string storeId, AssignPermissionToRoleRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.RoleName))
            {
                throw new ArgumentException("Role name is required.");
            }

            ValidatePermission(request.Relation, request.Object);
            return _rbacAdminStore.AssignPermissionToRoleInStoreAsync(tenantId, storeId, request.RoleName, request.Relation, request.Object, request.ConditionName, cancellationToken);
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

        public Task AssignRoleToUserInStoreAsync(string tenantId, string storeId, string userId, AssignRoleToUserRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!ObjectId.TryCreate(userId, out _) || !userId.StartsWith("user:", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("userId must be in user:<id> format.");
            }

            if (string.IsNullOrWhiteSpace(request.RoleName))
            {
                throw new ArgumentException("Role name is required.");
            }

            return _rbacAdminStore.AssignRoleToUserInStoreAsync(tenantId, storeId, userId, request.RoleName, cancellationToken);
        }

        public async Task<IReadOnlyList<UserDto>> GetUsersAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return await _rbacAdminStore.GetUsersAsync(tenantId, cancellationToken);
        }

        public async Task<UserRolesDto> GetUserRolesAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID is required.");
            }

            return await _rbacAdminStore.GetUserRolesAsync(tenantId, userId, cancellationToken);
        }

        public async Task<UserRolesDto> GetUserRolesInStoreAsync(string tenantId, string storeId, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID is required.");
            }

            return await _rbacAdminStore.GetUserRolesInStoreAsync(tenantId, storeId, userId, cancellationToken);
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
