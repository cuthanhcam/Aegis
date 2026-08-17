using Aegis.Api.Controllers.Helpers;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Aegis.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/tenants/{tenantId}/audit")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class AuditController : ControllerBase
    {
        private readonly IPermissionAppService _permissionAppService;
        private readonly IStoreRegistry _storeRegistry;

        public AuditController(IPermissionAppService permissionAppService, IStoreRegistry storeRegistry)
        {
            _permissionAppService = permissionAppService;
            _storeRegistry = storeRegistry;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AuditEventDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<AuditEventDto>>>> Query(
            [FromRoute] string tenantId,
            [FromQuery] string? action,
            [FromQuery] string? decision,
            [FromQuery] string? storeId,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<IReadOnlyList<AuditEventDto>>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            if (!string.IsNullOrWhiteSpace(storeId))
            {
                var store = await _storeRegistry.GetForTenantAsync(tenantId, storeId, cancellationToken);
                if (store is null)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<IReadOnlyList<AuditEventDto>>.Fail(NativeErrorCodes.StoreForbidden, "Store does not belong to the requested tenant."));
                }
            }

            var result = await _permissionAppService.QueryAuditAsync(tenantId, action, decision, storeId, cancellationToken);
            return this.OkResponse<IReadOnlyList<AuditEventDto>>(result);
        }
    }
}
