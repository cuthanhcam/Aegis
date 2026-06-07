# Target Architecture

Aegis is evolving from a learning project into a serious open-source authorization platform. This document defines the target architecture and the decisions that should be stable before adding large new feature areas.

## Product Shape

Aegis should be a centralized, multi-tenant authorization platform with:

- ReBAC as the primary authorization model.
- RBAC and ABAC as fallback or compatibility stages.
- PostgreSQL as the source of truth.
- Redis as a distributed runtime cache and invalidation layer.
- ASP.NET Core as the public API surface.
- Structured explainability for every decision.

## Runtime View

```text
-------------------+       +---------------------------+
| Client Services   | ----> | Aegis ASP.NET Core API    |
| SDKs / Admin UI   |       | Auth, routing, validation |
+-------------------+       +-------------+-------------+
                                          |
                                          v
                              +-----------+------------+
                              | Application Use Cases  |
                              | Commands, queries,     |
                              | tenant/store boundary  |
                              +-----------+------------+
                                          |
                                          v
                              +-----------+------------+
                              | Authorization Engine   |
                              | ReBAC, RBAC, ABAC,     |
                              | explanation builder    |
                              +------+----------+------+
                                     |          |
                                     v          v
                         +-----------+--+    +--+-------------+
                         | PostgreSQL   |    | Redis          |
                         | source truth |    | cache/inval    |
                         +--------------+    +----------------+
```

## Bounded Contexts

| Context | Responsibility |
| --- | --- |
| Tenancy | Tenant registry, tenant isolation, tenant-scoped administration. |
| Authorization Stores | Tenant-owned authorization namespaces for apps, services, or environments. |
| Authorization Models | Versioned model source, validation, activation, compiled rule representation. |
| Relationship Graph | ReBAC tuple writes, deletes, reads, reverse lookups, change feed. |
| Decision Engine | Deterministic evaluation pipeline and traversal budgets. |
| Policy Fallbacks | RBAC grants and ABAC conditions evaluated after primary ReBAC checks. |
| Explainability | Structured decision proof, reason codes, trace persistence policy. |
| Audit | Append-only operational and compliance events. |
| Operations | migrations, health, metrics, tracing, cache invalidation, background jobs. |

## Layering

```text
Api
  -> Application
      -> Domain
      -> Authorization
      -> Contracts
  -> Infrastructure

Infrastructure
  -> Application abstractions
  -> Domain
  -> Authorization interfaces

Authorization
  -> Domain/shared primitives only

Domain
  -> SharedKernel only
```

Rules:

- Domain must not reference Infrastructure, Api, or ASP.NET Core types.
- Authorization must not reference HTTP, controllers, EF, Npgsql, Redis, or application services.
- Application owns use-case orchestration and transaction boundaries.
- Infrastructure owns PostgreSQL, Redis, JWT implementation details, migrations, and external adapters.
- Contracts are versioned public DTOs, not domain objects.

## Decision Pipeline

Target decision order:

```text
1. Validate tenant, store, request, and model compatibility.
2. Evaluate explicit deny tuples.
3. Evaluate ReBAC direct and rewrite rules.
4. Evaluate RBAC fallback.
5. Evaluate ABAC conditions attached to grants or tuples.
6. Return default deny.
```

Explicit deny must always win.

RBAC and ABAC should not silently override ReBAC semantics. They should produce explicit reason codes such as `ALLOW_RBAC_FALLBACK` or `DENY_ABAC_CONDITION_FALSE`.

## Tenant and Store Boundary

Tenant is the isolation boundary.

Store is the authorization namespace inside a tenant.

```text
Tenant
  Store
    Authorization models
    Relationship tuples
    RBAC grants
    Audit events
```

Every runtime table should carry both `tenant_id` and `store_id` when the data belongs to an authorization store.

## Source of Truth and Caching

PostgreSQL remains authoritative.

Redis may be used for:

- Compiled model cache.
- Hot tuple lookup cache.
- Decision cache.
- Revision checkpoint cache.
- Distributed invalidation.
- Idempotency keys.

Redis must not be required to reconstruct authorization state after data loss.

## Explainability Contract

Explainability is a platform feature, not debug logging.

Each decision should be able to return:

- final allow/deny decision;
- reason code;
- model id and revision;
- tuple graph path or failed path;
- policy stage that decided;
- condition evaluation result;
- traversal budget stops such as max depth or cycle detection.

The API may expose compact traces by default and detailed proof trees behind `/explain`.

## Operational Requirements

The target platform should expose:

- liveness and readiness endpoints;
- migration status;
- PostgreSQL and Redis connectivity checks;
- OpenTelemetry traces;
- Prometheus-compatible metrics;
- structured logs with tenant/store/request correlation;
- benchmark suite for representative graph shapes.

