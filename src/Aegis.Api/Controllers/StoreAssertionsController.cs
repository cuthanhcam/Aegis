using Aegis.Api.Controllers.Helpers;
using Aegis.Api.Security;
using Aegis.Application.Features.Assertions;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stores/{storeId}/assertions")]
    [Authorize(Policy = AuthorizationPolicies.ManagementApiAccess)]
    public sealed class StoreAssertionsController : ControllerBase
    {
        private readonly IStoreRegistry _storeRegistry;
        private readonly ReadAssertionsUseCase _readAssertionsUseCase;
        private readonly WriteAssertionsUseCase _writeAssertionsUseCase;
        private readonly RunAssertionsUseCase _runAssertionsUseCase;
        private readonly ListAssertionRunsUseCase _listAssertionRunsUseCase;
        private readonly GetAssertionRunUseCase _getAssertionRunUseCase;
        private readonly GenerateAssertionsFromAuditUseCase _generateAssertionsFromAuditUseCase;

        public StoreAssertionsController(
            IStoreRegistry storeRegistry,
            ReadAssertionsUseCase readAssertionsUseCase,
            WriteAssertionsUseCase writeAssertionsUseCase,
            RunAssertionsUseCase runAssertionsUseCase,
            ListAssertionRunsUseCase listAssertionRunsUseCase,
            GetAssertionRunUseCase getAssertionRunUseCase,
            GenerateAssertionsFromAuditUseCase generateAssertionsFromAuditUseCase)
        {
            _storeRegistry = storeRegistry;
            _readAssertionsUseCase = readAssertionsUseCase;
            _writeAssertionsUseCase = writeAssertionsUseCase;
            _runAssertionsUseCase = runAssertionsUseCase;
            _listAssertionRunsUseCase = listAssertionRunsUseCase;
            _getAssertionRunUseCase = getAssertionRunUseCase;
            _generateAssertionsFromAuditUseCase = generateAssertionsFromAuditUseCase;
        }

        [HttpGet("{authorizationModelId}")]
        [ProducesResponseType(typeof(ApiResponse<AegisCompatReadAssertionsResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<AegisCompatReadAssertionsResponseDto>>> Read(
            [FromRoute] string storeId,
            [FromRoute] string authorizationModelId,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<AegisCompatReadAssertionsResponseDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var result = await _readAssertionsUseCase.ExecuteAsync(storeId, authorizationModelId, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpPost("{authorizationModelId}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Write(
            [FromRoute] string storeId,
            [FromRoute] string authorizationModelId,
            [FromBody] AegisCompatWriteAssertionsRequestDto request,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<string>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            await _writeAssertionsUseCase.ExecuteAsync(storeId, authorizationModelId, request, cancellationToken);
            return this.OkResponse("written");
        }

        [HttpPost("{authorizationModelId}/run")]
        [ProducesResponseType(typeof(ApiResponse<AegisAssertionRunRecordDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<AegisAssertionRunRecordDto>>> Run(
            [FromRoute] string storeId,
            [FromRoute] string authorizationModelId,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<AegisAssertionRunRecordDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var result = await _runAssertionsUseCase.ExecuteAsync(storeId, authorizationModelId, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpGet("{authorizationModelId}/runs")]
        [ProducesResponseType(typeof(ApiResponse<AegisAssertionRunListResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<AegisAssertionRunListResponseDto>>> ListRuns(
            [FromRoute] string storeId,
            [FromRoute] string authorizationModelId,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<AegisAssertionRunListResponseDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var result = await _listAssertionRunsUseCase.ExecuteAsync(storeId, authorizationModelId, cancellationToken);
            return this.OkResponse(result);
        }

        [HttpGet("runs/{runId}")]
        [ProducesResponseType(typeof(ApiResponse<AegisAssertionRunRecordDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AegisAssertionRunRecordDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<AegisAssertionRunRecordDto>>> GetRun(
            [FromRoute] string storeId,
            [FromRoute] string runId,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<AegisAssertionRunRecordDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var result = await _getAssertionRunUseCase.ExecuteAsync(storeId, runId, cancellationToken);
            if (result is null)
            {
                return this.NotFoundResponse<AegisAssertionRunRecordDto>(NativeErrorCodes.AssertionRunNotFound, $"Assertion run '{runId}' was not found.");
            }

            return this.OkResponse(result);
        }

        [HttpPost("{authorizationModelId}/generate-from-audit")]
        [ProducesResponseType(typeof(ApiResponse<AegisGenerateAssertionsFromAuditResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<AegisGenerateAssertionsFromAuditResponseDto>>> GenerateFromAudit(
            [FromRoute] string storeId,
            [FromRoute] string authorizationModelId,
            [FromBody] AegisGenerateAssertionsFromAuditRequestDto request,
            CancellationToken cancellationToken)
        {
            var storeAccess = await TenantAccessGuard.ValidateStoreTenantAsync<AegisGenerateAssertionsFromAuditResponseDto>(this, _storeRegistry, storeId, cancellationToken);
            if (storeAccess is not null)
            {
                return storeAccess;
            }

            var result = await _generateAssertionsFromAuditUseCase.ExecuteAsync(
                storeId,
                authorizationModelId,
                request,
                cancellationToken);
            return this.OkResponse(result);
        }
    }
}
