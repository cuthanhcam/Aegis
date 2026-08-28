# ADR 0010: Couple idempotency records to business commits

- Status: Accepted
- Date: 2026-08-28

## Context

Retrying a create after a timeout can produce two authorization models. A generic middleware cache cannot close the failure window between committing the model and storing the response. Process-local locks and Redis-only response caching also fail when replicas restart or a lease expires after an ambiguous commit.

## Decision

Idempotency is implemented at the application/persistence boundary for each supported mutation. For authorization-model creation, the idempotency reservation, model insert, and replay response are committed in one PostgreSQL transaction. A uniqueness constraint scopes keys by tenant, authenticated actor, store, and operation.

The client supplies an optional `Idempotency-Key` containing 8–128 safe ASCII characters. A SHA-256 fingerprint binds the key to the normalized schema version and model definition. Reusing a key with the same fingerprint returns the original HTTP 201 representation. Reusing it with a different fingerprint returns HTTP 409 `IDEMPOTENCY_CONFLICT`.

Records expire after 24 hours. Reuse after expiry is allowed only after the expired record is removed within the same transactional reservation path. An expiry index supports future scheduled cleanup; cleanup is an operational optimization, not a correctness dependency.

## Consequences

The first implementation covers native authorization-model creation only. Requests without a key retain existing behavior. Other creates and writes do not claim idempotency until their business commit and replay record share an atomic boundary.

Domain events are dispatched only for the transaction that creates the model, never for a replay. Response identity, creation time, revision, status, and ETag remain stable across retries.
