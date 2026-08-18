# ADR 0009: Protect authorization-model edits with strong entity tags

- Status: Accepted
- Date: 2026-08-18

## Context

Authorization models are security-critical configuration. Two administrators can read the same draft and then update or delete it. Without a precondition, the later request silently overwrites the earlier decision. A read-before-write check in the API is insufficient because another replica can commit between the check and mutation.

## Decision

Every authorization model has a positive, monotonically increasing `revision`. Single-resource reads expose that revision as a strong `ETag`. Updating or deleting a model requires `If-Match` containing exactly one quoted positive revision.

Missing preconditions return HTTP 428 with `PRECONDITION_REQUIRED`. A well-formed but stale precondition returns HTTP 412 with `CONCURRENCY_CONFLICT`. Malformed or weak tags return the existing HTTP 400 validation contract.

The PostgreSQL update and delete statements compare and mutate the revision atomically. The in-memory provider uses compare-and-swap semantics. This avoids a process-local lock that would fail when Aegis runs multiple replicas.

This iteration covers definition update and delete. Publish and rollback change multiple rows and require a store-scoped transaction precondition; they remain explicitly deferred to model-lifecycle hardening. Idempotency replay also remains deferred until a durable tenant-scoped store can guarantee one payload per key across replicas.

### Implementation evolution: lifecycle transitions

The next B1 iteration completed the deferred publish and rollback protection without changing the decision. Both endpoints require the target model's current strong ETag. PostgreSQL locks the owning store row, validates the target revision, and changes the target/current-published rows in one transaction. The store lock serializes lifecycle transitions across replicas. The in-memory provider performs the same transition under one critical section.

Migration 011 deterministically archives any pre-existing duplicate published rows before adding a partial unique index that permits at most one published model per store. Transaction serialization protects the transition path; the database constraint protects the invariant from every writer.

## Consequences

Clients must read a model before editing or deleting it, preserve the returned ETag, and refresh after HTTP 412. Blind retries are rejected. The additive `revision` response field helps diagnostics, but clients should treat the ETag header as the HTTP concurrency contract.

Migration `010_authorization_model_revision.sql` initializes existing rows at revision 1. Every model state or definition mutation increments the revision so previously issued tags become stale.
