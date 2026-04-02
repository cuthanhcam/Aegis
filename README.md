# Aegis

Tagline: Guarding access with a practical authorization engine.

Aegis is a ReBAC-first authorization platform blueprint with RBAC fallback, designed for multi-tenant systems that need deterministic permission checks and clear engine boundaries.

## What Aegis Focuses On

- Centralized authorization APIs (`/check`, `/explain`, tuple management)
- ReBAC canonical tuple model: `(subject, relation, object)`
- Explicit deny precedence (`deny > allow`)
- Tenant-scoped data and evaluation paths
- Engine/application separation to avoid lock-in to transport or persistence

## Architecture Direction

Aegis is structured as an authorization platform, not only a Web API project.

- Authorization engine as an isolated module
- Application layer for use-case orchestration
- Infrastructure layer for DB/cache/adapters
- API layer for transport and contracts

See full blueprint in `docs/architecture/project-structure.md`.

## Documentation Map

- `docs/Overview.md`: Product and platform overview
- `docs/architecture/project-structure.md`: Production-ready project structure
- `docs/architecture/permission-engine.md`: Evaluation model and conflict resolution
- `docs/architecture/database-design.md`: Tuple schema, indexes, and query patterns
- `docs/architecture/api-spec.md`: Endpoint contracts and response model

## Current Repository State

- Documentation-first foundation is complete
- Architecture and data model are aligned to ReBAC tuple semantics
- API and engine docs are aligned on explicit deny precedence
- Implementation can start directly from the documented module boundaries

## Development Principles

- ReBAC primary, RBAC fallback
- Deterministic authorization decisions
- Tenant isolation in every hot path
- Explainability for support and incident analysis
- MVP first, graph-ready next
