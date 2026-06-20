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
            context.Items["Aegis.ErrorCode"] = code;
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            if (ErrorEnvelopePathClassifier.IsCompatibilityPath(context.Request.Path))
            {
                return context.Response.WriteAsync(
                    JsonSerializer.Serialize(new AegisCompatErrorResponseDto(code.ToLowerInvariant(), message)));
            }

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(ApiResponse<string>.Fail(code, message)));
        }

        /// <summary>
        /// Path classification now lives in ErrorEnvelopePathClassifier for reuse across middleware and API behavior.
        /// </summary>
    }
}
