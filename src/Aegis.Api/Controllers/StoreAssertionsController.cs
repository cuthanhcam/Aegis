using Aegis.Api.Controllers.Helpers;
using Aegis.Api.Security;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stores/{storeId}/assertions")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class StoreAssertionsController : ControllerBase
    {
        private readonly IAssertionAppService _assertionAppService;
        private readonly IStoreRegistry _storeRegistry;

        public StoreAssertionsController(
            IAssertionAppService assertionAppService,
            IStoreRegistry storeRegistry)
        {
            _assertionAppService = assertionAppService;
            _storeRegistry = storeRegistry;
        }

        [HttpGet("{authorizationModelId}")]
        [ProducesResponseType(typeof(ApiResponse<AegisCompatReadAssertionsResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<AegisCompatReadAssertionsResponseDto>>> Read(
            [FromRoute] string storeId,
            [FromRoute] string authorizationModelId,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<AegisCompatReadAssertionsResponseDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var result = await _assertionAppService.ReadAsync(storeId, authorizationModelId, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("{authorizationModelId}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Write(
            [FromRoute] string storeId,
            [FromRoute] string authorizationModelId,
            [FromBody] AegisCompatWriteAssertionsRequestDto request,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<string>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            await _assertionAppService.WriteAsync(storeId, authorizationModelId, request, cancellationToken);
            return this.OkResponse("written");
        }
    }
}
