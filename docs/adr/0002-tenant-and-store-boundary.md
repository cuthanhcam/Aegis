# ADR 0002: Tenant and Store Boundary

## Status

Accepted

## Context

Aegis is multi-tenant. The current implementation uses tenant context in many APIs and tables, and also has stores for authorization models. These concepts must be distinct.

## Decision

Tenant is the isolation boundary.

Store is an authorization namespace owned by a tenant.

Store-scoped runtime data should include both `tenant_id` and `store_id` where practical.

## Consequences

- API handlers must validate tenant context before accessing store data.
- Database queries must scope by tenant, and store when applicable.
- Cache keys must include tenant and store.
- Audit events should include tenant and store.
