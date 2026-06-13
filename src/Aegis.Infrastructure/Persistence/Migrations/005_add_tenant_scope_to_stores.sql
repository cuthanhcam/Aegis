ALTER TABLE stores
    ADD COLUMN IF NOT EXISTS tenant_id TEXT NULL;

UPDATE stores
SET tenant_id = id
WHERE tenant_id IS NULL OR tenant_id = '';

ALTER TABLE stores
    ALTER COLUMN tenant_id SET NOT NULL;

CREATE INDEX IF NOT EXISTS ix_stores_tenant_created_at
ON stores (tenant_id, created_at DESC);
