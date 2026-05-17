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
                var tenantId = context.User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    tenantId = context.Request.Headers["X-Tenant-Id"].ToString();
                }

                if (!isAuthenticated && string.IsNullOrWhiteSpace(tenantId))
                {
                    await _next(context);
                    return;
                }

                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";
                    var payload = ApiResponse<string>.Fail("TENANT_REQUIRED", "Tenant context is required from JWT claim tenant_id or X-Tenant-Id header.");
                    await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                    return;
                }

                context.Items[TenantIdKey] = tenantId;
            }

            await _next(context);
        }
    }
}
