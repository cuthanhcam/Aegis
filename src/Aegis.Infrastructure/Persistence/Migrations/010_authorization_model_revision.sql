ALTER TABLE authorization_models
    ADD COLUMN IF NOT EXISTS revision BIGINT NOT NULL DEFAULT 1;

ALTER TABLE authorization_models
    ADD CONSTRAINT ck_authorization_models_revision_positive CHECK (revision > 0);
