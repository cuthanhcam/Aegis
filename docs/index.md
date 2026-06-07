# Aegis Documentation Index

This index helps you navigate Aegis documentation by role and task.

## Start Here

If you are new to Aegis:

- Read [Overview](Overview.md)
- Read [Product Overview](product/product-overview.md)

If you need implementation details quickly:

- Read [Getting Started](guides/getting-started-development.md)
- Read [API Reference](reference/api-reference.md)
- Use [Quick Reference](reference/quick-reference.md)

If you are improving Aegis as an OSS authorization platform:

- Read [Target Architecture](architecture/target-architecture.md)
- Read [Domain Model](architecture/domain-model.md)
- Read [Public API Design](architecture/public-api-design.md)
- Read [Database Schema Review](architecture/database-schema-review.md)
- Read [Database, Migrations, and Docker](operations/database-migrations-and-docker.md)

## Documentation Structure

```text
docs/
|-- index.md
|-- Overview.md
|-- product/
|   `-- product-overview.md
|-- concepts/
|   `-- core-concepts-tuple-model.md
|-- reference/
|   |-- api-reference.md
|   `-- quick-reference.md
|-- guides/
|   |-- getting-started-development.md
|   `-- deployment-operations-guide.md
|-- operations/
|   `-- database-migrations-and-docker.md
|-- architecture/
|   |-- README.md
|   |-- target-architecture.md
|   |-- domain-model.md
|   |-- public-api-design.md
|   |-- database-schema-review.md
|   |-- performance-plan.md
|   |-- testing-plan.md
|   |-- project-structure.md
|   |-- permission-engine.md
|   `-- database-design.md
|-- adr/
|   |-- 0001-rebac-is-primary.md
|   |-- 0002-tenant-and-store-boundary.md
|   |-- 0003-postgresql-source-of-truth.md
|   `-- 0004-explainability-is-public-contract.md
`-- frontend/
    |-- README.md
    `-- tracking.md
```

## By Audience

### Product and Business

- [Product Overview](product/product-overview.md)
- [Overview](Overview.md)

### Developers

- [Overview](Overview.md)
- [Getting Started](guides/getting-started-development.md)
- [Core Concepts](concepts/core-concepts-tuple-model.md)
- [API Reference](reference/api-reference.md)
- [Quick Reference](reference/quick-reference.md)

### DevOps and SRE

- [Deployment Operations Guide](guides/deployment-operations-guide.md)
- [Database, Migrations, and Docker](operations/database-migrations-and-docker.md)
- [Database Design](architecture/database-design.md)
- [Architecture README](architecture/README.md)

### Architects and Tech Leads

- [Architecture README](architecture/README.md)
- [Target Architecture](architecture/target-architecture.md)
- [Domain Model](architecture/domain-model.md)
- [Public API Design](architecture/public-api-design.md)
- [Database Schema Review](architecture/database-schema-review.md)
- [Performance Plan](architecture/performance-plan.md)
- [Testing Plan](architecture/testing-plan.md)
- [Project Structure](architecture/project-structure.md)
- [Permission Engine](architecture/permission-engine.md)
- [Core Concepts](concepts/core-concepts-tuple-model.md)

### OSS Contributors

- [ADR 0001: ReBAC Is Primary](adr/0001-rebac-is-primary.md)
- [ADR 0002: Tenant and Store Boundary](adr/0002-tenant-and-store-boundary.md)
- [ADR 0003: PostgreSQL Source of Truth](adr/0003-postgresql-source-of-truth.md)
- [ADR 0004: Explainability Contract](adr/0004-explainability-is-public-contract.md)

### Frontend

- [Frontend README](frontend/README.md)
- [Frontend Tracking](frontend/tracking.md)

## Reading Paths

### Understand Aegis in 30 minutes

1. [Overview](Overview.md)
2. [Product Overview](product/product-overview.md)
3. [Core Concepts](concepts/core-concepts-tuple-model.md)

### Build or integrate quickly

1. [Getting Started](guides/getting-started-development.md)
2. [API Reference](reference/api-reference.md)
3. [Quick Reference](reference/quick-reference.md)

### Deploy and operate

1. [Deployment Operations Guide](guides/deployment-operations-guide.md)
2. [Database, Migrations, and Docker](operations/database-migrations-and-docker.md)
3. [Database Design](architecture/database-design.md)
4. [Architecture README](architecture/README.md)

### Improve Aegis without rewriting it

1. [Target Architecture](architecture/target-architecture.md)
2. [Domain Model](architecture/domain-model.md)
3. [Database Schema Review](architecture/database-schema-review.md)
4. [Performance Plan](architecture/performance-plan.md)
5. [Testing Plan](architecture/testing-plan.md)

## Quick Answers

- Permission check endpoint: [API Reference](reference/api-reference.md)
- Decision debugging flow: [Quick Reference](reference/quick-reference.md)
- Tuple model and ReBAC/RBAC: [Core Concepts](concepts/core-concepts-tuple-model.md)
- Layering and dependency rules: [Project Structure](architecture/project-structure.md)
- Local database and migrations: [Database, Migrations, and Docker](operations/database-migrations-and-docker.md)

## Support

- Contribution guide: [CONTRIBUTING](../CONTRIBUTING.md)
- Security policy: [SECURITY](../SECURITY.md)
- Code of conduct: [CODE_OF_CONDUCT](../CODE_OF_CONDUCT.md)
