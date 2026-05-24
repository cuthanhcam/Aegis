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

        public StoreGraphController(IAuthorizationQueryAppService authorizationQueryAppService)
        {
            _authorizationQueryAppService = authorizationQueryAppService;
        }

        [HttpPost("list-users")]
        [ProducesResponseType(typeof(ApiResponse<ListUsersResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ListUsersResponseDto>>> ListUsers(
            [FromRoute] string storeId,
            [FromBody] ListUsersRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<ListUsersResponseDto>(this, storeId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var result = await _authorizationQueryAppService.ListUsersAsync(storeId, request, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("list-objects")]
        [ProducesResponseType(typeof(ApiResponse<ListObjectsResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ListObjectsResponseDto>>> ListObjects(
            [FromRoute] string storeId,
            [FromBody] ListObjectsRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<ListObjectsResponseDto>(this, storeId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var result = await _authorizationQueryAppService.ListObjectsAsync(storeId, request, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("expand")]
        [ProducesResponseType(typeof(ApiResponse<ExpandNodeDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ExpandNodeDto>>> Expand(
            [FromRoute] string storeId,
            [FromBody] ExpandRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<ExpandNodeDto>(this, storeId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var result = await _authorizationQueryAppService.ExpandAsync(storeId, request, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("list-users/compat")]
        [ProducesResponseType(typeof(AegisCompatListUsersResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<AegisCompatListUsersResponseDto>> ListUsersCompat(
            [FromRoute] string storeId,
            [FromBody] AegisCompatListUsersRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenantResult<AegisCompatListUsersResponseDto>(this, storeId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var result = await _authorizationQueryAppService.ListUsersAegisCompatAsync(storeId, request, cancellationToken);
            return Ok(result);
        }

        [HttpPost("list-objects/compat")]
        [ProducesResponseType(typeof(AegisCompatListObjectsResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<AegisCompatListObjectsResponseDto>> ListObjectsCompat(
            [FromRoute] string storeId,
            [FromBody] AegisCompatListObjectsRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenantResult<AegisCompatListObjectsResponseDto>(this, storeId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var result = await _authorizationQueryAppService.ListObjectsAegisCompatAsync(storeId, request, cancellationToken);
            return Ok(result);
        }

        [HttpPost("expand/compat")]
        [ProducesResponseType(typeof(AegisCompatExpandResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<AegisCompatExpandResponseDto>> ExpandCompat(
            [FromRoute] string storeId,
            [FromBody] AegisCompatExpandRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenantResult<AegisCompatExpandResponseDto>(this, storeId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var result = await _authorizationQueryAppService.ExpandAegisCompatAsync(storeId, request, cancellationToken);
            return Ok(result);
        }
    }
}
