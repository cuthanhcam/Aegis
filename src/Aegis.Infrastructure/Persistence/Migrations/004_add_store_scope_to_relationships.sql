ALTER TABLE relationships
    ADD COLUMN IF NOT EXISTS store_id TEXT NULL;

UPDATE relationships
SET store_id = tenant_id
WHERE store_id IS NULL OR store_id = '';

ALTER TABLE relationships
    ALTER COLUMN store_id SET NOT NULL;

ALTER TABLE relationship_changes
    ADD COLUMN IF NOT EXISTS store_id TEXT NULL;

UPDATE relationship_changes
SET store_id = tenant_id
WHERE store_id IS NULL OR store_id = '';

ALTER TABLE relationship_changes
    ALTER COLUMN store_id SET NOT NULL;

ALTER TABLE relationships
    DROP CONSTRAINT IF EXISTS relationships_tenant_id_subject_relation_object_ref_key;

ALTER TABLE relationships
    ADD CONSTRAINT relationships_tenant_store_subject_relation_object_key
    UNIQUE (tenant_id, store_id, subject, relation, object_ref);

DROP INDEX IF EXISTS ix_relationships_tenant_created_at;
DROP INDEX IF EXISTS ix_relationship_changes_tenant_created_at;
DROP INDEX IF EXISTS ix_relationships_direct_lookup;
DROP INDEX IF EXISTS ix_relationships_object_relation;
DROP INDEX IF EXISTS ix_relationships_subject_relation;

CREATE INDEX IF NOT EXISTS ix_relationships_tenant_store_created_at
ON relationships (tenant_id, store_id, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_relationships_direct_lookup
ON relationships (tenant_id, store_id, subject, relation, object_ref, effect);

CREATE INDEX IF NOT EXISTS ix_relationships_object_relation
ON relationships (tenant_id, store_id, object_ref, relation, effect);

CREATE INDEX IF NOT EXISTS ix_relationships_subject_relation
ON relationships (tenant_id, store_id, subject, relation, effect);

CREATE INDEX IF NOT EXISTS ix_relationship_changes_tenant_store_created_at
ON relationship_changes (tenant_id, store_id, created_at ASC);
