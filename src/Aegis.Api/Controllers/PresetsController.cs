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
    [Route("api/v1/tenants/{tenantId}/presets")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class PresetsController : ControllerBase
    {
        private readonly IPresetAppService _presetAppService;
        private readonly IStoreRegistry _storeRegistry;

        public PresetsController(IPresetAppService presetAppService, IStoreRegistry storeRegistry)
        {
            _presetAppService = presetAppService;
            _storeRegistry = storeRegistry;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PresetItemDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<PresetItemDto>>>> List(
            [FromRoute] string tenantId,
            [FromQuery] string? storeId,
            [FromQuery] string? source,
            [FromQuery] string? scope,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<IReadOnlyList<PresetItemDto>>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var storeAccessResult = await ValidateOptionalStoreAsync<IReadOnlyList<PresetItemDto>>(tenantId, storeId, cancellationToken);
            if (storeAccessResult is not null)
            {
                return storeAccessResult;
            }

            var result = _presetAppService.List(tenantId, storeId, source, scope);
            return this.OkResponse<IReadOnlyList<PresetItemDto>>(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PresetItemDto>), StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<PresetItemDto>>> Upsert(
            [FromRoute] string tenantId,
            [FromBody] UpsertPresetRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<PresetItemDto>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var storeAccessResult = await ValidateOptionalStoreAsync<PresetItemDto>(tenantId, request.StoreId, cancellationToken);
            if (storeAccessResult is not null)
            {
                return storeAccessResult;
            }

            var result = _presetAppService.Upsert(tenantId, request);
            return this.CreatedResponse(result);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Delete(
            [FromRoute] string tenantId,
            [FromBody] DeletePresetRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<string>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var storeAccessResult = await ValidateOptionalStoreAsync<string>(tenantId, request.StoreId, cancellationToken);
            if (storeAccessResult is not null)
            {
                return storeAccessResult;
            }

            var deleted = _presetAppService.Delete(tenantId, request);
            return this.DeletedResponse(deleted);
        }

        [HttpGet("meta")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyDictionary<string, PresetMetaDto>>), StatusCodes.Status200OK)]
        public ActionResult<ApiResponse<IReadOnlyDictionary<string, PresetMetaDto>>> GetMeta([FromRoute] string tenantId)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<IReadOnlyDictionary<string, PresetMetaDto>>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var result = _presetAppService.GetMeta(tenantId);
            return this.OkResponse<IReadOnlyDictionary<string, PresetMetaDto>>(result);
        }

        [HttpPut("meta")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyDictionary<string, PresetMetaDto>>), StatusCodes.Status200OK)]
        public ActionResult<ApiResponse<IReadOnlyDictionary<string, PresetMetaDto>>> SetMeta(
            [FromRoute] string tenantId,
            [FromBody] SetPresetMetaRequestDto request)
        {
            var accessResult = TenantAccessGuard.ValidateRouteTenant<IReadOnlyDictionary<string, PresetMetaDto>>(this, tenantId);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var result = _presetAppService.SetMeta(tenantId, request.Meta);
            return this.OkResponse<IReadOnlyDictionary<string, PresetMetaDto>>(result);
        }

        private async Task<ActionResult<ApiResponse<T>>?> ValidateOptionalStoreAsync<T>(
            string tenantId,
            string? storeId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                return null;
            }

            var store = await _storeRegistry.GetForTenantAsync(tenantId, storeId, cancellationToken);
            if (store is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.Fail(NativeErrorCodes.StoreForbidden, "Store does not belong to the requested tenant."));
            }

            return null;
        }
    }
}
