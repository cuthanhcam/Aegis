CREATE TABLE IF NOT EXISTS assertion_sets (
    store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
    authorization_model_id TEXT NOT NULL REFERENCES authorization_models(id) ON DELETE CASCADE,
    revision BIGINT NOT NULL CHECK (revision > 0),
    assertions_json JSONB NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (store_id, authorization_model_id)
);

CREATE INDEX IF NOT EXISTS ix_assertion_sets_store_updated_at
    ON assertion_sets (store_id, updated_at DESC);
