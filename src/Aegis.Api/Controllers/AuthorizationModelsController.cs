using Aegis.Api.Controllers.Helpers;
using Aegis.Api.Security;
using Aegis.Application.Interfaces;
using Aegis.Application.Features.AuthorizationModels;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stores/{storeId}/authorization-models")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class AuthorizationModelsController : ControllerBase
    {
        private readonly IAuthorizationModelAppService _authorizationModelAppService;
        private readonly IStoreRegistry _storeRegistry;
        private readonly CreateAuthorizationModelUseCase _createAuthorizationModelUseCase;
        private readonly UpdateAuthorizationModelUseCase _updateAuthorizationModelUseCase;
        private readonly DeleteAuthorizationModelUseCase _deleteAuthorizationModelUseCase;
        private readonly PublishAuthorizationModelUseCase _publishAuthorizationModelUseCase;
        private readonly RollbackAuthorizationModelUseCase _rollbackAuthorizationModelUseCase;

        public AuthorizationModelsController(
            IAuthorizationModelAppService authorizationModelAppService,
            IStoreRegistry storeRegistry,
            CreateAuthorizationModelUseCase createAuthorizationModelUseCase,
            UpdateAuthorizationModelUseCase updateAuthorizationModelUseCase,
            DeleteAuthorizationModelUseCase deleteAuthorizationModelUseCase,
            PublishAuthorizationModelUseCase publishAuthorizationModelUseCase,
            RollbackAuthorizationModelUseCase rollbackAuthorizationModelUseCase)
        {
            _authorizationModelAppService = authorizationModelAppService;
            _storeRegistry = storeRegistry;
            _createAuthorizationModelUseCase = createAuthorizationModelUseCase;
            _updateAuthorizationModelUseCase = updateAuthorizationModelUseCase;
            _deleteAuthorizationModelUseCase = deleteAuthorizationModelUseCase;
            _publishAuthorizationModelUseCase = publishAuthorizationModelUseCase;
            _rollbackAuthorizationModelUseCase = rollbackAuthorizationModelUseCase;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AuthorizationModelDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<AuthorizationModelDto>>>> List(
            [FromRoute] string storeId,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<IReadOnlyList<AuthorizationModelDto>>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var result = await _authorizationModelAppService.ListAsync(storeId, cancellationToken);
            return this.OkResponse<IReadOnlyList<AuthorizationModelDto>>(result);
        }

        [HttpGet("latest")]
        [ProducesResponseType(typeof(ApiResponse<AuthorizationModelDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthorizationModelDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<AuthorizationModelDto>>> GetLatest(
            [FromRoute] string storeId,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<AuthorizationModelDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var result = await _authorizationModelAppService.GetLatestAsync(storeId, cancellationToken);
            if (result is null)
            {
                return this.NotFoundResponse<AuthorizationModelDto>(NativeErrorCodes.AuthorizationModelNotFound, "No authorization model was found.");
            }

            Response.Headers.ETag = EntityTagPreconditions.Format(result.Revision);
            return this.OkResponse(result);
        }

        [HttpGet("{authorizationModelId}")]
        [ProducesResponseType(typeof(ApiResponse<AuthorizationModelDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthorizationModelDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<AuthorizationModelDto>>> Get(
            [FromRoute] string storeId,
            [FromRoute] string authorizationModelId,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<AuthorizationModelDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var result = await _authorizationModelAppService.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            if (result is null)
            {
                return this.NotFoundResponse<AuthorizationModelDto>(NativeErrorCodes.AuthorizationModelNotFound, $"Authorization model '{authorizationModelId}' was not found.");
            }

            Response.Headers.ETag = EntityTagPreconditions.Format(result.Revision);
            return this.OkResponse(result);
        }

        [HttpPost("{authorizationModelId}/publish")]
        [ProducesResponseType(typeof(ApiResponse<PublishAuthorizationModelResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PublishAuthorizationModelResponseDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status412PreconditionFailed)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status428PreconditionRequired)]
        public async Task<ActionResult<ApiResponse<PublishAuthorizationModelResponseDto>>> Publish(
            [FromRoute] string storeId,
            [FromRoute] string authorizationModelId,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<PublishAuthorizationModelResponseDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var expectedRevision = EntityTagPreconditions.RequireRevision(ifMatch);
            var result = await _publishAuthorizationModelUseCase.ExecuteAsync(storeId, authorizationModelId, expectedRevision, cancellationToken);
            if (result is null)
            {
                return this.NotFoundResponse<PublishAuthorizationModelResponseDto>(NativeErrorCodes.AuthorizationModelNotFound, $"Authorization model '{authorizationModelId}' was not found.");
            }

            Response.Headers.ETag = EntityTagPreconditions.Format(result.PublishedModel.Revision);
            return this.OkResponse(result);
        }

        [HttpPost("{authorizationModelId}/rollback")]
        [ProducesResponseType(typeof(ApiResponse<RollbackAuthorizationModelResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<RollbackAuthorizationModelResponseDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status412PreconditionFailed)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status428PreconditionRequired)]
        public async Task<ActionResult<ApiResponse<RollbackAuthorizationModelResponseDto>>> Rollback(
            [FromRoute] string storeId,
            [FromRoute] string authorizationModelId,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<RollbackAuthorizationModelResponseDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var expectedRevision = EntityTagPreconditions.RequireRevision(ifMatch);
            var result = await _rollbackAuthorizationModelUseCase.ExecuteAsync(storeId, authorizationModelId, expectedRevision, cancellationToken);
            if (result is null)
            {
                return this.NotFoundResponse<RollbackAuthorizationModelResponseDto>(NativeErrorCodes.AuthorizationModelNotFound, $"Authorization model '{authorizationModelId}' was not found.");
            }

            Response.Headers.ETag = EntityTagPreconditions.Format(result.ActiveModel.Revision);
            return this.OkResponse(result);
        }

        [HttpGet("{leftAuthorizationModelId}/diff/{rightAuthorizationModelId}")]
        [ProducesResponseType(typeof(ApiResponse<AuthorizationModelDiffDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthorizationModelDiffDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<AuthorizationModelDiffDto>>> Diff(
            [FromRoute] string storeId,
            [FromRoute] string leftAuthorizationModelId,
            [FromRoute] string rightAuthorizationModelId,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<AuthorizationModelDiffDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var result = await _authorizationModelAppService.DiffAsync(storeId, leftAuthorizationModelId, rightAuthorizationModelId, cancellationToken);
            if (result is null)
            {
                return this.NotFoundResponse<AuthorizationModelDiffDto>(NativeErrorCodes.AuthorizationModelNotFound, "One or both authorization models were not found.");
            }

            return this.OkResponse(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<AuthorizationModelDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResponse<AuthorizationModelDto>>> Create(
            [FromRoute] string storeId,
            [FromBody] CreateAuthorizationModelRequestDto request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<AuthorizationModelDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var validatedKey = IdempotencyHeaders.Validate(idempotencyKey);
            AuthorizationModelDto result;
            if (validatedKey is null)
            {
                result = await _createAuthorizationModelUseCase.ExecuteAsync(storeId, request, cancellationToken);
            }
            else
            {
                var tenantId = TenantAccessGuard.ResolveTenantId(User)!;
                var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException("Authenticated subject identifier is required for idempotent mutations.");
                result = await _createAuthorizationModelUseCase.ExecuteIdempotentAsync(
                    storeId,
                    request,
                    tenantId,
                    actorId,
                    validatedKey,
                    IdempotencyHeaders.Fingerprint(request),
                    cancellationToken);
            }
            Response.Headers.ETag = EntityTagPreconditions.Format(result.Revision);
            return this.CreatedResponse(result);
        }

        [HttpPost("validate")]
        [ProducesResponseType(typeof(ApiResponse<AuthorizationModelValidationResultDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<AuthorizationModelValidationResultDto>>> Validate(
            [FromRoute] string storeId,
            [FromBody] ValidateAuthorizationModelRequestDto request,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<AuthorizationModelValidationResultDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var result = await _authorizationModelAppService.ValidateAsync(request, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPut("{authorizationModelId}")]
        [ProducesResponseType(typeof(ApiResponse<AuthorizationModelDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthorizationModelDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status412PreconditionFailed)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status428PreconditionRequired)]
        public async Task<ActionResult<ApiResponse<AuthorizationModelDto>>> Update(
            [FromRoute] string storeId,
            [FromRoute] string authorizationModelId,
            [FromBody] CreateAuthorizationModelRequestDto request,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<AuthorizationModelDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var expectedRevision = EntityTagPreconditions.RequireRevision(ifMatch);
            var result = await _updateAuthorizationModelUseCase.ExecuteAsync(storeId, authorizationModelId, request, expectedRevision, cancellationToken);
            if (result is null)
            {
                return this.NotFoundResponse<AuthorizationModelDto>(NativeErrorCodes.AuthorizationModelNotFound, $"Authorization model '{authorizationModelId}' was not found.");
            }

            Response.Headers.ETag = EntityTagPreconditions.Format(result.Revision);
            return this.OkResponse(result);
        }

        [HttpDelete("{authorizationModelId}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status412PreconditionFailed)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status428PreconditionRequired)]
        public async Task<ActionResult<ApiResponse<string>>> Delete(
            [FromRoute] string storeId,
            [FromRoute] string authorizationModelId,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<string>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var expectedRevision = EntityTagPreconditions.RequireRevision(ifMatch);
            var deleted = await _deleteAuthorizationModelUseCase.ExecuteAsync(storeId, authorizationModelId, expectedRevision, cancellationToken);
            return this.DeletedResponse(deleted);
        }
    }
}
