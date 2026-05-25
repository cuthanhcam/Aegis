# Backend Docker Stack

This stack runs the Aegis API with Postgres for storage and Redis for authorization caching.

## Start

From the repository root:

```bash
$env:JWT_SECRET = "your-local-dev-secret"
docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml up --build
```

The stack includes a one-shot `migrate` service that applies schema migrations before the API starts serving traffic.

The Postgres container creates the `aegis` database automatically on first boot through `POSTGRES_DB=aegis`.

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
- Port: `5432`
- Database: `aegis`
- Username: `postgres`
- Password: `postgres`

Example connection string:

```text
Host=localhost;Port=5432;Database=aegis;Username=postgres;Password=postgres
```

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