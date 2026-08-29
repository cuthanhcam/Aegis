CREATE TABLE IF NOT EXISTS outbox_messages (
    id UUID PRIMARY KEY,
    event_type TEXT NOT NULL,
    payload JSONB NOT NULL,
    occurred_on TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    attempt_count INTEGER NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
    last_error TEXT NULL,
    next_attempt_at TIMESTAMPTZ NOT NULL,
    processed_at TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_outbox_messages_pending
    ON outbox_messages (next_attempt_at, created_at, id)
    WHERE processed_at IS NULL;
