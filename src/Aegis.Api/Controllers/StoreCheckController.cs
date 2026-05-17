using Aegis.Api.Controllers.Helpers;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Common;
using Aegis.Contracts.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stores/{storeId}")]
    public sealed class StoreCheckController : ControllerBase
    {
        private readonly IPermissionAppService _permissionAppService;

        public StoreCheckController(IPermissionAppService permissionAppService)
        {
            _permissionAppService = permissionAppService;
        }

        [HttpPost("check")]
        [ProducesResponseType(typeof(ApiResponse<CheckResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<CheckResponseDto>>> Check(
            [FromRoute] string storeId,
            [FromBody] CheckRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _permissionAppService.CheckAsync(storeId, request, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("explain")]
        [ProducesResponseType(typeof(ApiResponse<CheckResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<CheckResponseDto>>> Explain(
            [FromRoute] string storeId,
            [FromBody] CheckRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _permissionAppService.ExplainAsync(storeId, request, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("batch-check")]
        [ProducesResponseType(typeof(ApiResponse<BatchCheckResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<BatchCheckResponseDto>>> BatchCheck(
            [FromRoute] string storeId,
            [FromBody] BatchCheckRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _permissionAppService.BatchCheckInStoreAsync(storeId, request, cancellationToken);
            return this.OkResponse(result);
        }
    }
}
