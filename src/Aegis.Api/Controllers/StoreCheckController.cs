using Aegis.Api.Controllers.Helpers;
using Aegis.Api.Security;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Compatibility;
using Aegis.Contracts.Common;
using Aegis.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stores/{storeId}")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class StoreCheckController : ControllerBase
    {
        private readonly IPermissionAppService _permissionAppService;
        private readonly IStoreRegistry _storeRegistry;

        public StoreCheckController(IPermissionAppService permissionAppService, IStoreRegistry storeRegistry)
        {
            _permissionAppService = permissionAppService;
            _storeRegistry = storeRegistry;
        }

        [HttpPost("check")]
        [ProducesResponseType(typeof(ApiResponse<CheckResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<CheckResponseDto>>> Check(
            [FromRoute] string storeId,
            [FromBody] CheckRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<CheckResponseDto>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _permissionAppService.CheckInStoreAsync(
                tenantId,
                storeId,
                new StoreCheckRequestDto(
                    request.Subject,
                    request.Relation,
                    request.Object,
                    request.ContextualTuples,
                    request.Consistency,
                    request.AuthorizationModelId,
                    request.Context),
                cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("explain")]
        [ProducesResponseType(typeof(ApiResponse<CheckResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<CheckResponseDto>>> Explain(
            [FromRoute] string storeId,
            [FromBody] CheckRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<CheckResponseDto>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _permissionAppService.ExplainInStoreAsync(
                tenantId,
                storeId,
                new StoreCheckRequestDto(
                    request.Subject,
                    request.Relation,
                    request.Object,
                    request.ContextualTuples,
                    request.Consistency,
                    request.AuthorizationModelId,
                    request.Context),
                cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("batch-check")]
        [ProducesResponseType(typeof(ApiResponse<BatchCheckResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<BatchCheckResponseDto>>> BatchCheck(
            [FromRoute] string storeId,
            [FromBody] BatchCheckRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<BatchCheckResponseDto>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _permissionAppService.BatchCheckInStoreAsync(tenantId, storeId, request, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("check/compat")]
        [ProducesResponseType(typeof(AegisCompatCheckResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<AegisCompatCheckResponseDto>> CheckCompat(
            [FromRoute] string storeId,
            [FromBody] AegisCompatCheckRequestDto request,
            CancellationToken cancellationToken)
        {
            var storeAccessResult = await TenantAccessGuard.ValidateCompatStoreTenantAsync<AegisCompatCheckResponseDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccessResult is not null)
            {
                return storeAccessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _permissionAppService.CheckAegisCompatInStoreAsync(tenantId, storeId, request, cancellationToken);
            return Ok(result);
        }

        [HttpPost("batch-check/compat")]
        [ProducesResponseType(typeof(AegisCompatBatchCheckResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<AegisCompatBatchCheckResponseDto>> BatchCheckCompat(
            [FromRoute] string storeId,
            [FromBody] AegisCompatBatchCheckRequestDto request,
            CancellationToken cancellationToken)
        {
            var storeAccessResult = await TenantAccessGuard.ValidateCompatStoreTenantAsync<AegisCompatBatchCheckResponseDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccessResult is not null)
            {
                return storeAccessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _permissionAppService.BatchCheckAegisCompatInStoreAsync(tenantId, storeId, request, cancellationToken);
            return Ok(result);
        }
    }
}
