using Aegis.Contracts.Administration;

namespace Aegis.Application.Interfaces;

public interface IRbacAdminService
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task CreateRoleAsync(
        string tenantId,
        CreateRoleRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task CreatePermissionAsync(
        string tenantId,
        CreatePermissionRequestDto request,
        CancellationToken cancellationToken = default);

    Task AssignPermissionToRoleAsync(
        string tenantId,
        AssignPermissionToRoleRequestDto request,
        CancellationToken cancellationToken = default);

    Task AssignRoleToUserAsync(
        string tenantId,
        string userId,
        AssignRoleToUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetUsersAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<UserDto> CreateUserAsync(
        string tenantId,
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task<UserDto?> UpdateUserAsync(
        string tenantId,
        string userId,
        UpdateUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteUserAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<UserRolesDto> GetUserRolesAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default);
}
