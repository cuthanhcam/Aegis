# Aegis Documentation

Welcome to the Aegis documentation.

Aegis is a centralized authorization platform. These docs are written like product documentation: what Aegis is, how to use it, how to integrate with it, and how to operate it.

Substantial topics follow the [article metadata and content contract](article-metadata-schema.md): mental model, concrete examples, security and failure implications, verification, and next reading.

## Start Here

| Goal                              | Read                                                                   |
| --------------------------------- | ---------------------------------------------------------------------- |
| Understand Aegis as a product     | [Product Overview](product/product-overview.md)                        |
| Understand the system boundaries  | [System Architecture](architecture/system-architecture.md)             |
| Learn the authorization model     | [Core Concepts](concepts/README.md)                                    |
| Understand repeatable decisions   | [Deterministic Authorization](concepts/deterministic-authorization.md) |
| Review tenant security boundaries | [Tenant and Store Isolation](concepts/tenant-store-isolation.md)       |
| Build the replacement console     | [Frontend Console Architecture](architecture/frontend-console.md)      |
| Use Aegis day to day              | [User Guide](guides/user-guide.md)                                     |
| Use the admin dashboard           | [Dashboard Guide](guides/dashboard-guide.md)                           |
| Integrate an application          | [API Integration Guide](guides/api-integration.md)                     |
| Look up endpoints                 | [API Reference](reference/api-reference.md)                            |
| Try seeded examples               | [Demo Data Guide](reference/demo-data.md)                              |
| Run Aegis locally                 | [Local Development Guide](guides/getting-started-development.md)       |
| Deploy and operate Aegis          | [Deployment Guide](operations/deployment.md)                           |
| Prepare for production            | [Operating Aegis in Production](operations/production-readiness.md)    |
| See planned platform work         | [Roadmap](product/roadmap.md)                                          |

Architecture rationale is recorded in the [Architecture Decision Records](decisions/README.md). Contributors should follow [Engineering Governance](development/engineering-governance.md) so decisions, verification evidence, and unfinished work stay traceable.

Backend contributors should also maintain the [Backend Runtime Inventory](architecture/backend-runtime-inventory.md), which maps HTTP, configuration, persistence, cache, worker, health, and metrics surfaces to their remaining product-readiness work.

## Documentation Structure

```text
docs/
|-- README.md
|-- index.md
|-- article-metadata-schema.md
|-- architecture/
|   |-- system-architecture.md
|   `-- frontend-console.md
|-- product/
|   |-- product-overview.md
|   `-- roadmap.md
|-- concepts/
|   |-- README.md
|   |-- core-concepts-tuple-model.md
|   |-- deterministic-authorization.md
|   `-- tenant-store-isolation.md
|-- guides/
|   |-- user-guide.md
|   |-- dashboard-guide.md
|   |-- api-integration.md
|   `-- getting-started-development.md
|-- reference/
|   |-- api-reference.md
|   |-- quick-reference.md
|   `-- demo-data.md
`-- operations/
    |-- deployment.md
    `-- production-readiness.md
```

## Reading Paths

### I am evaluating Aegis

1. [Product Overview](product/product-overview.md)
2. [Core Concepts](concepts/README.md)
3. [Roadmap](product/roadmap.md)

### I want to use Aegis

1. [User Guide](guides/user-guide.md)
2. [Dashboard Guide](guides/dashboard-guide.md)
3. [Demo Data Guide](reference/demo-data.md)

### I want to integrate my app

1. [Deterministic Authorization](concepts/deterministic-authorization.md)
2. [API Integration Guide](guides/api-integration.md)
3. [API Reference](reference/api-reference.md)
4. [Quick Reference](reference/quick-reference.md)

### I want to run Aegis

1. [Local Development Guide](guides/getting-started-development.md)
2. [Deployment Guide](operations/deployment.md)
3. [Operating Aegis in Production](operations/production-readiness.md)
4. [API Reference](reference/api-reference.md)

### I want to develop Aegis

1. [System Architecture](architecture/system-architecture.md)
2. [Tenant and Store Isolation](concepts/tenant-store-isolation.md)
3. [Frontend Console Architecture](architecture/frontend-console.md)
4. [Product-readiness Planning Pack](../temp/README.md)
