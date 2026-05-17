using Aegis.Api.Controllers.Helpers;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/tenants/{tenantId}/roles")]
    public sealed class RolesController : ControllerBase
    {
        private readonly IRbacAdminService _rbacAdminService;

        public RolesController(IRbacAdminService rbacAdminService)
        {
            _rbacAdminService = rbacAdminService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleDto>>>> List(
            [FromRoute] string tenantId,
            CancellationToken cancellationToken)
        {
            var result = await _rbacAdminService.GetRolesAsync(tenantId, cancellationToken);
            return this.OkResponse<IReadOnlyList<RoleDto>>(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Create(
            [FromRoute] string tenantId,
            [FromBody] CreateRoleRequestDto request,
            CancellationToken cancellationToken)
        {
            await _rbacAdminService.CreateRoleAsync(tenantId, request, cancellationToken);
            return this.OkResponse("created");
        }

        [HttpPost("assign-permission")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> AssignPermission(
            [FromRoute] string tenantId,
            [FromBody] AssignPermissionToRoleRequestDto request,
            CancellationToken cancellationToken)
        {
            await _rbacAdminService.AssignPermissionToRoleAsync(tenantId, request, cancellationToken);
            return this.OkResponse("assigned");
        }
    }
}
