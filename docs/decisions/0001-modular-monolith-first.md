# ADR 0001: Build a modular monolith before extracting services

- Status: Accepted
- Date: 2026-08-16
- Owners: Aegis maintainers

## Context

Aegis needs strong consistency across model publication, relationships, decision caches, audit evidence, and outbox records. Premature process boundaries would introduce network failure and operational cost before scale or ownership evidence justifies them. Sharing tables across nominal microservices would create a distributed monolith.

## Decision

Aegis will remain one deployable backend during product-readiness work while enforcing module and project boundaries in code and tests. Service extraction is allowed only when measured scale, security isolation, data residency, ownership, or release cadence provides a concrete reason.

An extracted service owns its persistence contract, migrations, credentials, cache namespace, SLO, and operational runbooks. Sharing a PostgreSQL or Redis cluster is allowed as infrastructure consolidation; direct access to another service's schema or cache keys is not.

## Consequences

- Authorization work stays local and easier to reason about.
- Transactions and recovery remain simpler during hardening.
- Project dependency tests become release gates.
- Modules must not communicate through infrastructure internals.
- Future extraction requires an ADR and evidence, not preference.

## Validation

`ProjectDependencyTests` enforces the current production project reference policy. Later module-level tests will enforce namespace and persistence ownership as the module structure becomes explicit.
