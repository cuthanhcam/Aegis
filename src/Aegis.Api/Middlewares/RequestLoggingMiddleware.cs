using System.Diagnostics;
using System.Security.Claims;

namespace Aegis.Api.Middlewares
{
    public sealed class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startedAt = Stopwatch.GetTimestamp();

            try
            {
                await _next(context);
            }
            finally
            {
                var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
                var endpointName = context.GetEndpoint()?.DisplayName ?? "unmatched endpoint";
                var tenantId = GetTenantId(context) ?? "-";
                var userId = GetUserId(context) ?? "anonymous";
                var requestId = context.TraceIdentifier;
                var traceId = Activity.Current?.TraceId.ToString() ?? requestId;
                var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var statusCode = context.Response.StatusCode;
                var logLevel = statusCode >= StatusCodes.Status500InternalServerError
                    ? LogLevel.Error
                    : statusCode >= StatusCodes.Status400BadRequest
                        ? LogLevel.Warning
                        : LogLevel.Information;

                _logger.Log(
                    logLevel,
                    "HTTP {Method} {Path}{QueryString} => {StatusCode} in {ElapsedMs:0.00} ms | endpoint={Endpoint} | tenant={TenantId} | user={UserId} | trace={TraceId} | requestId={RequestId} | remoteIp={RemoteIp}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.QueryString,
                    statusCode,
                    elapsedMs,
                    endpointName,
                    tenantId,
                    userId,
                    traceId,
                    requestId,
                    remoteIp);
            }
        }

        private static string? GetTenantId(HttpContext context)
        {
            if (context.Items.TryGetValue(TenantContextMiddleware.TenantIdKey, out var tenantContext) && tenantContext is string tenantId && !string.IsNullOrWhiteSpace(tenantId))
            {
                return tenantId;
            }

            var headerTenantId = context.Request.Headers["X-Tenant-Id"].ToString();
            if (!string.IsNullOrWhiteSpace(headerTenantId))
            {
                return headerTenantId;
            }

            return context.User.FindFirst("tenant_id")?.Value
                ?? context.User.FindFirst("tid")?.Value;
        }

        private static string? GetUserId(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub")
                ?? context.User.FindFirstValue("preferred_username")
                ?? context.User.FindFirstValue("name")
                ?? context.User.Identity?.Name;
        }
    }
}
