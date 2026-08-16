---
title: Tenant and store isolation
description: How Aegis preserves authorization scope across identity, routes, repositories, databases, caches, traces, and audits.
category: concepts
audience: [backend-engineer, security-engineer, operator]
status: published
last_updated: 2026-08-16
series: authorization-foundations
order: 3
---

# Tenant and store isolation

Multi-tenancy is Aegis's central data-security boundary. A correct algorithm remains unsafe if it evaluates one tenant's request with another tenant's model, relationships, cache entry, or audit data.

## Scope hierarchy

A tenant is an administrative and security boundary. A store is a tenant-owned authorization namespace. An identity may have tenant administration rights, store rights, or permission to call decision APIs without permission to inspect policy data.

```text
authenticated principal
  └─ tenant membership and roles
      └─ store ownership and delegated access
          └─ model, relationship, check, graph, assertion, and audit operations
```

Store identifiers are not capabilities. Knowing one never grants access.

## Defense in depth

### Identity and HTTP

Validate issuer, audience, signature, lifetime, and claims. Tenant scope comes from a trusted issuer or controlled exchange, never an arbitrary request header. At HTTP boundaries compare authenticated scope with route/body scope and reject ambiguity. Apply one enumeration policy consistently when choosing forbidden versus not found.

### Application and persistence

Pass an immutable execution context containing tenant, optional store, actor, roles/scopes, correlation, deadline, and cancellation. Prefer `GetStore(tenantId, storeId)` to scope-optional APIs. Tenant-owned rows carry tenant identifiers; constraints, unique keys, indexes, and queries incorporate scope. PostgreSQL row-level security can add a second boundary but does not replace safe repositories.

### Cache, telemetry, and audit

Cache prefixes include environment, semantics version, tenant, store, and resource/decision identity with canonical serialization. Logs and traces use approved identifiers and exclude tokens, secrets, unrestricted tuples, raw context, and policy source. Audit-query authorization is at least as strict as mutation authorization.

## Negative test matrix

| Caller                   | Target                       | Expected result                         |
| ------------------------ | ---------------------------- | --------------------------------------- |
| Tenant A administrator   | Tenant A store               | Allowed by role                         |
| Tenant A administrator   | Tenant B store with known ID | No data and no mutation                 |
| Tenant A decision client | Tenant A check               | Allowed within scope                    |
| Tenant A decision client | Tenant A audit/model admin   | Forbidden unless separately scoped      |
| Revoked principal        | Formerly accessible store    | Rejected within the revocation contract |
| Unauthenticated caller   | Non-public endpoint          | Rejected without resource disclosure    |

Repeat the matrix at HTTP, use-case, repository, database, cache, graph, export, background-job, and event boundaries. Include mixed-scope batches and identifier normalization edges.

## Background work and incidents

Outbox handlers, seeders, retention jobs, publishers, and repair tools bypass HTTP middleware, so messages and commands carry explicit scope and repositories enforce it. Operator tools need least privilege, dry-run for broad changes, and audit evidence.

A suspected isolation failure is a security incident. Preserve evidence, stop the affected path safely, identify affected versions and tenants, rotate credentials where necessary, and correlate deployment, database, cache/event, decision, and audit history.

## Verification checklist

- [ ] Scope is explicit from identity through persistence.
- [ ] Rows, constraints, indexes, and caches incorporate scope.
- [ ] Negative tests cover HTTP, data, cache, batch, graph, and background paths.
- [ ] Explain, audit, export, and administration have independent privileges.
- [ ] Errors and telemetry do not disclose cross-tenant existence.
- [ ] An isolation incident drill has been completed.

## Continue reading

Return to [System architecture](../architecture/system-architecture.md) or continue to [Operating Aegis in production](../operations/production-readiness.md).
