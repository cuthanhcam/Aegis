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
    [Route("api/v1/stores/{storeId}/permissions")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class StorePermissionsController : ControllerBase
    {
        private readonly IRbacAdminService _rbacAdminService;
        private readonly IStoreRegistry _storeRegistry;

        public StorePermissionsController(IRbacAdminService rbacAdminService, IStoreRegistry storeRegistry)
        {
            _rbacAdminService = rbacAdminService;
            _storeRegistry = storeRegistry;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionDto>>>> List(
            [FromRoute] string storeId,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<IReadOnlyList<PermissionDto>>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _rbacAdminService.GetPermissionsInStoreAsync(tenantId, storeId, cancellationToken);
            return this.OkResponse<IReadOnlyList<PermissionDto>>(result);
        }

        [HttpGet("resolve")]
        [ProducesResponseType(typeof(ApiResponse<PermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<PermissionDto>>> Get(
            [FromRoute] string storeId,
            [FromQuery] string relation,
            [FromQuery(Name = "object")] string objectRef,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<PermissionDto>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _rbacAdminService.GetPermissionInStoreAsync(tenantId, storeId, relation, objectRef, cancellationToken);
            if (result is null)
            {
                return this.NotFoundResponse<PermissionDto>(NativeErrorCodes.PermissionNotFound, "Permission was not found.");
            }

            return this.OkResponse(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Create(
            [FromRoute] string storeId,
            [FromBody] CreatePermissionRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<string>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            await _rbacAdminService.CreatePermissionInStoreAsync(tenantId, storeId, request, cancellationToken);
            return this.OkResponse("created");
        }

        [HttpPost("assign-to-role")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> AssignToRole(
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
