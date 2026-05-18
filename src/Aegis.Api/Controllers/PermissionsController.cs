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
    [Route("api/v1/tenants/{tenantId}/permissions")]
    [Authorize(Policy = AuthorizationPolicies.PermissionApiAccess)]
    public sealed class PermissionsController : ControllerBase
    {
        private readonly IRbacAdminService _rbacAdminService;

        public PermissionsController(IRbacAdminService rbacAdminService)
        {
            _rbacAdminService = rbacAdminService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionDto>>>> List(
            [FromRoute] string tenantId,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<IReadOnlyList<PermissionDto>>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var result = await _rbacAdminService.GetPermissionsAsync(tenantId, cancellationToken);
            return this.OkResponse<IReadOnlyList<PermissionDto>>(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Create(
            [FromRoute] string tenantId,
            [FromBody] CreatePermissionRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<string>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            await _rbacAdminService.CreatePermissionAsync(tenantId, request, cancellationToken);
            return this.OkResponse("created");
        }

        [HttpPost("assign-to-role")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> AssignToRole(
            [FromRoute] string tenantId,
            [FromBody] AssignPermissionToRoleRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<string>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            await _rbacAdminService.AssignPermissionToRoleAsync(tenantId, request, cancellationToken);
            return this.OkResponse("assigned");
        }
    }
}
