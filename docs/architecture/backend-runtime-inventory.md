---
title: Backend runtime inventory
description: A maintained inventory of Aegis HTTP surfaces, configuration, persistence, caches, background work, and operational endpoints.
category: architecture
audience: [backend-engineer, operator, security-engineer]
status: published
last_updated: 2026-08-17
---

# Backend runtime inventory

This inventory establishes the Phase B0 baseline. It is a review map, not a replacement for generated OpenAPI, configuration validation, or executable tests. Update it whenever a runtime surface is added, removed, renamed, or changes ownership.

## Hosting composition

The current supported composition is the all-in-one `Aegis.Api` host. It registers controllers, application use cases, authorization engine, storage adapters, cache, session service, outbox worker, health checks, metrics, CORS, authentication, authorization policies, rate limiting, and development-only Swagger.

The future `runtime`, `control-plane`, and `worker` profiles are architectural seams accepted in ADR 0003; they are not implemented or supported deployment modes yet.

## HTTP surface

Eighteen controllers expose the following route groups under `/api/v1`:

| Group                | Base route                                            | Operations                                                                   |
| -------------------- | ----------------------------------------------------- | ---------------------------------------------------------------------------- |
| Authentication       | `/auth`                                               | login, refresh, current session, logout, logout all                          |
| Tenant check/explain | `/check`, `/explain`                                  | native decision and trace using tenant query scope                           |
| Stores               | `/stores`                                             | list, create, get, delete                                                    |
| Store decision       | `/stores/{storeId}`                                   | check, explain, batch check, compatibility check/batch                       |
| Models               | `/stores/{storeId}/authorization-models`              | list, latest, get, create, validate, update, delete, publish, rollback, diff |
| Relationships        | `/stores/{storeId}/relationships`                     | query, changes, write, delete                                                |
| Graph                | `/stores/{storeId}/graph`                             | list users, list objects, expand, plus compatibility variants                |
| Assertions           | `/stores/{storeId}/assertions`                        | read/write suite, run, run history, generate from audit                      |
| Tenant RBAC          | `/tenants/{tenantId}/roles`, `/permissions`, `/users` | administration and assignments                                               |
| Store RBAC           | `/stores/{storeId}/roles`, `/permissions`, `/users`   | store-scoped administration and assignments                                  |
| Audit                | `/tenants/{tenantId}/audit`                           | filtered decision/activity query                                             |
| Presets              | `/tenants/{tenantId}/presets`                         | list, create, delete, metadata read/update                                   |
| Metrics              | `/metrics`, `/api/v1/metrics/authorization`           | Prometheus text and authorization snapshot                                   |
| Health               | `/health/live`, `/health/ready`                       | process liveness and configured dependency readiness                         |

Native and compatibility behavior currently coexist. Phase B1 must generate this inventory from OpenAPI, classify compatibility guarantees, and fail CI on unintended contract drift.

## Configuration surface

| Key                         | Current behavior                                            | Product-readiness note                                                           |
| --------------------------- | ----------------------------------------------------------- | -------------------------------------------------------------------------------- |
| `Storage:Provider`          | `Postgres` or fallback in-memory                            | Validate supported values instead of silently accepting arbitrary fallback       |
| `ConnectionStrings:Aegis`   | Required for PostgreSQL                                     | Source from secret provider in production                                        |
| `Cache:Provider`            | memory or Redis                                             | Validate supported values and define correctness behavior on Redis loss          |
| `Cache:DecisionTtlSeconds`  | Defaults to 15 seconds                                      | Add bounds and consistency contract                                              |
| `Cache:Redis:Configuration` | Required when Redis selected                                | Secret/redaction policy required                                                 |
| `Jwt:*`                     | issuer, audience, symmetric secret, token/session durations | Demo/local identity must be separated from production OIDC/JWKS                  |
| `Auth:DemoUsers`            | Required by startup validation                              | Must become development/evaluation-only                                          |
| `Cors:AllowedOrigins`       | Required list                                               | Environment-specific allowlist; no wildcard with credentials                     |
| `RateLimiting:Auth:*`       | Fixed-window login limit                                    | Validate positive bounds and trusted proxy/IP behavior                           |
| `RequestTimeouts:DefaultSeconds` | Global request deadline, default 30 seconds             | Startup validates the supported 1–300 second range                               |
| `AuthorizationEngine:*`     | depth and parsed-model cache budgets                        | Validate all bounds and document exhaustion behavior                             |
| `Outbox:*`                  | Batch, poll, and retry schedule                              | Startup validates bounds; retention, leasing, and dead-letter policy remain       |
| `Seed:Development:Enabled`  | Controls development seed                                   | Environment guard remains mandatory                                              |
| `Logging:Http:Enabled`      | Adds method/path/status/duration logging in development     | Maintain redaction and cardinality policy                                        |

