CREATE TABLE IF NOT EXISTS stores (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE IF NOT EXISTS authorization_models (
    id TEXT PRIMARY KEY,
    store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
    schema_version TEXT NOT NULL,
    model TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_authorization_models_store_created_at ON authorization_models(store_id, created_at DESC);

CREATE TABLE IF NOT EXISTS relationships (
    id UUID PRIMARY KEY,
    tenant_id TEXT NOT NULL,
    store_id TEXT NOT NULL,
    subject TEXT NOT NULL,
    relation TEXT NOT NULL,
    object_ref TEXT NOT NULL,
    effect TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    UNIQUE (tenant_id, store_id, subject, relation, object_ref)
);

ALTER TABLE relationships
    ADD CONSTRAINT ck_relationships_effect
    CHECK (effect IN ('Allow', 'Deny'));

CREATE INDEX IF NOT EXISTS ix_relationships_tenant_store_created_at ON relationships(tenant_id, store_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_relationships_direct_lookup ON relationships(tenant_id, store_id, subject, relation, object_ref, effect);
CREATE INDEX IF NOT EXISTS ix_relationships_object_relation ON relationships(tenant_id, store_id, object_ref, relation, effect);
CREATE INDEX IF NOT EXISTS ix_relationships_subject_relation ON relationships(tenant_id, store_id, subject, relation, effect);

CREATE TABLE IF NOT EXISTS relationship_changes (
    id UUID PRIMARY KEY,
    tenant_id TEXT NOT NULL,
    store_id TEXT NOT NULL,
    subject TEXT NOT NULL,
    relation TEXT NOT NULL,
    object_ref TEXT NOT NULL,
    operation TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_relationship_changes_tenant_store_created_at ON relationship_changes(tenant_id, store_id, created_at ASC);

CREATE TABLE IF NOT EXISTS rbac_roles (
    tenant_id TEXT NOT NULL,
    role_name TEXT NOT NULL,
    description TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (tenant_id, role_name)
);

CREATE TABLE IF NOT EXISTS rbac_permissions (
    tenant_id TEXT NOT NULL,
    relation TEXT NOT NULL,
    object_ref TEXT NOT NULL,
    condition_name TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (tenant_id, relation, object_ref)
);

CREATE TABLE IF NOT EXISTS rbac_role_permissions (
    tenant_id TEXT NOT NULL,
    role_name TEXT NOT NULL,
    relation TEXT NOT NULL,
    object_ref TEXT NOT NULL,
    condition_name TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (tenant_id, role_name, relation, object_ref)
);

CREATE TABLE IF NOT EXISTS rbac_users (
    tenant_id TEXT NOT NULL,
    user_id TEXT NOT NULL,
    email TEXT NULL,
    display_name TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (tenant_id, user_id)
);

CREATE TABLE IF NOT EXISTS rbac_user_roles (
    tenant_id TEXT NOT NULL,
    user_id TEXT NOT NULL,
    role_name TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (tenant_id, user_id, role_name)
);

CREATE TABLE IF NOT EXISTS audit_events (
    id UUID PRIMARY KEY,
    tenant_id TEXT NOT NULL,
    action TEXT NOT NULL,
    subject TEXT NOT NULL,
    relation TEXT NOT NULL,
    object_ref TEXT NOT NULL,
    decision TEXT NOT NULL,
    reason_code TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_audit_events_tenant_created_at ON audit_events(tenant_id, created_at DESC);
