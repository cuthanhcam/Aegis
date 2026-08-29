ALTER TABLE assertion_run_records
    ADD COLUMN IF NOT EXISTS definition_revision BIGINT NOT NULL DEFAULT 0;

ALTER TABLE assertion_run_records
    DROP CONSTRAINT IF EXISTS ck_assertion_run_records_definition_revision;

ALTER TABLE assertion_run_records
    ADD CONSTRAINT ck_assertion_run_records_definition_revision
    CHECK (definition_revision >= 0);
