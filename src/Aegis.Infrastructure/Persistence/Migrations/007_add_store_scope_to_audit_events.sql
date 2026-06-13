ALTER TABLE audit_events
    ADD COLUMN IF NOT EXISTS store_id TEXT NULL;

CREATE INDEX IF NOT EXISTS ix_audit_events_tenant_store_created_at
    ON audit_events(tenant_id, store_id, created_at DESC);
