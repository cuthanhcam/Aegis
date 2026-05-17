using Aegis.Api.Controllers.Helpers;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Common;
using Aegis.Contracts.Relationships;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stores/{storeId}/relationships")]
    public sealed class StoreRelationshipsController : ControllerBase
    {
        private readonly IRelationshipService _relationshipAppService;

        public StoreRelationshipsController(IRelationshipService relationshipAppService)
        {
            _relationshipAppService = relationshipAppService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RelationshipTupleDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<RelationshipTupleDto>>>> Query(
            [FromRoute] string storeId,
            [FromQuery] string? subject,
            [FromQuery] string? relation,
            [FromQuery(Name = "object")] string? objectRef,
            [FromQuery] string? effect,
            CancellationToken cancellationToken)
        {
            var result = await _relationshipAppService.QueryAsync(storeId, subject, relation, objectRef, effect, cancellationToken);
            return this.OkResponse<IReadOnlyList<RelationshipTupleDto>>(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Upsert(
            [FromRoute] string storeId,
            [FromBody] RelationshipWriteRequestDto request,
            CancellationToken cancellationToken)
        {
            await _relationshipAppService.UpsertAsync(storeId, request, cancellationToken);
            return this.OkResponse("upserted");
        }

        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Delete(
            [FromRoute] string storeId,
            [FromBody] RelationshipDeleteRequestDto request,
            CancellationToken cancellationToken)
        {
            var deleted = await _relationshipAppService.DeleteAsync(storeId, request, cancellationToken);
            return this.DeletedResponse(deleted);
        }
    }
}
