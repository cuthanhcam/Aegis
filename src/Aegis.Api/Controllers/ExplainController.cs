using Aegis.Api.Controllers.Helpers;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Common;
using Aegis.Contracts.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/explain")]
    public sealed class ExplainController : ControllerBase
    {
        private readonly IPermissionAppService _permissionAppService;

        public ExplainController(IPermissionAppService permissionAppService)
        {
            _permissionAppService = permissionAppService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CheckResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<CheckResponseDto>>> Explain(
            [FromQuery] string tenantId,
            [FromBody] CheckRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _permissionAppService.ExplainAsync(tenantId, request, cancellationToken);
            return this.OkResponse(result);
        }
    }
}
