# Getting Started: Aegis Development

This guide gets the backend API and admin dashboard running locally.

## Prerequisites

- .NET 8 SDK
- Node.js 20 or newer
- pnpm 9 or newer
- Docker Desktop
- PowerShell

## Repository

```powershell
cd D:\Workspace\Aegis
```

## Configure Local Environment

The Docker stack requires a few environment variables.

```powershell
$env:POSTGRES_PASSWORD = "aegis-local-postgres"
$env:JWT_SECRET = "aegis-local-jwt-secret-change-me"
$env:AEGIS_DEMO_ADMIN_PASSWORD = "admin123"
$env:AEGIS_DEMO_DEV_PASSWORD = "dev123"
```

These values are for local development only.

## Start the Backend

```powershell
docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml up --build
```

This starts:

- PostgreSQL
- Redis
- Migration container
- Aegis API

The API is available at:

```text
http://localhost:5271
```

## Start the Frontend

In another terminal:

```powershell
pnpm --dir frontend install
pnpm --dir frontend --filter @aegis/admin-dashboard dev
```

The dashboard is available at:

```text
http://localhost:5173
```

## Login

Default tenant:

```text
username: admin
password: admin123
tenant: default
```

Development tenant:

```text
username: dev
password: dev123
tenant: tenant-dev
```

## Verify the API

Login:

```powershell
curl -X POST http://localhost:5271/api/v1/auth/login `
  -H "Content-Type: application/json" `
  -d "{\"username\":\"admin\",\"password\":\"admin123\"}"
```

Copy the access token from the response.

Run a check:

```powershell
curl -X POST http://localhost:5271/api/v1/stores/store-docs-default/check `
  -H "Authorization: Bearer <access-token>" `
  -H "Content-Type: application/json" `
  -d "{\"user\":\"user:anne\",\"relation\":\"viewer\",\"object\":\"document:roadmap\",\"consistency\":\"fully_consistent\"}"
```

Run explain:

```powershell
curl -X POST http://localhost:5271/api/v1/stores/store-docs-default/explain `
  -H "Authorization: Bearer <access-token>" `
  -H "Content-Type: application/json" `
  -d "{\"user\":\"user:anne\",\"relation\":\"viewer\",\"object\":\"document:roadmap\",\"consistency\":\"fully_consistent\"}"
```

## Demo Stores

Seed data creates stores that are useful for testing:

| Store | Tenant | Try |
| --- | --- | --- |
| `store-docs-default` | `default` | `user:anne viewer document:roadmap` |
| `store-support-default` | `default` | `user:agent1 viewer ticket:INC-1001` |
| `store-billing-default` | `default` | `user:finance viewer account:acme` |
| `store-lab-tenant-dev` | `tenant-dev` | `user:intern viewer project:aegis-lab` |
| `store-analytics-tenant-dev` | `tenant-dev` | `user:intern viewer dashboard:quality` |

See [Demo Data Guide](../reference/demo-data.md).

## Useful Commands

### Build Backend

```powershell
dotnet build Aegis.sln
```

### Run All Tests

```powershell
dotnet test
```

### Run Unit Tests

```powershell
dotnet test tests\Aegis.UnitTests\Aegis.UnitTests.csproj
```

### Run Integration Tests

```powershell
dotnet test tests\Aegis.IntegrationTests\Aegis.IntegrationTests.csproj
```

### Typecheck Dashboard

```powershell
pnpm --dir frontend --filter @aegis/admin-dashboard typecheck
```

### Build Dashboard

```powershell
pnpm --dir frontend --filter @aegis/admin-dashboard build
```

## Project Structure

```text
src/
  Aegis.Api              HTTP API and middleware
  Aegis.Application      Use cases and interfaces
  Aegis.Authorization    Authorization engine
  Aegis.Contracts        API contracts
  Aegis.Domain           Domain entities
  Aegis.Infrastructure   Persistence, cache, seed data
  Aegis.SharedKernel     Shared primitives

frontend/
  apps/admin-dashboard   React dashboard
  packages/api-client    TypeScript API client
  packages/types         Shared frontend types
  packages/ui            Shared UI primitives

tests/
  Aegis.UnitTests
  Aegis.IntegrationTests
```

## Debugging Tips

### API is not reachable

Check containers:

```powershell
docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml ps
```

Check logs:

```powershell
docker logs aegis-api
```

### Check returns deny

Use this sequence:

1. Confirm the active store.
2. Confirm the object type exists in that store's model.
3. Confirm the relation exists in the model.
4. Confirm the tuple exists.
5. Run explain.
6. Check audit events.

### Graph query returns `type_not_found`

The object type in the request does not exist in the active store's authorization model. For example, `document:roadmap` is valid in the docs store, but not in the support store.

### Frontend cannot call backend

Confirm:

- API is running at `http://localhost:5271`.
- Dashboard is running at `http://localhost:5173`.
- CORS allows `http://localhost:5173`.
- You are logged in and the token is not expired.

## Next Steps

- Read [User Guide](user-guide.md).
- Read [Core Concepts](../concepts/core-concepts-tuple-model.md).
- Read [API Reference](../reference/api-reference.md).
- Explore [Demo Data](../reference/demo-data.md).

