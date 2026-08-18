using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Aegis.Api.Observability;
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
                await WriteErrorAsync(context, ex.StatusCode, NativeCodeForStatus(ex.StatusCode), ex.ErrorCode, ex.Message);
            }
            catch (ArgumentException ex)
            {
                await WriteErrorAsync(context, StatusCodes.Status400BadRequest, NativeErrorCodes.ValidationError, "validation_error", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await WriteErrorAsync(context, StatusCodes.Status400BadRequest, NativeErrorCodes.InvalidOperation, "invalid_operation", ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                await WriteErrorAsync(context, StatusCodes.Status404NotFound, NativeErrorCodes.NotFound, "not_found", ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                await WriteErrorAsync(context, StatusCodes.Status403Forbidden, NativeErrorCodes.PermissionDenied, "permission_denied", ex.Message);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // Preserve the cancellation signal for the outer request-timeout middleware
                // or server disconnect handling. It is not an internal application failure.
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}: {ExceptionType}",
                    context.Request.Method, context.Request.Path, ex.GetType().Name);
                await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, NativeErrorCodes.InternalError, "internal_error", "Unexpected error occurred.");
            }
        }

        /// <summary>
        /// Writes error response with appropriate envelope based on request path.
        /// Compatibility endpoints: AegisCompatErrorResponseDto (flat envelope)
        /// Native endpoints: ApiResponse<T>.Fail (nested envelope)
        /// </summary>
        private static Task WriteErrorAsync(HttpContext context, int statusCode, string nativeCode, string compatibilityCode, string message)
        {
            var isCompatibilityPath = ErrorEnvelopePathClassifier.IsCompatibilityPath(context.Request.Path);
            var responseCode = isCompatibilityPath ? compatibilityCode : nativeCode;
            context.Items["Aegis.ErrorCode"] = responseCode;
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            if (isCompatibilityPath)
            {
                return context.Response.WriteAsync(
                    JsonSerializer.Serialize(new AegisCompatErrorResponseDto(compatibilityCode.ToLowerInvariant(), message)));
            }

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(ApiResponse<string>.Fail(nativeCode, message, RequestTraceContext.GetTraceId(context))));
        }

        private static string NativeCodeForStatus(int statusCode) => statusCode switch
        {
            StatusCodes.Status400BadRequest => NativeErrorCodes.ValidationError,
            StatusCodes.Status401Unauthorized => NativeErrorCodes.Unauthorized,
            StatusCodes.Status403Forbidden => NativeErrorCodes.PermissionDenied,
            StatusCodes.Status404NotFound => NativeErrorCodes.NotFound,
            _ when statusCode >= StatusCodes.Status500InternalServerError => NativeErrorCodes.InternalError,
            _ => NativeErrorCodes.InvalidOperation,
        };

        /// <summary>
        /// Path classification now lives in ErrorEnvelopePathClassifier for reuse across middleware and API behavior.
        /// </summary>
    }
}
