# ADR 0006: Govern public API contracts explicitly

- Status: Accepted
- Date: 2026-08-16
- Owners: Aegis maintainers

## Context

Aegis is an authorization platform consumed by products, SDKs, operators, and eventually a rewritten management console. A small route, payload, status-code, or error-code change can therefore break several consumers at once. Controllers consistently use `/api/v1`, but that convention was not enforced and OpenAPI was only exposed through development middleware.

Compatibility-shaped endpoints also exist inside the native route space. They preserve selected external semantics, but must not silently dictate the long-term Aegis domain model or make native contracts ambiguous.

## Decision

The native HTTP API uses an explicit major version in its route: `/api/v1`. Every controller action declares an HTTP method and participates in the generated `v1` OpenAPI document. Architecture-level integration tests fail when an action escapes this boundary. Operational endpoints such as `/metrics` and health probes are deliberately unversioned because they describe the host rather than the product API; each exception must be allowlisted by the contract test.

The governed contract includes routes, methods, request and response schemas, status codes, authentication requirements, and stable machine-readable error codes. Human-readable error messages are diagnostic text and are not compatibility identifiers.

A change is breaking when an existing conforming consumer must change to preserve behavior. Removing or renaming fields or operations, narrowing accepted input, changing field meaning, changing success or error status semantics, or replacing stable error codes requires one of these paths:

1. introduce a new major API version;
2. retain the old behavior for a documented deprecation window; or
3. obtain an explicitly recorded exception when no external consumer has received the contract.

Additive optional fields and new operations may remain in `v1` when they preserve existing behavior. Compatibility surfaces are documented and tested separately; they do not bypass tenant isolation, authentication, authorization, audit, or deterministic-decision rules.

OpenAPI is generated from the executable application graph through `eng/export-openapi.ps1`. The artifact is suitable for contract diffing and client generation. Pipeline publication remains a release gate, but the existing pipeline will not be changed until repository-owner approval.

## Consequences

- Route drift becomes a test failure instead of a review convention.
- Backend and future frontend work share one machine-readable contract source.
- Breaking changes require deliberate lifecycle work and migration communication.
- Stable error codes become part of the public compatibility promise.
- An OpenAPI diff baseline and generated TypeScript client are still required before B1 is Verified.

## Validation

`ApiContractGovernanceTests` resolves MVC action descriptors and the runtime Swagger provider. It validates versioned routes and HTTP methods, verifies the `v1` document, and optionally writes JSON when `AEGIS_OPENAPI_OUTPUT` is set. `eng/export-openapi.ps1` provides the repeatable export command.
