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
    [Route("api/v1/stores/{storeId}/authorization-models")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class AuthorizationModelsController : ControllerBase
    {
        private readonly IAuthorizationModelAppService _authorizationModelAppService;

        public AuthorizationModelsController(IAuthorizationModelAppService authorizationModelAppService)
        {
            _authorizationModelAppService = authorizationModelAppService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AuthorizationModelDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<AuthorizationModelDto>>>> List(
            [FromRoute] string storeId,
            CancellationToken cancellationToken)
        {
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
            var result = await _authorizationModelAppService.GetLatestAsync(storeId, cancellationToken);
            if (result is null)
            {
                return this.NotFoundResponse<AuthorizationModelDto>("AUTHORIZATION_MODEL_NOT_FOUND", "No authorization model was found.");
            }

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
            var result = await _authorizationModelAppService.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            if (result is null)
            {
                return this.NotFoundResponse<AuthorizationModelDto>("AUTHORIZATION_MODEL_NOT_FOUND", $"Authorization model '{authorizationModelId}' was not found.");
            }

            return this.OkResponse(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<AuthorizationModelDto>), StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<AuthorizationModelDto>>> Create(
            [FromRoute] string storeId,
            [FromBody] CreateAuthorizationModelRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _authorizationModelAppService.CreateAsync(storeId, request, cancellationToken);
            return this.CreatedResponse(result);
        }

        [HttpPut("{authorizationModelId}")]
        [ProducesResponseType(typeof(ApiResponse<AuthorizationModelDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthorizationModelDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<AuthorizationModelDto>>> Update(
            [FromRoute] string storeId,
            [FromRoute] string authorizationModelId,
            [FromBody] CreateAuthorizationModelRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _authorizationModelAppService.UpdateAsync(storeId, authorizationModelId, request, cancellationToken);
            if (result is null)
            {
                return this.NotFoundResponse<AuthorizationModelDto>("AUTHORIZATION_MODEL_NOT_FOUND", $"Authorization model '{authorizationModelId}' was not found.");
            }

            return this.OkResponse(result);
        }

        [HttpDelete("{authorizationModelId}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Delete(
            [FromRoute] string storeId,
            [FromRoute] string authorizationModelId,
            CancellationToken cancellationToken)
        {
            var deleted = await _authorizationModelAppService.DeleteAsync(storeId, authorizationModelId, cancellationToken);
            return this.DeletedResponse(deleted);
        }
    }
}
