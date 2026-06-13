ALTER TABLE rbac_roles
    ADD COLUMN IF NOT EXISTS store_id TEXT NULL;

UPDATE rbac_roles
SET store_id = tenant_id
WHERE store_id IS NULL OR store_id = '';

ALTER TABLE rbac_roles
    ALTER COLUMN store_id SET NOT NULL;

ALTER TABLE rbac_permissions
    ADD COLUMN IF NOT EXISTS store_id TEXT NULL;

UPDATE rbac_permissions
SET store_id = tenant_id
WHERE store_id IS NULL OR store_id = '';

ALTER TABLE rbac_permissions
    ALTER COLUMN store_id SET NOT NULL;

ALTER TABLE rbac_role_permissions
    ADD COLUMN IF NOT EXISTS store_id TEXT NULL;

UPDATE rbac_role_permissions
SET store_id = tenant_id
WHERE store_id IS NULL OR store_id = '';

ALTER TABLE rbac_role_permissions
    ALTER COLUMN store_id SET NOT NULL;

ALTER TABLE rbac_user_roles
    ADD COLUMN IF NOT EXISTS store_id TEXT NULL;

UPDATE rbac_user_roles
SET store_id = tenant_id
WHERE store_id IS NULL OR store_id = '';

ALTER TABLE rbac_user_roles
    ALTER COLUMN store_id SET NOT NULL;

ALTER TABLE rbac_roles
    DROP CONSTRAINT IF EXISTS rbac_roles_pkey;

ALTER TABLE rbac_roles
    ADD PRIMARY KEY (tenant_id, store_id, role_name);

ALTER TABLE rbac_permissions
    DROP CONSTRAINT IF EXISTS rbac_permissions_pkey;

ALTER TABLE rbac_permissions
    ADD PRIMARY KEY (tenant_id, store_id, relation, object_ref);

ALTER TABLE rbac_role_permissions
    DROP CONSTRAINT IF EXISTS rbac_role_permissions_pkey;

ALTER TABLE rbac_role_permissions
    ADD PRIMARY KEY (tenant_id, store_id, role_name, relation, object_ref);

ALTER TABLE rbac_user_roles
    DROP CONSTRAINT IF EXISTS rbac_user_roles_pkey;

ALTER TABLE rbac_user_roles
    ADD PRIMARY KEY (tenant_id, store_id, user_id, role_name);

CREATE INDEX IF NOT EXISTS ix_rbac_user_roles_tenant_store_user
ON rbac_user_roles (tenant_id, store_id, user_id);
