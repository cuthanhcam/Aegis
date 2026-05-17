using Aegis.Api.Controllers.Helpers;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/tenants/{tenantId}/audit")]
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
            var result = await _permissionAppService.QueryAuditAsync(tenantId, action, decision, cancellationToken);
            return this.OkResponse<IReadOnlyList<AuditEventDto>>(result);
        }
    }
}
