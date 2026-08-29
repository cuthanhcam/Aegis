# Deployment Guide

This guide describes the runtime pieces required to operate Aegis.

## Runtime Components

Aegis uses:

- Aegis API
- PostgreSQL
- Redis
- Admin dashboard

PostgreSQL is the source of truth for stores, models, relationships, RBAC data, audit events, and development seed data. Redis is used for cache-backed runtime behavior when enabled.

## Required Configuration

Core settings:

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings__Aegis` | PostgreSQL connection string |
| `Jwt__Secret` | JWT signing secret for local/demo auth |
| `Storage__Provider` | Storage backend, usually `Postgres` |
| `Cache__Provider` | Cache backend, usually `Redis` |
| `Cache__Redis__Configuration` | Redis connection |
| `Cors__AllowedOrigins__0` | Dashboard origin |

## Health Checks

Aegis exposes health endpoints:

```text
GET /health/live
GET /health/ready
```

Use liveness to detect whether the process is running. Use readiness to detect whether dependencies are available.

## Metrics

Aegis exposes metrics at:

```text
GET /metrics
```

Metrics include authorization engine counters such as memo cache and model parse cache behavior.

## Logging

Request logs include method, path, status, duration, endpoint, tenant, user, trace id, request id, remote IP, and error code.

HTTP body logging is disabled by default and should only be enabled deliberately because request bodies can contain sensitive authorization data.

## Migrations and Seed Data

Local development can run migrations and seed demo data automatically through the development Docker stack.

Production deployments should:

- Run `eng/migrate-database.ps1` explicitly with a migration-only database identity before starting replicas.
- Configure API replicas with `Database__Migrations__Mode=Validate` and a runtime identity without DDL grants.
- Keep seed data disabled unless intentionally bootstrapping a demo environment.
- Back up PostgreSQL before schema changes.

`Apply` remains the default for backward-compatible single-service and local operation. Selecting `Validate` makes startup read-only with respect to schema history and fails closed if migrations are missing, pending, or inconsistent. Do not switch managed replicas to `Validate` until the separate migration step and rollback ordering are proven in that environment.

## Operational Checklist

- [ ] PostgreSQL is reachable.
- [ ] Redis is reachable if cache provider is Redis.
- [ ] JWT/auth configuration is set.
- [ ] Dashboard origin is allowed by CORS.
- [ ] Migrations have run.
- [ ] Health endpoints are monitored.
- [ ] Metrics are scraped.
- [ ] Logs include trace and request ids.

