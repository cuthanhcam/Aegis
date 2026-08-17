using Aegis.Api.Controllers.Helpers;
using Aegis.Api.Middlewares;
using Aegis.Api.Security;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Common;
using Aegis.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/explain")]
    [Authorize(Policy = AuthorizationPolicies.PermissionApiAccess)]
    public sealed class ExplainController : ControllerBase
    {
        private readonly IPermissionAppService _permissionAppService;

        public ExplainController(IPermissionAppService permissionAppService)
        {
            _permissionAppService = permissionAppService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CheckResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<CheckResponseDto>>> Explain(
            [FromQuery] string? tenantId,
            [FromBody] CheckRequestDto request,
            CancellationToken cancellationToken)
        {
            var accessResult = TenantAccessGuard.ValidateContextTenant<CheckResponseDto>(this, tenantId, ResolveContextTenantId());
            if (accessResult is not null)
            {
                return accessResult;
            }

            var resolvedTenantResult = ResolveTenantId(tenantId);
            if (resolvedTenantResult.ErrorResult is not null)
            {
                return resolvedTenantResult.ErrorResult;
            }

            var result = await _permissionAppService.ExplainAsync(resolvedTenantResult.TenantId!, request, cancellationToken);
            return this.OkResponse(result);
        }

        private (string? TenantId, ActionResult<ApiResponse<CheckResponseDto>>? ErrorResult) ResolveTenantId(string? queryTenantId)
        {
            var contextualTenantId = ResolveContextTenantId();

            if (!string.IsNullOrWhiteSpace(contextualTenantId)
                && !string.IsNullOrWhiteSpace(queryTenantId)
                && !contextualTenantId.Equals(queryTenantId, StringComparison.OrdinalIgnoreCase))
            {
                return (
                    null,
                    BadRequest(ApiResponse<CheckResponseDto>.Fail(
                        NativeErrorCodes.TenantMismatch,
                        "Tenant in query does not match authenticated/header tenant context.")));
            }

            var effectiveTenantId = !string.IsNullOrWhiteSpace(contextualTenantId)
                ? contextualTenantId
                : queryTenantId;

            if (string.IsNullOrWhiteSpace(effectiveTenantId))
            {
                return (
                    null,
                    BadRequest(ApiResponse<CheckResponseDto>.Fail(
                        NativeErrorCodes.TenantRequired,
                        "Tenant context is required from JWT/header context or tenantId query parameter.")));
            }

            return (effectiveTenantId, null);
        }

        private string? ResolveContextTenantId()
        {
            return HttpContext.Items.TryGetValue(TenantContextMiddleware.TenantIdKey, out var tenantContext)
                ? tenantContext as string
                : null;
        }
    }
}
