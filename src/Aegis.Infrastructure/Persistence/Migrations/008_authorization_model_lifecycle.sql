ALTER TABLE authorization_models
    ADD COLUMN IF NOT EXISTS state TEXT NOT NULL DEFAULT 'Draft',
    ADD COLUMN IF NOT EXISTS published_at TIMESTAMPTZ NULL,
    ADD COLUMN IF NOT EXISTS archived_at TIMESTAMPTZ NULL,
    ADD COLUMN IF NOT EXISTS superseded_by TEXT NULL;

UPDATE authorization_models
SET state = 'Draft'
WHERE state IS NULL OR state = '';

CREATE INDEX IF NOT EXISTS ix_authorization_models_store_state_published_at
    ON authorization_models(store_id, state, published_at DESC);
