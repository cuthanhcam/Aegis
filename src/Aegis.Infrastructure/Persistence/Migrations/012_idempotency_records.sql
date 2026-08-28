CREATE TABLE IF NOT EXISTS idempotency_records (
    tenant_id TEXT NOT NULL,
    actor_id TEXT NOT NULL,
    store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
    operation TEXT NOT NULL,
    idempotency_key TEXT NOT NULL,
    request_fingerprint CHAR(64) NOT NULL,
    response_json JSONB NULL,
    created_at TIMESTAMPTZ NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (tenant_id, actor_id, store_id, operation, idempotency_key),
    CONSTRAINT ck_idempotency_key_length CHECK (char_length(idempotency_key) BETWEEN 8 AND 128),
    CONSTRAINT ck_idempotency_fingerprint_length CHECK (char_length(request_fingerprint) = 64)
);

CREATE INDEX IF NOT EXISTS ix_idempotency_records_expires_at
    ON idempotency_records(expires_at);
