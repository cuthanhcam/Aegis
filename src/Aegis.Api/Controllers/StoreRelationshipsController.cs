using Aegis.Api.Controllers.Helpers;
using Aegis.Api.Security;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Common;
using Aegis.Contracts.Relationships;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stores/{storeId}/relationships")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class StoreRelationshipsController : ControllerBase
    {
        private readonly IRelationshipService _relationshipAppService;
        private readonly IStoreRegistry _storeRegistry;

        public StoreRelationshipsController(IRelationshipService relationshipAppService, IStoreRegistry storeRegistry)
        {
            _relationshipAppService = relationshipAppService;
            _storeRegistry = storeRegistry;
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
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<IReadOnlyList<RelationshipTupleDto>>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _relationshipAppService.QueryAsync(tenantId, storeId, subject, relation, objectRef, effect, cancellationToken);
            return this.OkResponse<IReadOnlyList<RelationshipTupleDto>>(result);
        }

        [HttpGet("changes")]
        [ProducesResponseType(typeof(ApiResponse<ReadChangesResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ReadChangesResponseDto>>> ReadChanges(
            [FromRoute] string storeId,
            [FromQuery(Name = "page_size"), Range(1, ApiRequestLimits.MaxPageSize)] int? pageSize,
            [FromQuery(Name = "continuation_token"), StringLength(ApiRequestLimits.MaxContinuationTokenLength)] string? continuationToken,
            [FromQuery, StringLength(ApiRequestLimits.MaxResourceTypeFilterLength)] string? type,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<ReadChangesResponseDto>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var result = await _relationshipAppService.ReadChangesAsync(
                tenantId,
                storeId,
                new ReadChangesRequestDto(pageSize, continuationToken, type),
                cancellationToken);

            return this.OkResponse(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Upsert(
            [FromRoute] string storeId,
            [FromBody] RelationshipWriteRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<string>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            await _relationshipAppService.UpsertAsync(tenantId, storeId, request, cancellationToken);
            return this.OkResponse("upserted");
        }

        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Delete(
            [FromRoute] string storeId,
            [FromBody] RelationshipDeleteRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = await TenantAccessGuard.ValidateStoreTenantAsync<string>(this, _storeRegistry, storeId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }

            var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
            var deleted = await _relationshipAppService.DeleteAsync(tenantId, storeId, request, cancellationToken);
            return this.DeletedResponse(deleted);
        }
    }
}
