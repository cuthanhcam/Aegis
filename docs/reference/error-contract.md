# Native API error contract

Aegis native endpoints return a stable envelope so products can handle failures without parsing English text.

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "The Username field must be a string or array type with a minimum length of '3'.",
    "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
    "details": {
      "Username": ["The Username field must be a string or array type with a minimum length of '3'."],
      "Password": ["The Password field must be a string or array type with a minimum length of '6'."]
    }
  }
}
```

## Consumer rules

- Branch on HTTP status and `error.code`, never on `error.message`.
- Record `traceId` when reporting an incident or support request.
- Treat `details` as optional. Keys identify request fields and values may contain multiple safe validation messages.
- Do not display raw internal errors; Aegis maps unhandled failures to `INTERNAL_ERROR` with a safe message.
- A retry is safe only when the operation contract explicitly says so. An error code alone does not imply idempotency.

## Stable native codes

| Code | Meaning |
| --- | --- |
| `VALIDATION_ERROR` | Input or compatibility-originated native request validation failed |
| `INVALID_OPERATION` | The requested state transition or operation is invalid |
| `NOT_FOUND` | A generic requested resource does not exist |
| `PERMISSION_DENIED` | The authenticated actor cannot perform the operation |
| `INTERNAL_ERROR` | An unexpected server failure was safely redacted |
| `RATE_LIMIT_EXCEEDED` | The configured request budget was exceeded |
| `REQUEST_TIMEOUT` | The request exceeded its configured execution deadline |
| `PRECONDITION_REQUIRED` | A required mutation precondition was omitted |
| `CONCURRENCY_CONFLICT` | The supplied resource revision is stale |
| `IDEMPOTENCY_CONFLICT` | An idempotency key was reused with a different request payload |
| `UNAUTHORIZED` | Required authentication/session identity is absent or invalid |
| `INVALID_CREDENTIALS` | Login credentials were rejected |
| `INVALID_REFRESH_TOKEN` | Refresh credentials are invalid or expired |
| `TENANT_REQUIRED` | No tenant context was supplied |
| `TENANT_MISMATCH` | Conflicting tenant contexts were supplied |
| `TENANT_FORBIDDEN` | The actor cannot access the tenant scope |
| `STORE_FORBIDDEN` | The store does not belong to the authorized tenant scope |
| `STORE_NOT_FOUND` | The requested store does not exist |
| `USER_NOT_FOUND` | The requested user does not exist |
| `PERMISSION_NOT_FOUND` | The requested permission does not exist |
| `AUTHORIZATION_MODEL_NOT_FOUND` | The requested authorization model does not exist |
| `ASSERTION_RUN_NOT_FOUND` | The requested assertion run does not exist |

This registry covers transport failures. Authorization decision reason codes such as `DENY_NOT_FOUND` belong to the decision result contract and are governed separately.

## Compatibility endpoints

Routes explicitly classified as compatibility surfaces keep their flat lowercase error payload. Consumers must use the contract documented for that surface rather than assuming the native envelope. Compatibility does not bypass authentication, tenant/store isolation, audit, or rate limits.

## Operator correlation

The response `traceId` is the same identifier recorded as `trace_id` by request-completion logging. Operators should use it to locate the request, status, endpoint, tenant/store context, and safe error code. It must not be treated as a secret or as proof that an authorization decision was correct.
