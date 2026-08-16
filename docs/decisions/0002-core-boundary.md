# ADR 0002: Keep Aegis Core focused on authorization semantics

- Status: Accepted
- Date: 2026-08-16
- Owners: Aegis maintainers

## Context

Aegis promises centralized, explainable authorization for products needing fine-grained access control. Identity UI, persistence, deployment, and administration support that promise but must not define decision semantics.

## Decision

The core boundary consists of authorization model semantics, relationship graph semantics, deterministic evaluation, explicit deny precedence, bounded work, explain evidence, immutable model-version behavior, and their domain invariants.

Core code must not depend on ASP.NET Core, PostgreSQL, Redis, a telemetry vendor, UI state, account/session storage, billing, or deployment topology. It must be executable through isolated tests.

## Consequences

- Authentication establishes trusted identity outside Core.
- API and Application translate transport and actor context into core inputs.
- Infrastructure implements persistence and cache ports without changing outcomes.
- Administrative features may use the same host while remaining outside the decision model.
- Any new dependency in Domain or Authorization receives architecture review.

## Validation

Project dependency tests protect the initial assembly boundary. The golden decision corpus planned in Phase B0 will prove semantics independently of infrastructure.
