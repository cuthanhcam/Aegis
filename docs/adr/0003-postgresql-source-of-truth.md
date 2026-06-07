# ADR 0003: PostgreSQL Is the Source of Truth

## Status

Accepted

## Context

Aegis uses PostgreSQL for persistence and Redis for cache acceleration. Authorization platforms must avoid stale or unrecoverable permission state.

## Decision

PostgreSQL is the authoritative source of truth.

Redis may be used for cache, idempotency, and invalidation support, but not as the only copy of authorization state.

## Consequences

- Redis loss must not lose authorization tuples, models, RBAC grants, or audit events.
- Cache keys must be tenant/store scoped.
- Cache invalidation must be revision-aware where possible.
- Write paths must commit to PostgreSQL before publishing invalidation signals.
