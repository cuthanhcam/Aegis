ALTER TABLE rbac_permissions
    ADD COLUMN IF NOT EXISTS condition_name TEXT NULL;

ALTER TABLE rbac_role_permissions
    ADD COLUMN IF NOT EXISTS condition_name TEXT NULL;
