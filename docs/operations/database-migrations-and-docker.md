# Database, Migrations, and Docker

This guide documents how Aegis currently manages PostgreSQL schema changes and how to run the database with Docker.

## Current Approach

Aegis currently uses SQL migrations as code.

It does not use EF Core Code First migrations today.

Current flow:

```text
src/Aegis.Infrastructure/Persistence/Migrations/*.sql
  -> embedded into Aegis.Infrastructure
  -> PostgresMigrationRunner
  -> schema_migrations table
  -> PostgreSQL
```

This is a good fit for Aegis right now because most persistence code uses explicit SQL via Npgsql. It keeps database changes predictable and avoids introducing an ORM only for migrations.

## How Migrations Run

Migrations are applied by:

- API startup when `Storage:Provider` is `Postgres`;
- the Docker `migrate` service when running with `--migrate-only`;
- `docker/migrate.ps1` for local PowerShell workflows.

The migration runner records applied files in:

```sql
schema_migrations
```

You can inspect applied migrations:

```sql
select migration_name, applied_at
from schema_migrations
order by applied_at;
```

## Required Configuration

Set storage provider:

```json
{
  "Storage": {
    "Provider": "Postgres"
  }
}
```

Set connection string:

```json
{
  "ConnectionStrings": {
    "Aegis": "Host=localhost;Port=5432;Database=aegis;Username=postgres;Password=postgres"
  }
}
```

For Docker Compose, this is supplied through environment variables:

```text
ConnectionStrings__Aegis=Host=postgres;Port=5432;Database=aegis;Username=postgres;Password=<password>
```

## Run PostgreSQL and Redis with Docker

From the repository root:

```powershell
$env:POSTGRES_PASSWORD = "postgres"
$env:JWT_SECRET = "replace-with-local-dev-secret"
$env:AEGIS_DEMO_ADMIN_PASSWORD = "admin-dev-password"
$env:AEGIS_DEMO_DEV_PASSWORD = "dev-password"

docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml up --build
```

Services:

- `postgres`: PostgreSQL database.
- `redis`: Redis cache.
- `migrate`: one-shot migration runner.
- `api`: Aegis API.

## Run Migrations Only

```powershell
$env:POSTGRES_PASSWORD = "postgres"
$env:JWT_SECRET = "replace-with-local-dev-secret"
$env:AEGIS_DEMO_ADMIN_PASSWORD = "admin-dev-password"
$env:AEGIS_DEMO_DEV_PASSWORD = "dev-password"

docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml up --build migrate
```

Or use:

```powershell
.\docker\migrate.ps1 -JwtSecret "replace-with-local-dev-secret"
```

## Connect to the Docker Database

```powershell
docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml exec postgres psql -U postgres -d aegis
```

Useful checks:

```sql
\dt
select * from schema_migrations order by applied_at desc;
select count(*) from relationships;
```

## Add a New Migration

Create a new SQL file:

```text
src/Aegis.Infrastructure/Persistence/Migrations/003_descriptive_name.sql
```

Guidelines:

- use a numeric prefix;
- make migrations additive when possible;
- use `IF NOT EXISTS` for tables and indexes;
- use `ADD COLUMN IF NOT EXISTS` for additive columns;
- avoid destructive changes until compatibility is planned;
- include indexes required by new query paths.

Example:

```sql
alter table relationships
    add column if not exists store_id text null;

create index if not exists ix_relationships_tenant_store_object_relation
on relationships (tenant_id, store_id, object_ref, relation);
```

Run:

```powershell
.\docker\migrate.ps1
```

## Reset Local Database

This removes the Docker volume unless `-KeepData` is supplied.

```powershell
.\docker\reset.ps1 -JwtSecret "replace-with-local-dev-secret"
```

## Should Aegis Use EF Core Code First?

Not immediately.

Current persistence is explicit SQL through Npgsql. Adding EF Core only for migrations would increase moving parts without improving the authorization engine.

Consider EF Core Code First later if Aegis decides to:

- map domain aggregates through EF;
- use DbContext transaction boundaries broadly;
- generate migrations from entity configuration;
- standardize repository implementation around EF.

Until then, prefer SQL migrations as code.

## Compatibility Position

The current migration system already supports the main workflow people expect from Code First:

- schema changes live in source control;
- a connection string controls the target database;
- migrations run automatically in Docker;
- migration history is stored in the database.

The difference is that SQL is written explicitly rather than generated from C# entity classes.

