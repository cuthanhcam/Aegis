using Aegis.Api.Controllers.Helpers;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/tenants/{tenantId}/presets")]
    public sealed class PresetsController : ControllerBase
    {
        private readonly IPresetAppService _presetAppService;

        public PresetsController(IPresetAppService presetAppService)
        {
            _presetAppService = presetAppService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PresetItemDto>>), StatusCodes.Status200OK)]
        public ActionResult<ApiResponse<IReadOnlyList<PresetItemDto>>> List(
            [FromRoute] string tenantId,
            [FromQuery] string? storeId,
            [FromQuery] string? source,
            [FromQuery] string? scope)
        {
            var result = _presetAppService.List(tenantId, storeId, source, scope);
            return this.OkResponse<IReadOnlyList<PresetItemDto>>(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PresetItemDto>), StatusCodes.Status201Created)]
        public ActionResult<ApiResponse<PresetItemDto>> Upsert(
            [FromRoute] string tenantId,
            [FromBody] UpsertPresetRequestDto request)
        {
            var result = _presetAppService.Upsert(tenantId, request);
            return this.CreatedResponse(result);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public ActionResult<ApiResponse<string>> Delete(
            [FromRoute] string tenantId,
            [FromBody] DeletePresetRequestDto request)
        {
            var deleted = _presetAppService.Delete(tenantId, request);
            return this.DeletedResponse(deleted);
        }

        [HttpGet("meta")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyDictionary<string, PresetMetaDto>>), StatusCodes.Status200OK)]
        public ActionResult<ApiResponse<IReadOnlyDictionary<string, PresetMetaDto>>> GetMeta([FromRoute] string tenantId)
        {
            var result = _presetAppService.GetMeta(tenantId);
            return this.OkResponse<IReadOnlyDictionary<string, PresetMetaDto>>(result);
        }

        [HttpPut("meta")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyDictionary<string, PresetMetaDto>>), StatusCodes.Status200OK)]
        public ActionResult<ApiResponse<IReadOnlyDictionary<string, PresetMetaDto>>> SetMeta(
            [FromRoute] string tenantId,
            [FromBody] SetPresetMetaRequestDto request)
        {
            var result = _presetAppService.SetMeta(tenantId, request.Meta);
            return this.OkResponse<IReadOnlyDictionary<string, PresetMetaDto>>(result);
        }
    }
}
