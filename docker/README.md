# Backend Docker Stack

This stack runs the Aegis API with Postgres for storage and Redis for authorization caching.

## Start

From the repository root:

PowerShell:

```bash
$env:POSTGRES_PASSWORD = "postgres"
$env:POSTGRES_PORT = "55432"
$env:REDIS_PORT = "6379"
$env:JWT_SECRET = "your-local-dev-secret"
$env:AEGIS_DEMO_ADMIN_PASSWORD = "admin-dev-password"
$env:AEGIS_DEMO_DEV_PASSWORD = "dev-password"
docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml up --build
```

You can use [docker/env.example](env.example) as the source for local values.

The stack includes a one-shot `migrate` service that applies schema migrations before the API starts serving traffic.

The Postgres container creates the `aegis` database automatically on first boot through `POSTGRES_DB=aegis`.

Redis is also started and used as the distributed authorization decision cache when `Cache__Provider=Redis`.

If you need a clean reset, use [docker/reset.ps1](docker/reset.ps1).

```powershell
.\docker\reset.ps1 -JwtSecret "your-local-dev-secret"
```

This stops the stack, removes the Postgres data volume, and recreates the database from scratch.

## Access the Database

Open an interactive `psql` session against the running container:

```bash
docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml exec postgres psql -U postgres -d aegis
```

If you prefer PowerShell, use [docker/psql.ps1](docker/psql.ps1).

If you want to connect from another client, use these values:

- Host: `localhost`
- Port: `55432` if you set `POSTGRES_PORT=55432`, otherwise `5432`
- Database: `aegis`
- Username: `postgres`
- Password: `postgres`

Example connection string:

```text
Host=localhost;Port=55432;Database=aegis;Username=postgres;Password=postgres
```

In DBeaver:

- Driver: PostgreSQL
- Host: `localhost`
- Port: `55432` if you set `POSTGRES_PORT=55432`, otherwise `5432`
- Database: `aegis`
- Username: `postgres`
- Password: the value of `POSTGRES_PASSWORD`

If the `aegis` database is not visible yet, start the stack first. PostgreSQL creates it during first container boot, and the `migrate` service creates the schema.

Common admin queries:

```sql
\dt
SELECT * FROM schema_migrations ORDER BY applied_at DESC;
SELECT COUNT(*) FROM stores;
```

## Notes

- Postgres stores the application data and schema migrations.
- Redis stores short-lived authorization decision cache entries.
- If you want to switch cache mode off Redis, set `Cache__Provider=Memory`.
- The development compose override expects `JWT_SECRET` to be supplied from your shell or an ignored local `.env` file such as `docker/.env.local`.
