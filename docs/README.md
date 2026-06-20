# Aegis Documentation

Aegis is an authorization platform for building centralized, explainable access control across applications, tenants, and services.

This documentation set is organized so it can work in three places:

- As repository documentation for contributors.
- As source content for a future public documentation website.
- As embeddable help content inside the Aegis admin dashboard.

## Start Here

| Goal | Read |
| --- | --- |
| Understand what Aegis is | [Product Overview](product/product-overview.md) |
| Learn the core model | [Core Concepts](concepts/core-concepts-tuple-model.md) |
| Use Aegis through the dashboard and API | [User Guide](guides/user-guide.md) |
| Run Aegis locally | [Development Setup](guides/getting-started-development.md) |
| Find API endpoints | [API Reference](reference/api-reference.md) |
| Use seeded demo data | [Demo Data Guide](reference/demo-data.md) |
| See planned platform work | [Roadmap](product/roadmap.md) |
| Deploy and operate Aegis | [Deployment Operations Guide](guides/deployment-operations-guide.md) |
| Understand the backend design | [Architecture Overview](architecture/README.md) |

## Documentation Map

```text
docs/
|-- README.md                         Documentation home
|-- index.md                          Full navigation index
|-- product/                          Product positioning and use cases
|-- concepts/                         Authorization concepts and mental model
|-- guides/                           How-to guides for users and operators
|-- reference/                        API, demo data, and quick references
|-- architecture/                     Backend/frontend architecture details
|-- operations/                       Database, migrations, and runtime operations
|-- adr/                              Architecture decision records
`-- frontend/                         Frontend implementation notes
```

## What Aegis Does

Aegis centralizes authorization decisions. Instead of hardcoding permission logic in every product service, applications ask Aegis:

```text
Can user:anne view document:roadmap?
```

Aegis evaluates the request against:

- Relationship tuples, such as `user:anne viewer document:roadmap`.
- Authorization models, such as `viewer` being derived from `editor` or `owner`.
- Role assignments and role permissions.
- Optional request context for conditional decisions.

It returns a deterministic allow or deny decision, and can explain why the decision happened.

## Core Product Areas

- Stores: isolated authorization workspaces for applications, teams, environments, or tenants.
- Authorization models: schemas that define object types, relations, and relation rewrites.
- Relationships: tuples that express who has which relation to which object.
- Checks: runtime permission decisions.
- Explain: decision traces for debugging and audits.
- Graph queries: list users, list objects, and expand relationship trees.
- RBAC administration: roles, permissions, users, and assignments.
- Audit: historical records of decisions and administrative changes.
- Compatibility: OpenFGA-style endpoints for easier integration with familiar authorization patterns.

## Recommended Reading Paths

### New User

1. [Product Overview](product/product-overview.md)
2. [Core Concepts](concepts/core-concepts-tuple-model.md)
3. [User Guide](guides/user-guide.md)
4. [Demo Data Guide](reference/demo-data.md)

### Backend Developer

1. [Development Setup](guides/getting-started-development.md)
2. [API Reference](reference/api-reference.md)
3. [Architecture Overview](architecture/README.md)
4. [Permission Engine](architecture/permission-engine.md)
5. [Database Design](architecture/database-design.md)

### Frontend Developer

1. [User Guide](guides/user-guide.md)
2. [Frontend README](frontend/README.md)
3. [Documentation Strategy](guides/documentation-strategy.md)
4. [API Reference](reference/api-reference.md)

### Operator

1. [Deployment Operations Guide](guides/deployment-operations-guide.md)
2. [Database, Migrations, and Docker](operations/database-migrations-and-docker.md)
3. [Database Design](architecture/database-design.md)
4. [Performance Plan](architecture/performance-plan.md)

## Documentation Principles

- Prefer runnable examples over abstract descriptions.
- Keep API examples aligned with backend controllers and contracts.
- Separate user-facing docs from architecture notes.
- Keep documentation reusable for a future docs site.
- Keep dashboard-help pages short and task-oriented.
