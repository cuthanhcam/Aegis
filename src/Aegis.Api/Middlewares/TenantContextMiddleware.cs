using Aegis.Contracts.Common;
using System.Text.Json;

namespace Aegis.Api.Middlewares
{
    public sealed class TenantContextMiddleware
    {
        public const string TenantIdKey = "Aegis.TenantId";

        private readonly RequestDelegate _next;

        public TenantContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/api/v1/auth", StringComparison.OrdinalIgnoreCase)
                && !context.Request.Path.StartsWithSegments("/api/v1/stores", StringComparison.OrdinalIgnoreCase))
            {
                var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
                var tenantId = context.Request.Headers["X-Tenant-Id"].ToString();
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    tenantId = context.User.FindFirst("tenant_id")?.Value;
                }

                if (!isAuthenticated && string.IsNullOrWhiteSpace(tenantId))
                {
                    await _next(context);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    context.Items[TenantIdKey] = tenantId;
                }
            }

            await _next(context);
        }
    }
}
