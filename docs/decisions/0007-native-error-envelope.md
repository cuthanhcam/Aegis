# ADR 0007: Preserve and enrich the native v1 error envelope

- Status: Accepted
- Date: 2026-08-17
- Owners: Aegis maintainers

## Context

The native v1 API already exposes failures through `ApiResponse<T>.error`, while compatibility endpoints expose a deliberately different flat envelope. Replacing the native shape with RFC Problem Details inside v1 would be a breaking response change for existing consumers. The current implementation also had inconsistent code casing between controllers and exception middleware, returned only the first validation failure, and did not consistently expose the correlation identifier used by server logs.

## Decision

Native `/api/v1` failures retain the existing `ApiResponse<T>` envelope. Its error object contains a stable uppercase snake-case `code`, safe human-readable `message`, request `traceId`, and optional field-keyed `details`. Details are currently emitted for model validation and must never include secrets, credentials, tokens, policy source, or tenant data that the caller is not authorized to read.

Native error identifiers are declared in `NativeErrorCodes`. Application and API code reference the registry instead of introducing string literals. Error messages are not compatibility identifiers and may be clarified without versioning. Removing or changing the meaning of a code is a breaking contract change.

Compatibility routes preserve their documented lowercase flat envelope. Exception handling maps one failure into separate native and compatibility identifiers instead of deriving one contract from the casing of the other.

`traceId` matches the distributed activity trace when available and falls back to the ASP.NET Core request identifier. It is diagnostic correlation, not authorization evidence, and clients must not infer failure semantics from it.

Problem Details may be reconsidered for a new major API version or content-negotiated contract, but it will not be introduced as an in-place v1 replacement.

## Consequences

- Existing v1 clients retain their top-level success/data/error shape.
- Every MVC-generated native error, rate-limit rejection, validation failure, and middleware exception can be correlated with request logs.
- Validation clients receive all safe field errors in one response.
- Compatibility behavior remains independently testable.
- Direct non-MVC responses added in the future must use the native response factory or explicitly attach trace metadata.

## Validation

Unit tests enforce unique uppercase snake-case registry values. Integration tests cover validation details, rate-limit errors, controller-generated tenant errors, stable codes, and non-empty trace identifiers. OpenAPI diff and generated-client compilation validate the additive contract change.
