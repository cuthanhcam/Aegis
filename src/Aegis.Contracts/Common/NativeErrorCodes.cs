namespace Aegis.Contracts.Common;

/// <summary>
/// Stable machine-readable error codes exposed by the native Aegis API.
/// Messages may evolve, but these identifiers are part of the v1 contract.
/// </summary>
public static class NativeErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string InvalidOperation = "INVALID_OPERATION";
    public const string NotFound = "NOT_FOUND";
    public const string PermissionDenied = "PERMISSION_DENIED";
    public const string InternalError = "INTERNAL_ERROR";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string RequestTimeout = "REQUEST_TIMEOUT";
    public const string PreconditionRequired = "PRECONDITION_REQUIRED";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string IdempotencyConflict = "IDEMPOTENCY_CONFLICT";
    public const string TenantForbidden = "TENANT_FORBIDDEN";
    public const string TenantMismatch = "TENANT_MISMATCH";
    public const string TenantRequired = "TENANT_REQUIRED";
    public const string StoreForbidden = "STORE_FORBIDDEN";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
    public const string StoreNotFound = "STORE_NOT_FOUND";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string PermissionNotFound = "PERMISSION_NOT_FOUND";
    public const string AuthorizationModelNotFound = "AUTHORIZATION_MODEL_NOT_FOUND";
    public const string AssertionRunNotFound = "ASSERTION_RUN_NOT_FOUND";
}
