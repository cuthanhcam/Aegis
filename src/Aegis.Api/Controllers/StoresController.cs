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
    [Route("api/v1/stores")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class StoresController : ControllerBase
    {
        private readonly IStoreAppService _storeAppService;

        public StoresController(IStoreAppService storeAppService)
        {
            _storeAppService = storeAppService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StoreDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<StoreDto>>>> List(CancellationToken cancellationToken)
        {
            var result = await _storeAppService.ListAsync(cancellationToken);
            return this.OkResponse<IReadOnlyList<StoreDto>>(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<StoreDto>), StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<StoreDto>>> Create(
            [FromBody] CreateStoreRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _storeAppService.CreateAsync(request, cancellationToken);
            return this.CreatedResponse(result);
        }

        [HttpGet("{storeId}")]
        [ProducesResponseType(typeof(ApiResponse<StoreDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<StoreDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<StoreDto>>> Get(
            [FromRoute] string storeId,
            CancellationToken cancellationToken)
        {
            var result = await _storeAppService.GetByIdAsync(storeId, cancellationToken);
            if (result is null)
            {
                return this.NotFoundResponse<StoreDto>("STORE_NOT_FOUND", $"Store '{storeId}' was not found.");
            }

            return this.OkResponse(result);
        }

        [HttpDelete("{storeId}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Delete(
            [FromRoute] string storeId,
            CancellationToken cancellationToken)
        {
            var deleted = await _storeAppService.DeleteAsync(storeId, cancellationToken);
            return this.DeletedResponse(deleted);
        }
    }
}
