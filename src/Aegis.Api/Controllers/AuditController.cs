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

        public AuditController(IPermissionAppService permissionAppService)
        {
            _permissionAppService = permissionAppService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AuditEventDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<AuditEventDto>>>> Query(
            [FromRoute] string tenantId,
            [FromQuery] string? action,
            [FromQuery] string? decision,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<IReadOnlyList<AuditEventDto>>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var result = await _permissionAppService.QueryAuditAsync(tenantId, action, decision, cancellationToken);
            return this.OkResponse<IReadOnlyList<AuditEventDto>>(result);
        }
    }
}
