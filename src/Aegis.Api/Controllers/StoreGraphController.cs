using Aegis.Api.Controllers.Helpers;
using Aegis.Api.Security;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Aegis.Contracts.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stores/{storeId}/graph")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class StoreGraphController : ControllerBase
    {
        private readonly IAuthorizationQueryAppService _authorizationQueryAppService;
        private readonly IStoreRegistry _storeRegistry;

        public StoreGraphController(IAuthorizationQueryAppService authorizationQueryAppService, IStoreRegistry storeRegistry)
        {
            _authorizationQueryAppService = authorizationQueryAppService;
            _storeRegistry = storeRegistry;
        }

        [HttpPost("list-users")]
        [ProducesResponseType(typeof(ApiResponse<ListUsersResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ListUsersResponseDto>>> ListUsers(
            [FromRoute] string storeId,
            [FromBody] ListUsersRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<ListUsersResponseDto>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _authorizationQueryAppService.ListUsersAsync(tenantId, storeId, request, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("list-objects")]
        [ProducesResponseType(typeof(ApiResponse<ListObjectsResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ListObjectsResponseDto>>> ListObjects(
            [FromRoute] string storeId,
            [FromBody] ListObjectsRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<ListObjectsResponseDto>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _authorizationQueryAppService.ListObjectsAsync(tenantId, storeId, request, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("expand")]
        [ProducesResponseType(typeof(ApiResponse<ExpandNodeDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ExpandNodeDto>>> Expand(
            [FromRoute] string storeId,
            [FromBody] ExpandRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<ExpandNodeDto>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _authorizationQueryAppService.ExpandAsync(tenantId, storeId, request, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("list-users/compat")]
        [ProducesResponseType(typeof(AegisCompatListUsersResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<AegisCompatListUsersResponseDto>> ListUsersCompat(
            [FromRoute] string storeId,
            [FromBody] AegisCompatListUsersRequestDto request,
            CancellationToken cancellationToken)
        {
            var storeAccessResult = await ValidateCompatStoreTenantAsync(storeId, cancellationToken);
            if (storeAccessResult is not null)
            {
                return storeAccessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _authorizationQueryAppService.ListUsersAegisCompatAsync(tenantId, storeId, request, cancellationToken);
            return Ok(result);
        }

        [HttpPost("list-objects/compat")]
        [ProducesResponseType(typeof(AegisCompatListObjectsResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<AegisCompatListObjectsResponseDto>> ListObjectsCompat(
            [FromRoute] string storeId,
            [FromBody] AegisCompatListObjectsRequestDto request,
            CancellationToken cancellationToken)
        {
            var storeAccessResult = await ValidateCompatStoreTenantAsync(storeId, cancellationToken);
            if (storeAccessResult is not null)
            {
                return storeAccessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _authorizationQueryAppService.ListObjectsAegisCompatAsync(tenantId, storeId, request, cancellationToken);
            return Ok(result);
        }

        [HttpPost("expand/compat")]
        [ProducesResponseType(typeof(AegisCompatExpandResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<AegisCompatExpandResponseDto>> ExpandCompat(
            [FromRoute] string storeId,
            [FromBody] AegisCompatExpandRequestDto request,
            CancellationToken cancellationToken)
        {
            var storeAccessResult = await ValidateCompatStoreTenantAsync(storeId, cancellationToken);
            if (storeAccessResult is not null)
            {
                return storeAccessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _authorizationQueryAppService.ExpandAegisCompatAsync(tenantId, storeId, request, cancellationToken);
            return Ok(result);
        }

        private async Task<ActionResult?> ValidateCompatStoreTenantAsync(string storeId, CancellationToken cancellationToken)
        {
            var tenantId = TenantAccessGuard.ResolveTenantId(User);
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new AegisCompatErrorResponseDto("tenant_forbidden", "Tenant claim is required."));
            }

            var store = await _storeRegistry.GetForTenantAsync(tenantId, storeId, cancellationToken);
            if (store is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new AegisCompatErrorResponseDto("store_forbidden", "Store does not belong to the authenticated tenant."));
            }

            return null;
        }
    }
}
