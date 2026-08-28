using Aegis.Api.Controllers.Helpers;
using Aegis.Api.Security;
using Aegis.Application.Interfaces;
using Aegis.Application.Features.Stores;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stores")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class StoresController : ControllerBase
    {
        private readonly IStoreAppService _storeAppService;
        private readonly CreateStoreUseCase _createStoreUseCase;

        public StoresController(IStoreAppService storeAppService, CreateStoreUseCase createStoreUseCase)
        {
            _storeAppService = storeAppService;
            _createStoreUseCase = createStoreUseCase;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StoreDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<StoreDto>>>> List(CancellationToken cancellationToken)
        {
            var tenantId = TenantAccessGuard.ResolveTenantId(User);
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<IReadOnlyList<StoreDto>>.Fail(NativeErrorCodes.TenantForbidden, "Tenant claim is required."));
            }

            var result = await _storeAppService.ListAsync(tenantId, cancellationToken);
            return this.OkResponse<IReadOnlyList<StoreDto>>(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<StoreDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResponse<StoreDto>>> Create(
            [FromBody] CreateStoreRequestDto request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            var tenantId = TenantAccessGuard.ResolveTenantId(User);
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<StoreDto>.Fail(NativeErrorCodes.TenantForbidden, "Tenant claim is required."));
            }

            var validatedKey = IdempotencyHeaders.Validate(idempotencyKey);
            StoreDto result;
            if (validatedKey is null)
            {
                result = await _createStoreUseCase.ExecuteAsync(tenantId, request, cancellationToken);
            }
            else
            {
                var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException("Authenticated subject identifier is required for idempotent mutations.");
                result = await _createStoreUseCase.ExecuteIdempotentAsync(
                    tenantId,
                    request,
                    actorId,
                    validatedKey,
                    IdempotencyHeaders.Fingerprint(request),
                    cancellationToken);
            }
            return this.CreatedResponse(result);
        }

        [HttpGet("{storeId}")]
        [ProducesResponseType(typeof(ApiResponse<StoreDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<StoreDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<StoreDto>>> Get(
            [FromRoute] string storeId,
            CancellationToken cancellationToken)
        {
            var tenantId = TenantAccessGuard.ResolveTenantId(User);
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<StoreDto>.Fail(NativeErrorCodes.TenantForbidden, "Tenant claim is required."));
            }

            var result = await _storeAppService.GetByIdAsync(tenantId, storeId, cancellationToken);
            if (result is null)
            {
                return this.NotFoundResponse<StoreDto>(NativeErrorCodes.StoreNotFound, $"Store '{storeId}' was not found.");
            }

            return this.OkResponse(result);
        }

        [HttpDelete("{storeId}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Delete(
            [FromRoute] string storeId,
            CancellationToken cancellationToken)
        {
            var tenantId = TenantAccessGuard.ResolveTenantId(User);
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<string>.Fail(NativeErrorCodes.TenantForbidden, "Tenant claim is required."));
            }

            var deleted = await _storeAppService.DeleteAsync(tenantId, storeId, cancellationToken);
            return this.DeletedResponse(deleted);
        }
    }
}
