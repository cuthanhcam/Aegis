CREATE UNIQUE INDEX IF NOT EXISTS ux_stores_tenant_id_id
    ON stores (tenant_id, id);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_relationships_store' AND conrelid = 'relationships'::regclass) THEN
        ALTER TABLE relationships
            ADD CONSTRAINT fk_relationships_store
            FOREIGN KEY (tenant_id, store_id) REFERENCES stores (tenant_id, id)
            ON DELETE CASCADE NOT VALID;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_relationship_changes_store' AND conrelid = 'relationship_changes'::regclass) THEN
        ALTER TABLE relationship_changes
            ADD CONSTRAINT fk_relationship_changes_store
            FOREIGN KEY (tenant_id, store_id) REFERENCES stores (tenant_id, id)
            ON DELETE CASCADE NOT VALID;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_rbac_roles_store' AND conrelid = 'rbac_roles'::regclass) THEN
        ALTER TABLE rbac_roles
            ADD CONSTRAINT fk_rbac_roles_store
            FOREIGN KEY (tenant_id, store_id) REFERENCES stores (tenant_id, id)
            ON DELETE CASCADE NOT VALID;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_rbac_permissions_store' AND conrelid = 'rbac_permissions'::regclass) THEN
        ALTER TABLE rbac_permissions
            ADD CONSTRAINT fk_rbac_permissions_store
            FOREIGN KEY (tenant_id, store_id) REFERENCES stores (tenant_id, id)
            ON DELETE CASCADE NOT VALID;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_rbac_role_permissions_store' AND conrelid = 'rbac_role_permissions'::regclass) THEN
        ALTER TABLE rbac_role_permissions
            ADD CONSTRAINT fk_rbac_role_permissions_store
            FOREIGN KEY (tenant_id, store_id) REFERENCES stores (tenant_id, id)
            ON DELETE CASCADE NOT VALID;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_rbac_user_roles_store' AND conrelid = 'rbac_user_roles'::regclass) THEN
        ALTER TABLE rbac_user_roles
            ADD CONSTRAINT fk_rbac_user_roles_store
            FOREIGN KEY (tenant_id, store_id) REFERENCES stores (tenant_id, id)
            ON DELETE CASCADE NOT VALID;
    END IF;
END $$;
