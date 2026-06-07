# Database Schema Review

This document reviews the current PostgreSQL schema and defines the target schema direction for a platform-grade Aegis deployment.

## Current Strengths

The current schema already includes:

- stores;
- authorization models;
- relationship tuples;
- relationship change log;
- RBAC roles, users, permissions, and assignments;
- audit events;
- useful initial tenant/time indexes.

This is enough for local development and early ReBAC/RBAC exploration.

## Current Gaps

### Tenant and Store Boundary

Current `stores` do not carry `tenant_id`, while `relationships` carry `tenant_id` but not `store_id`.

Target behavior:

- tenant is the isolation boundary;
- store is the authorization namespace inside a tenant;
- model, tuple, RBAC, and audit data should carry both `tenant_id` and `store_id` when store-scoped.

### Opaque Tuple Columns

Current tuples store:

```text
subject text
relation text
object_ref text
```

Target tuples should store parsed typed references:

```text
subject_type text
subject_id text
subject_relation text null
relation text
object_type text
object_id text
```

Keep the public API string format, but persist typed columns.

Benefits:

- stronger indexes;
- model validation;
- lower parsing cost during graph traversal;
- easier list-users/list-objects implementation;
- cleaner audit and query filtering.

### Effect Type Mismatch

The migration currently defines `relationships.effect` as `SMALLINT`, while the infrastructure writes string values from `tuple.Effect.ToString()`.

The target schema should use either:

- `effect text not null check (effect in ('allow', 'deny'))`; or
- a PostgreSQL enum.

Use text first for simpler OSS migrations.

### Change Feed

Current `relationship_changes` uses offset pagination.

Target change feed should use monotonic revisions:

```text
revision bigint not null
```

Clients should read:

```http
GET /relationships/changes?afterRevision=123
```

### RBAC Integrity

Current RBAC tables do not fully enforce foreign keys.

Target:

- `rbac_user_roles` references users and roles;
- `rbac_role_permissions` references roles and permissions;
- all RBAC rows include `tenant_id` and `store_id`;
- deletes should be explicit and audited.

### Audit Detail

Current audit events store only action, tuple fields, decision, and reason.

Target audit events should include:

- `store_id`;
- `actor`;
- `request_id`;
- `authorization_model_id`;
- `revision`;
- `explanation_id`;
- `metadata jsonb`;
- redaction policy for request context.

## Target Core Tables

```sql
create table tenants (
    id text primary key,
    slug text not null unique,
    display_name text not null,
    status text not null,
    created_at timestamptz not null,
    updated_at timestamptz not null
);

create table stores (
    id text primary key,
    tenant_id text not null references tenants(id) on delete cascade,
    name text not null,
    current_authorization_model_id text null,
    created_at timestamptz not null,
    updated_at timestamptz not null,
    unique (tenant_id, name)
);

create table authorization_models (
    id text primary key,
    tenant_id text not null,
    store_id text not null references stores(id) on delete cascade,
    schema_version text not null,
    source text not null,
    parsed jsonb not null,
    status text not null,
    revision bigint not null,
    created_at timestamptz not null,
    activated_at timestamptz null
);

create table relationship_tuples (
    id uuid primary key,
    tenant_id text not null,
    store_id text not null,
    subject_type text not null,
    subject_id text not null,
    subject_relation text null,
    relation text not null,
    object_type text not null,
    object_id text not null,
    effect text not null,
    condition_name text null,
    condition_context jsonb null,
    revision bigint not null,
    created_at timestamptz not null,
    updated_at timestamptz not null,
    expires_at timestamptz null,
    check (effect in ('allow', 'deny'))
);
```

## Critical Indexes

Direct check:

```sql
create index ix_tuples_direct
on relationship_tuples (
    tenant_id,
    store_id,
    subject_type,
    subject_id,
    relation,
    object_type,
    object_id,
    effect
);
```

Reverse lookup:

```sql
create index ix_tuples_object_relation
on relationship_tuples (
    tenant_id,
    store_id,
    object_type,
    object_id,
    relation,
    effect
);
```

Subject expansion:

```sql
create index ix_tuples_subject_relation
on relationship_tuples (
    tenant_id,
    store_id,
    subject_type,
    subject_id,
    relation,
    effect
);
```

Change feed:

```sql
create index ix_relationship_changes_revision
on relationship_changes (
    tenant_id,
    store_id,
    revision
);
```

## Migration Strategy

Use additive migrations:

1. Add `tenant_id` to stores.
2. Add `store_id` to relationship, RBAC, and audit tables.
3. Add parsed tuple columns next to existing string columns.
4. Backfill parsed columns.
5. Add new indexes.
6. Move reads to new columns.
7. Move writes to new columns.
8. Remove old columns only after compatibility period.

Avoid destructive migrations before the public API and SDKs are stabilized.

