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
                var errorCode = context.Items.TryGetValue("Aegis.ErrorCode", out var errorCodeValue)
                    ? errorCodeValue?.ToString() ?? "-"
                    : "-";
                var logLevel = statusCode >= StatusCodes.Status500InternalServerError
                    ? LogLevel.Error
                    : statusCode >= StatusCodes.Status400BadRequest
                        ? LogLevel.Warning
                        : LogLevel.Information;

                _logger.Log(
                    logLevel,
                    "HTTP request completed method={Method} path={Path}{QueryString} status={StatusCode} duration_ms={ElapsedMs:0.00} endpoint={Endpoint} tenant={TenantId} user={UserId} trace_id={TraceId} request_id={RequestId} remote_ip={RemoteIp} error_code={ErrorCode}",
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
                    remoteIp,
                    errorCode);
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
