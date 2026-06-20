using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Aegis.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aegis.Api.Security
{
    public static class TenantAccessGuard
    {
        private static readonly string[] TenantClaimTypes = ["tenant_id", "tid"];

        public static string? ResolveTenantId(ClaimsPrincipal user)
        {
            return TenantClaimTypes
                .Select(claimType => user.FindFirst(claimType)?.Value)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }

        public static ActionResult<ApiResponse<T>>? ValidateRouteTenant<T>(ControllerBase controller, string routeTenantId)
        {
            var tenantId = ResolveTenantId(controller.User);
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return controller.StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.Fail("TENANT_FORBIDDEN", "Tenant claim is required."));
            }

            if (!tenantId.Equals(routeTenantId, StringComparison.OrdinalIgnoreCase))
            {
                return controller.StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.Fail("TENANT_FORBIDDEN", "Tenant claim does not match the requested tenant."));
            }

            return null;
        }

        public static ActionResult<ApiResponse<T>>? ValidateContextTenant<T>(ControllerBase controller, string? queryTenantId, string? contextualTenantId)
        {
            if (!string.IsNullOrWhiteSpace(queryTenantId)
                && !string.IsNullOrWhiteSpace(contextualTenantId)
                && !contextualTenantId.Equals(queryTenantId, StringComparison.OrdinalIgnoreCase))
            {
                return controller.BadRequest(ApiResponse<T>.Fail("TENANT_MISMATCH", "Tenant in query does not match authenticated/header tenant context."));
            }

            var claimTenantId = ResolveTenantId(controller.User);
            if (string.IsNullOrWhiteSpace(claimTenantId))
            {
                return controller.StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.Fail("TENANT_FORBIDDEN", "Tenant claim is required."));
            }

            if (!string.IsNullOrWhiteSpace(queryTenantId)
                && !claimTenantId.Equals(queryTenantId, StringComparison.OrdinalIgnoreCase))
            {
                return controller.StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.Fail("TENANT_FORBIDDEN", "Tenant claim does not match the requested tenant."));
            }

            if (!string.IsNullOrWhiteSpace(contextualTenantId)
                && !claimTenantId.Equals(contextualTenantId, StringComparison.OrdinalIgnoreCase))
            {
                return controller.StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.Fail("TENANT_FORBIDDEN", "Tenant claim does not match the authenticated tenant context."));
            }

            return null;
        }

        public static async Task<ActionResult<ApiResponse<T>>?> ValidateStoreTenantAsync<T>(
            ControllerBase controller,
            IStoreRegistry storeRegistry,
            string storeId,
            CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId(controller.User);
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return controller.StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.Fail("TENANT_FORBIDDEN", "Tenant claim is required."));
            }

            var store = await storeRegistry.GetForTenantAsync(tenantId, storeId, cancellationToken);
            if (store is null)
            {
                return controller.StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.Fail("STORE_FORBIDDEN", "Store does not belong to the authenticated tenant."));
            }

            return null;
        }

        public static async Task<ActionResult<T>?> ValidateCompatStoreTenantAsync<T>(
            ControllerBase controller,
            IStoreRegistry storeRegistry,
            string storeId,
            CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId(controller.User);
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return controller.StatusCode(
                    StatusCodes.Status403Forbidden,
                    new AegisCompatErrorResponseDto("tenant_forbidden", "Tenant claim is required."));
            }

            var store = await storeRegistry.GetForTenantAsync(tenantId, storeId, cancellationToken);
            if (store is null)
            {
                return controller.StatusCode(
                    StatusCodes.Status403Forbidden,
                    new AegisCompatErrorResponseDto("store_forbidden", "Store does not belong to the authenticated tenant."));
            }

            return null;
        }
    }
}
