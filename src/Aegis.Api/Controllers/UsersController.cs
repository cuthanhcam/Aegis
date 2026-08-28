using Aegis.Api.Controllers.Helpers;
using Aegis.Api.Security;
using Aegis.Application.Features.Users;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/tenants/{tenantId}/users")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class UsersController : ControllerBase
    {
        private readonly IRbacAdminService _rbacAdminService;
        private readonly CreateUserUseCase _createUserUseCase;
        private readonly UpdateUserUseCase _updateUserUseCase;
        private readonly DeleteUserUseCase _deleteUserUseCase;

        public UsersController(
            IRbacAdminService rbacAdminService,
            CreateUserUseCase createUserUseCase,
            UpdateUserUseCase updateUserUseCase,
            DeleteUserUseCase deleteUserUseCase)
        {
            _rbacAdminService = rbacAdminService;
            _createUserUseCase = createUserUseCase;
            _updateUserUseCase = updateUserUseCase;
            _deleteUserUseCase = deleteUserUseCase;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<UserDto>>>> List(
            [FromRoute] string tenantId,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<IReadOnlyList<UserDto>>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var result = await _rbacAdminService.GetUsersAsync(tenantId, cancellationToken);
            return this.OkResponse<IReadOnlyList<UserDto>>(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<UserDto>>> Create(
            [FromRoute] string tenantId,
            [FromBody] CreateUserRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<UserDto>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var result = await _createUserUseCase.ExecuteAsync(tenantId, request, cancellationToken);
            return this.CreatedResponse(result);
        }

        [HttpPut("{userId}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<UserDto>>> Update(
            [FromRoute] string tenantId,
            [FromRoute] string userId,
            [FromBody] UpdateUserRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<UserDto>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var result = await _updateUserUseCase.ExecuteAsync(tenantId, userId, request, cancellationToken);
            if (result is null)
            {
                return this.NotFoundResponse<UserDto>(NativeErrorCodes.UserNotFound, $"User '{userId}' was not found.");
            }

            return this.OkResponse(result);
        }

        [HttpDelete("{userId}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Delete(
            [FromRoute] string tenantId,
            [FromRoute] string userId,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<string>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var deleted = await _deleteUserUseCase.ExecuteAsync(tenantId, userId, cancellationToken);
            return this.DeletedResponse(deleted);
        }

        [HttpGet("{userId}/roles")]
        [ProducesResponseType(typeof(ApiResponse<UserRolesDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<UserRolesDto>>> GetRoles(
            [FromRoute] string tenantId,
            [FromRoute] string userId,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<UserRolesDto>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var result = await _rbacAdminService.GetUserRolesAsync(tenantId, userId, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("{userId}/roles")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> AssignRole(
            [FromRoute] string tenantId,
            [FromRoute] string userId,
            [FromBody] AssignRoleToUserRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<string>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            await _rbacAdminService.AssignRoleToUserAsync(tenantId, userId, request, cancellationToken);
            return this.OkResponse("assigned");
        }
    }
}
