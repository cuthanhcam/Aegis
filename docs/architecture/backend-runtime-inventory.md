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
| `Outbox:*`                  | Present in settings                                         | Worker currently hard-codes batch 100/poll 10 seconds; bind and validate options |
| `Seed:Development:Enabled`  | Controls development seed                                   | Environment guard remains mandatory                                              |
| `Logging:Http:Enabled`      | Adds method/path/status/duration logging in development     | Maintain redaction and cardinality policy                                        |

Configuration access is currently split between `Program.cs`, Infrastructure registration, and initialization. A later B0/B1 slice should introduce typed, startup-validated option groups and tests for invalid configurations.

## Persistence and migrations

PostgreSQL is the durable provider; in-memory implementations support tests and evaluation. Ten embedded forward migrations currently cover initial schema, RBAC conditions, relationship effects/indexes, store and tenant scoping, authorization model lifecycle and revisions, and assertion-run history.

The migration runner orders embedded resource names, records successful names in `schema_migrations`, and executes each migration transactionally. It does not yet record checksums, acquire a migration lock, enforce expand/contract compatibility, or separate migration authority fully from application startup. These are Phase B3 gaps.

## Cache inventory

| Cache                  | Scope/key identity                                                                                                                   | Invalidation                                                                 |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------- |
| Decision cache         | tenant, effective store, subject, relation, object, consistency, requested model ID, trace flag, canonical contextual tuples/context | tenant-wide invalidation after relevant store/model/relationship/RBAC writes |
| Parsed-model cache     | model text with bounded LRU/TTL                                                                                                      | expiry/capacity; immutable model text makes reuse safe                       |
| PostgreSQL RBAC grants | tenant, store, subject                                                                                                               | tenant grant eviction after RBAC writes                                      |

Redis is optional for the decision cache; local memory remains present. Cache loss may affect latency but cannot be authoritative. Phase B2/B3 must prove model-version identity, multi-instance invalidation, collision safety, and mutation visibility.

## Background processing

One hosted service processes the domain-event outbox. It creates a scope, requests up to 100 pending items, logs failures, and waits 10 seconds. The outbox store is currently in-memory even with PostgreSQL selected, and the publisher logs rather than delivering to a durable external destination. This is a development foundation, not crash-safe production delivery.

Required evolution includes durable outbox persistence, configuration-bound batch/poll settings, idempotent publishing, retry/backoff, poison handling, backlog age/count metrics, graceful draining, and operator replay controls.

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
