using Aegis.Api.Controllers.Helpers;
using Aegis.Api.Security;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stores/{storeId}/users")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class StoreUsersController : ControllerBase
    {
        private readonly IRbacAdminService _rbacAdminService;
        private readonly IStoreRegistry _storeRegistry;

        public StoreUsersController(IRbacAdminService rbacAdminService, IStoreRegistry storeRegistry)
        {
            _rbacAdminService = rbacAdminService;
            _storeRegistry = storeRegistry;
        }

        [HttpGet("{userId}/roles")]
        [ProducesResponseType(typeof(ApiResponse<UserRolesDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<UserRolesDto>>> GetRoles(
            [FromRoute] string storeId,
            [FromRoute] string userId,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<UserRolesDto>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _rbacAdminService.GetUserRolesInStoreAsync(tenantId, storeId, userId, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("{userId}/roles")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> AssignRole(
            [FromRoute] string storeId,
            [FromRoute] string userId,
            [FromBody] AssignRoleToUserRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<string>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            await _rbacAdminService.AssignRoleToUserInStoreAsync(tenantId, storeId, userId, request, cancellationToken);
            return this.OkResponse("assigned");
        }
    }
}
