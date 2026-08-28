using Aegis.Contracts.Administration;

namespace Aegis.Application.Interfaces;

public interface IRbacAdminService
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDto>> GetRolesInStoreAsync(
        string tenantId,
        string storeId,
        CancellationToken cancellationToken = default);

    Task CreateRoleAsync(
        string tenantId,
        CreateRoleRequestDto request,
        CancellationToken cancellationToken = default);

    Task CreateRoleInStoreAsync(
        string tenantId,
        string storeId,
        CreateRoleRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionDto>> GetPermissionsInStoreAsync(
        string tenantId,
        string storeId,
        CancellationToken cancellationToken = default);

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
        CancellationToken cancellationToken = default);

    Task CreatePermissionAsync(
        string tenantId,
        CreatePermissionRequestDto request,
        CancellationToken cancellationToken = default);

    Task CreatePermissionInStoreAsync(
        string tenantId,
        string storeId,
        CreatePermissionRequestDto request,
        CancellationToken cancellationToken = default);

    Task AssignPermissionToRoleAsync(
        string tenantId,
        AssignPermissionToRoleRequestDto request,
        CancellationToken cancellationToken = default);

    Task AssignPermissionToRoleInStoreAsync(
        string tenantId,
        string storeId,
        AssignPermissionToRoleRequestDto request,
        CancellationToken cancellationToken = default);

    Task AssignRoleToUserAsync(
        string tenantId,
        string userId,
        AssignRoleToUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task AssignRoleToUserInStoreAsync(
        string tenantId,
        string storeId,
        string userId,
        AssignRoleToUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetUsersAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<UserRolesDto> GetUserRolesAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<UserRolesDto> GetUserRolesInStoreAsync(
        string tenantId,
        string storeId,
        string userId,
        CancellationToken cancellationToken = default);
}