Configuration access is currently split between `Program.cs`, Infrastructure registration, and initialization. A later B0/B1 slice should introduce typed, startup-validated option groups and tests for invalid configurations.

## Persistence and migrations

PostgreSQL is the durable provider; in-memory implementations support tests and evaluation. Sixteen embedded forward migrations currently cover initial schema, RBAC conditions, relationship effects/indexes, store and tenant scoping, authorization model lifecycle/revisions/single-active invariant, assertion definitions/run history, transactional idempotency, and atomic store deletion.

The migration runner orders embedded resource names, serializes concurrent instances with a PostgreSQL session advisory lock, executes and records each migration in one transaction, and stores a normalized SHA-256 checksum. It fails closed on checksum drift, missing embedded history, lock timeout, cancellation, or statement failure. Existing pre-checksum history is bootstrapped once from the matching embedded resources. Lock and statement deadlines are configurable under `Database:Migrations`.

Startup now has explicit `Apply` and `Validate` authority modes. `Apply` preserves monolith/local behavior. `Validate` is read-only and refuses absent history, pending/unknown migrations, missing checksums, or drift. The one-shot `Aegis.Migrator` executable owns the same apply logic without hosting the API or seed path. This creates the code boundary for production privilege separation; managed identity/grant cutover and rehearsal remain Phase B3/B5 deployment work.

## Cache inventory

| Cache                  | Scope/key identity                                                                                                                   | Invalidation                                                                 |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------- |
| Decision cache         | tenant, effective store, subject, relation, object, consistency, requested model ID, trace flag, canonical contextual tuples/context | tenant-wide invalidation after relevant store/model/relationship/RBAC writes |
| Parsed-model cache     | model text with bounded LRU/TTL                                                                                                      | expiry/capacity; immutable model text makes reuse safe                       |
| PostgreSQL RBAC grants | tenant, store, subject                                                                                                               | tenant grant eviction after RBAC writes                                      |

Redis is optional for the decision cache; local memory remains present. Cache loss may affect latency but cannot be authoritative. Phase B2/B3 must prove model-version identity, multi-instance invalidation, collision safety, and mutation visibility.

## Background processing

One hosted service processes the domain-event outbox with validated batch, polling, and retry settings. PostgreSQL profiles persist payload, attempts, bounded error state, next-attempt time, and completion; in-memory profiles remain local/test only. The publisher still logs rather than delivering to a durable external destination.

Business writes and outbox append are not yet one transaction, and pending reads do not lease work across multiple workers. Required evolution includes transaction ownership, claim/lease semantics, idempotent publishing, poison handling, backlog age/count metrics, graceful draining, retention, and operator replay controls.

## Operational signals

Authorization metrics currently count request allow/deny/error, database queries/results, evaluation memo hits/misses, and parsed-model cache hits/misses. Liveness has no dependency predicate. Readiness includes PostgreSQL and Redis only when configured. Request logging includes method, path, status, duration, endpoint, safe tenant/user fields, trace ID, request ID, remote IP, and error code.

Phase B4 must add OpenTelemetry export, bounded-cardinality review, SLI definitions, model/decision correlation, dashboards, alerts, and runbooks. Audit evidence and operational telemetry remain separate security surfaces.

## Inventory verification checklist

- [x] Controller route groups inventoried.
- [x] Configuration keys and defaults inventoried.
- [x] PostgreSQL migrations inventoried.
- [x] Decision, parsed-model, and RBAC caches inventoried.
- [x] Hosted background processing inventoried.
- [x] Current metrics and health surfaces inventoried.
- [ ] Generate endpoint inventory from OpenAPI in CI.
- [ ] Replace manual configuration inventory with typed option validation tests.
- [ ] Add ownership and deployment-profile classification to each runtime component.

## Continue reading

See [System architecture](./system-architecture.md), [ADR 0001](../decisions/0001-modular-monolith-first.md), and the [backend product-readiness plan](../../temp/backend-product-readiness-plan.md).
