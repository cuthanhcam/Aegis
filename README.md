# Aegis

> Centralized, explainable authorization for multi-tenant applications.

[![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET-Core-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis&logoColor=white)](https://redis.io/)
[![React](https://img.shields.io/badge/React-19-149ECA?logo=react&logoColor=white)](https://react.dev/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Aegis is an authorization platform for applications that need fine-grained, auditable, and explainable access control. It combines relationship-based authorization, role-based administration, conditional checks, graph queries, audit trails, and a modern admin dashboard.

The project is under active development. The backend and dashboard are already usable for local development, demos, and integration experiments.

## Why Aegis?

Modern applications often spread permission logic across services, controllers, database queries, and background jobs. That makes access decisions hard to reason about, hard to audit, and easy to drift.

Aegis moves authorization into a dedicated platform:

- Centralize access decisions across services.
- Model fine-grained permissions with relationship tuples.
- Explain why a request was allowed or denied.
- Keep authorization data isolated by tenant and store.
- Manage roles, permissions, users, and assignments.
- Explore access graphs with list-users, list-objects, and expand.
- Audit decisions and administrative changes.
- Integrate through Aegis-native APIs or OpenFGA-style compatibility endpoints.

## Features

- Store-scoped authorization workspaces.
- Authorization model registry and validation.
- ReBAC tuple checks with relation rewrites.
- RBAC roles, permissions, users, and assignments.
- Context-aware checks for conditional decisions.
- Check, explain, batch-check, list-users, list-objects, and expand APIs.
- OpenFGA-compatible check, batch-check, graph, and assertion routes.
- Relationship change history.
- Audit event query.
- Prometheus-style metrics endpoint.
- PostgreSQL and Redis-backed local development stack.
- React admin dashboard with store, model, relationship, graph, access, and audit workflows.

## Architecture

```text
Applications
    |
    v
Aegis API
    |
    v
Application Services
    |
    v
Authorization Engine
    |
    v
PostgreSQL + Redis
```

Repository layout:

```text
src/
  Aegis.Api              HTTP API, middleware, auth, health, metrics
  Aegis.Application      Use cases and service interfaces
  Aegis.Authorization    Authorization engine, ReBAC, RBAC, ABAC, cache
  Aegis.Contracts        Request/response contracts
  Aegis.Domain           Domain entities and value objects
  Aegis.Infrastructure   PostgreSQL, Redis, outbox, seed data
  Aegis.SharedKernel     Shared primitives and configuration

frontend/
  apps/admin-dashboard   React admin dashboard
  packages/api-client    TypeScript API client
  packages/types         Shared frontend contracts
  packages/ui            Shared UI package

docs/                    Product, user, API, operations, and architecture docs
tests/                   Unit and integration tests
docker/                  Local development containers
```

## Quick Start

### Prerequisites

- .NET 8 SDK
- Node.js 20+
- pnpm 9+
- Docker Desktop

### 1. Configure local secrets

PowerShell:

```powershell
$env:POSTGRES_PASSWORD = "aegis-local-postgres"
$env:JWT_SECRET = "aegis-local-jwt-secret-change-me"
$env:AEGIS_DEMO_ADMIN_PASSWORD = "admin123"
$env:AEGIS_DEMO_DEV_PASSWORD = "dev123"
```

### 2. Start backend dependencies and API

```powershell
docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml up --build
```

The API is exposed at:

```text
http://localhost:5271
```

### 3. Start the dashboard

```powershell
pnpm --dir frontend install
pnpm --dir frontend --filter @aegis/admin-dashboard dev
```

The dashboard runs at:

```text
http://localhost:5173
```

### 4. Login

Use the seeded demo account:

```text
username: admin
password: admin123
tenant: default
```

There is also a development tenant account:

```text
username: dev
password: dev123
tenant: tenant-dev
```

## First API Call

Login:

```powershell
curl -X POST http://localhost:5271/api/v1/auth/login `
  -H "Content-Type: application/json" `
  -d "{\"username\":\"admin\",\"password\":\"admin123\"}"
```

Use the returned access token as a bearer token, then run a store-scoped check:

```powershell
curl -X POST http://localhost:5271/api/v1/stores/store-docs-default/check `
  -H "Authorization: Bearer <access-token>" `
  -H "Content-Type: application/json" `
  -d "{\"user\":\"user:anne\",\"relation\":\"viewer\",\"object\":\"document:roadmap\",\"consistency\":\"fully_consistent\"}"
```

Expected result:

```json
{
  "success": true,
  "data": {
    "allowed": true,
    "decision": "ALLOW",
    "reasonCode": "ALLOW_REBAC_DIRECT"
  },
  "error": null
}
```

## Demo Data

Development seeding creates useful stores and relationships:

| Store | Tenant | Example |
| --- | --- | --- |
| `store-docs-default` | `default` | `user:anne viewer document:roadmap` |
| `store-support-default` | `default` | `user:agent1 viewer ticket:INC-1001` |
| `store-billing-default` | `default` | `user:finance viewer account:acme` |
| `store-lab-tenant-dev` | `tenant-dev` | `user:intern viewer project:aegis-lab` |
| `store-analytics-tenant-dev` | `tenant-dev` | `user:intern viewer dashboard:quality` |

See [Demo Data Guide](docs/reference/demo-data.md) for more examples.

## Documentation

- [Documentation Home](docs/README.md)
- [Product Overview](docs/product/product-overview.md)
- [User Guide](docs/guides/user-guide.md)
- [Core Concepts](docs/concepts/core-concepts-tuple-model.md)
- [API Reference](docs/reference/api-reference.md)
- [Quick Reference](docs/reference/quick-reference.md)
- [Development Setup](docs/guides/getting-started-development.md)
- [Deployment Operations Guide](docs/guides/deployment-operations-guide.md)
- [Architecture Overview](docs/architecture/README.md)
- [Documentation Strategy](docs/guides/documentation-strategy.md)

## Common Workflows

### Run tests

```powershell
dotnet test
```

### Build backend

```powershell
dotnet build Aegis.sln
```

### Typecheck dashboard

```powershell
pnpm --dir frontend --filter @aegis/admin-dashboard typecheck
```

### Build dashboard

```powershell
pnpm --dir frontend --filter @aegis/admin-dashboard build
```

## Roadmap

See [Aegis Roadmap](docs/product/roadmap.md) for the public roadmap.

High-level priorities:

- Model lifecycle, publishing, rollback, and assertion runner.
- Relationship revisions, idempotency, bulk import/export, and change streams.
- Enterprise auth, organizations, service accounts, and API keys.
- Audit, compliance, webhooks, quotas, and observability.
- API hardening and documentation-site readiness.

## Contributing

Contributions are welcome. Start with:

- [Contributing Guide](CONTRIBUTING.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)
- [Security Policy](SECURITY.md)
- [Architecture Docs](docs/architecture/README.md)

## License

Aegis is released under the [MIT License](LICENSE).
