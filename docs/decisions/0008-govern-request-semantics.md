# ADR 0008: Govern request limits, cursors, deadlines, and cancellation

- Status: Accepted
- Date: 2026-08-17
- Owners: Aegis maintainers

## Context

Aegis evaluates security-sensitive queries whose cost can grow with batch size, graph depth, result size, and dependency latency. Request limits were previously scattered as private constants, compatibility batch checks had no top-level cap, native relationship cursors exposed raw numeric offsets, and the host had no application request deadline. Most async controller actions already accepted cancellation tokens, but this was not enforced.

Changing every existing collection response to a paged envelope in one v1 release would break consumers. Governance therefore needs an incremental path that establishes shared limits and safe primitives before migrating each list endpoint.

## Decision

`ApiRequestLimits` is the public source for v1 page, batch, cursor-length, and filter-length limits. Native paged endpoints emit opaque, versioned continuation tokens. The current codec accepts legacy non-negative numeric tokens during rolling upgrades but never emits them. Clients must store and return a cursor unchanged and must not parse or construct it.

Native `relationships/changes` uses a default page size of 50 and a maximum of 100. Native and compatibility batch check accept at most 1,000 items. Compatibility payload names and error envelopes remain separate, but resource-protection limits apply equally.

The all-in-one host applies a configurable default request deadline of 30 seconds, bounded to 1–300 seconds at startup. A timeout returns HTTP 504 with `REQUEST_TIMEOUT` for native routes or `request_timeout` for compatibility routes. The response carries the same trace correlation policy as other errors.

All asynchronous controller actions accept and propagate `CancellationToken`. An architecture-level integration guard prevents new async actions from dropping the request-aborted signal.

Existing unpaged list responses remain unchanged until each endpoint receives an additive or versioned migration plan. Unsupported sorting parameters are not silently treated as contractual behavior; ordering remains endpoint-defined and must be documented during migration.

## Consequences

- Cost limits are visible to OpenAPI, generated clients, application code, and documentation.
- Cursor representation can evolve without creating a consumer contract.
- Old numeric cursors remain usable during the transition.
- Slow requests receive a bounded execution budget and cooperative cancellation.
- Full collection pagination is still B1 work and must proceed endpoint by endpoint.

## Validation

Codec tests cover round trips, legacy tokens, malformed input, negative offsets, and unsupported versions. Integration tests prove page-limit validation, opaque cursor emission, cursor reuse, and cancellation-token presence on async controller actions. OpenAPI diff and generated TypeScript compilation validate the public annotations and error-code addition.
