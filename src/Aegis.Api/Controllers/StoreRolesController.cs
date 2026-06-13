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
    [Route("api/v1/stores/{storeId}/roles")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class StoreRolesController : ControllerBase
    {
        private readonly IRbacAdminService _rbacAdminService;
        private readonly IStoreRegistry _storeRegistry;

        public StoreRolesController(IRbacAdminService rbacAdminService, IStoreRegistry storeRegistry)
        {
            _rbacAdminService = rbacAdminService;
            _storeRegistry = storeRegistry;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleDto>>>> List(
            [FromRoute] string storeId,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<IReadOnlyList<RoleDto>>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _rbacAdminService.GetRolesInStoreAsync(tenantId, storeId, cancellationToken);
            return this.OkResponse<IReadOnlyList<RoleDto>>(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Create(
            [FromRoute] string storeId,
            [FromBody] CreateRoleRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<string>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            await _rbacAdminService.CreateRoleInStoreAsync(tenantId, storeId, request, cancellationToken);
            return this.OkResponse("created");
        }

        [HttpPost("assign-permission")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> AssignPermission(
            [FromRoute] string storeId,
            [FromBody] AssignPermissionToRoleRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<string>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            await _rbacAdminService.AssignPermissionToRoleInStoreAsync(tenantId, storeId, request, cancellationToken);
            return this.OkResponse("assigned");
        }
    }
}
