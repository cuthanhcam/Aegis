using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using System.Text.Json;

namespace Aegis.Api.Middlewares
{
    /// <summary>
    /// Centralized exception-to-response handler for consistent error envelopes.
    /// Routes compatibility endpoints to AegisCompatErrorResponseDto and native endpoints to ApiResponse.
    /// </summary>
    public sealed class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (CompatibilityApiException ex)
            {
                await WriteErrorAsync(context, ex.StatusCode, ex.ErrorCode, ex.Message);
            }
            catch (ArgumentException ex)
            {
                await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "validation_error", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "invalid_operation", ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                await WriteErrorAsync(context, StatusCodes.Status404NotFound, "not_found", ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                await WriteErrorAsync(context, StatusCodes.Status403Forbidden, "permission_denied", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}: {ExceptionType}",
                    context.Request.Method, context.Request.Path, ex.GetType().Name);
                await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "internal_error", "Unexpected error occurred.");
            }
        }

        /// <summary>
        /// Writes error response with appropriate envelope based on request path.
        /// Compatibility endpoints: AegisCompatErrorResponseDto (flat envelope)
        /// Native endpoints: ApiResponse<T>.Fail (nested envelope)
        /// </summary>
        private static Task WriteErrorAsync(HttpContext context, int statusCode, string code, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            if (IsAegisCompatCompatibilityPath(context.Request.Path))
            {
                return context.Response.WriteAsync(
                    JsonSerializer.Serialize(new AegisCompatErrorResponseDto(code.ToLowerInvariant(), message)));
            }

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(ApiResponse<string>.Fail(code, message)));
        }

        /// <summary>
        /// Detects if request is to a compatibility (OpenFGA-like) endpoint.
        /// These endpoints live under /api/v1/stores/{storeId}/ and should emit
        /// AegisCompatErrorResponseDto for backward compatibility with OpenFGA clients.
        /// </summary>
        private static bool IsAegisCompatCompatibilityPath(PathString path)
        {
            var pathValue = path.Value ?? string.Empty;

            // All compatibility endpoints are store-scoped under /api/v1/stores/{storeId}/
            if (!pathValue.StartsWith("/api/v1/stores/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Compatibility operation names (case-insensitive)
            var compatOperations = new[]
            {
                "/read",
                "/write",
                "/check",
                "/batch-check",
                "/list-objects",
                "/streamed-list-objects",
                "/list-users",
                "/expand",
                "/assertions",
                "/assertions/read",
                "/assertions/write",
            };

            return compatOperations.Any(op => pathValue.EndsWith(op, StringComparison.OrdinalIgnoreCase));
        }
    }
}
