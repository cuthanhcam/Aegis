DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'relationships'
          AND column_name = 'effect'
          AND data_type <> 'text'
    ) THEN
        ALTER TABLE relationships
            ALTER COLUMN effect TYPE TEXT
            USING CASE effect::TEXT
                WHEN '0' THEN 'Allow'
                WHEN '1' THEN 'Deny'
                ELSE effect::TEXT
            END;
    END IF;
END $$;

ALTER TABLE relationships
    DROP CONSTRAINT IF EXISTS ck_relationships_effect;

ALTER TABLE relationships
    ADD CONSTRAINT ck_relationships_effect
    CHECK (effect IN ('Allow', 'Deny'));

CREATE INDEX IF NOT EXISTS ix_relationships_direct_lookup
ON relationships (tenant_id, subject, relation, object_ref, effect);

CREATE INDEX IF NOT EXISTS ix_relationships_object_relation
ON relationships (tenant_id, object_ref, relation, effect);

CREATE INDEX IF NOT EXISTS ix_relationships_subject_relation
ON relationships (tenant_id, subject, relation, effect);
