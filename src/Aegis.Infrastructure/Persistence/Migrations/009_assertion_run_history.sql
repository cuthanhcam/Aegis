CREATE TABLE IF NOT EXISTS assertion_run_records (
    run_id TEXT PRIMARY KEY,
    store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
    authorization_model_id TEXT NOT NULL,
    started_at TIMESTAMPTZ NOT NULL,
    completed_at TIMESTAMPTZ NOT NULL,
    total INTEGER NOT NULL,
    passed INTEGER NOT NULL,
    failed INTEGER NOT NULL,
    results_json JSONB NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_assertion_run_records_store_model_started_at
    ON assertion_run_records (store_id, authorization_model_id, started_at DESC);
