using Npgsql;

namespace Aegis.Infrastructure.Persistence
{
    internal static class PostgresDevelopmentSeeder
    {
        public static async Task SeedAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken = default)
        {
            const string sql = """
                INSERT INTO stores (id, tenant_id, name, created_at, updated_at)
                VALUES
                    ('store-docs-default', 'default', 'Documents Workspace', NOW() - INTERVAL '9 days', NOW() - INTERVAL '1 day'),
                    ('store-billing-default', 'default', 'Billing Console', NOW() - INTERVAL '8 days', NOW() - INTERVAL '2 days'),
                    ('store-support-default', 'default', 'Support Portal', NOW() - INTERVAL '7 days', NOW() - INTERVAL '3 days'),
                    ('store-lab-tenant-dev', 'tenant-dev', 'Developer Sandbox', NOW() - INTERVAL '6 days', NOW() - INTERVAL '1 day'),
                    ('store-analytics-tenant-dev', 'tenant-dev', 'Analytics Workspace', NOW() - INTERVAL '5 days', NOW() - INTERVAL '2 days')
                ON CONFLICT (id) DO UPDATE
                SET tenant_id = EXCLUDED.tenant_id,
                    name = EXCLUDED.name,
                    updated_at = EXCLUDED.updated_at;

                INSERT INTO authorization_models (id, store_id, schema_version, model, created_at)
                VALUES
                    ('model-docs-v1', 'store-docs-default', '1.1',
                'type user
                type team
                  define member: [user]
                type document
                  define owner: [user]
                  define editor: [user, team#member] or owner
                  define viewer: [user, team#member] or editor',
                    NOW() - INTERVAL '8 days'),
                    ('model-billing-v1', 'store-billing-default', '1.1',
                'type user
                type account
                  define admin: [user]
                  define analyst: [user]
                  define viewer: [user] or analyst or admin',
                    NOW() - INTERVAL '7 days'),
                    ('model-support-v1', 'store-support-default', '1.1',
                'type user
                type queue
                  define manager: [user]
                  define agent: [user]
                  define viewer: [user] or agent or manager
                type ticket
                  define assignee: [user]
                  define viewer: [user] or assignee',
                    NOW() - INTERVAL '6 days'),
                    ('model-dev-v1', 'store-lab-tenant-dev', '1.1',
                'type user
                type project
                  define maintainer: [user]
                  define contributor: [user]
                  define viewer: [user] or contributor or maintainer',
                    NOW() - INTERVAL '5 days')
                ON CONFLICT (id) DO UPDATE
                SET schema_version = EXCLUDED.schema_version,
                    model = EXCLUDED.model;

                INSERT INTO relationships (id, tenant_id, store_id, subject, relation, object_ref, effect, created_at, updated_at)
                VALUES
                    ('10000000-0000-0000-0000-000000000001', 'default', 'store-docs-default', 'user:admin', 'owner', 'document:roadmap', 'Allow', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),
                    ('10000000-0000-0000-0000-000000000002', 'default', 'store-docs-default', 'team:platform', 'member', 'user:anne', 'Allow', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),
                    ('10000000-0000-0000-0000-000000000003', 'default', 'store-docs-default', 'team:platform', 'member', 'user:bob', 'Allow', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days'),
                    ('10000000-0000-0000-0000-000000000004', 'default', 'store-docs-default', 'user:anne', 'editor', 'document:roadmap', 'Allow', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days'),
                    ('10000000-0000-0000-0000-000000000005', 'default', 'store-docs-default', 'user:carol', 'viewer', 'document:roadmap', 'Deny', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),
                    ('10000000-0000-0000-0000-000000000006', 'default', 'store-docs-default', 'user:bob', 'viewer', 'document:design-spec', 'Allow', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),
                    ('10000000-0000-0000-0000-000000000007', 'default', 'store-billing-default', 'user:admin', 'admin', 'account:acme', 'Allow', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days'),
                    ('10000000-0000-0000-0000-000000000008', 'default', 'store-billing-default', 'user:finance', 'analyst', 'account:acme', 'Allow', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),
                    ('10000000-0000-0000-0000-000000000009', 'default', 'store-support-default', 'user:agent1', 'assignee', 'ticket:INC-1001', 'Allow', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
                    ('10000000-0000-0000-0000-000000000010', 'default', 'store-support-default', 'user:lead', 'manager', 'queue:enterprise', 'Allow', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
                    ('10000000-0000-0000-0000-000000000011', 'tenant-dev', 'store-lab-tenant-dev', 'user:dev', 'maintainer', 'project:aegis-lab', 'Allow', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
                    ('10000000-0000-0000-0000-000000000012', 'tenant-dev', 'store-lab-tenant-dev', 'user:intern', 'viewer', 'project:aegis-lab', 'Allow', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day')
                ON CONFLICT (tenant_id, store_id, subject, relation, object_ref) DO UPDATE
                SET effect = EXCLUDED.effect,
                    updated_at = EXCLUDED.updated_at;

                INSERT INTO relationship_changes (id, tenant_id, store_id, subject, relation, object_ref, operation, created_at)
                VALUES
                    ('20000000-0000-0000-0000-000000000001', 'default', 'store-docs-default', 'user:admin', 'owner', 'document:roadmap', 'upsert', NOW() - INTERVAL '5 days'),
                    ('20000000-0000-0000-0000-000000000002', 'default', 'store-docs-default', 'user:anne', 'editor', 'document:roadmap', 'upsert', NOW() - INTERVAL '4 days'),
                    ('20000000-0000-0000-0000-000000000003', 'default', 'store-docs-default', 'user:carol', 'viewer', 'document:roadmap', 'upsert', NOW() - INTERVAL '3 days'),
                    ('20000000-0000-0000-0000-000000000004', 'default', 'store-billing-default', 'user:finance', 'analyst', 'account:acme', 'upsert', NOW() - INTERVAL '3 days'),
                    ('20000000-0000-0000-0000-000000000005', 'tenant-dev', 'store-lab-tenant-dev', 'user:dev', 'maintainer', 'project:aegis-lab', 'upsert', NOW() - INTERVAL '2 days')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO rbac_users (tenant_id, user_id, email, display_name, created_at, updated_at)
                VALUES
                    ('default', 'user:admin', 'admin@aegis.local', 'Aegis Admin', NOW() - INTERVAL '10 days', NOW() - INTERVAL '1 day'),
                    ('default', 'user:anne', 'anne@aegis.local', 'Anne Platform', NOW() - INTERVAL '9 days', NOW() - INTERVAL '2 days'),
                    ('default', 'user:bob', 'bob@aegis.local', 'Bob Builder', NOW() - INTERVAL '9 days', NOW() - INTERVAL '2 days'),
                    ('default', 'user:carol', 'carol@aegis.local', 'Carol Auditor', NOW() - INTERVAL '8 days', NOW() - INTERVAL '2 days'),
                    ('default', 'user:finance', 'finance@aegis.local', 'Finance Analyst', NOW() - INTERVAL '8 days', NOW() - INTERVAL '2 days'),
                    ('default', 'user:agent1', 'agent1@aegis.local', 'Support Agent', NOW() - INTERVAL '7 days', NOW() - INTERVAL '1 day'),
                    ('default', 'user:lead', 'lead@aegis.local', 'Support Lead', NOW() - INTERVAL '7 days', NOW() - INTERVAL '1 day'),
                    ('tenant-dev', 'user:dev', 'dev@aegis.local', 'Developer User', NOW() - INTERVAL '6 days', NOW() - INTERVAL '1 day'),
                    ('tenant-dev', 'user:intern', 'intern@aegis.local', 'Intern User', NOW() - INTERVAL '6 days', NOW() - INTERVAL '1 day')
                ON CONFLICT (tenant_id, user_id) DO UPDATE
                SET email = EXCLUDED.email,
                    display_name = EXCLUDED.display_name,
                    updated_at = EXCLUDED.updated_at;

                INSERT INTO rbac_roles (tenant_id, store_id, role_name, description, created_at, updated_at)
                VALUES
                    ('default', 'store-docs-default', 'docs_admin', 'Full administration over document authorization data.', NOW() - INTERVAL '9 days', NOW() - INTERVAL '1 day'),
                    ('default', 'store-docs-default', 'docs_editor', 'Can edit and view product documents.', NOW() - INTERVAL '9 days', NOW() - INTERVAL '1 day'),
                    ('default', 'store-docs-default', 'docs_viewer', 'Read-only document access.', NOW() - INTERVAL '9 days', NOW() - INTERVAL '1 day'),
                    ('default', 'store-billing-default', 'billing_admin', 'Manage billing accounts and permissions.', NOW() - INTERVAL '8 days', NOW() - INTERVAL '1 day'),
                    ('default', 'store-billing-default', 'billing_analyst', 'Analyze billing account data.', NOW() - INTERVAL '8 days', NOW() - INTERVAL '1 day'),
                    ('default', 'store-support-default', 'support_manager', 'Manage support queue policy.', NOW() - INTERVAL '7 days', NOW() - INTERVAL '1 day'),
                    ('default', 'store-support-default', 'support_agent', 'Work assigned tickets.', NOW() - INTERVAL '7 days', NOW() - INTERVAL '1 day'),
                    ('tenant-dev', 'store-lab-tenant-dev', 'project_maintainer', 'Maintain sandbox projects.', NOW() - INTERVAL '6 days', NOW() - INTERVAL '1 day'),
                    ('tenant-dev', 'store-lab-tenant-dev', 'project_viewer', 'View sandbox projects.', NOW() - INTERVAL '6 days', NOW() - INTERVAL '1 day')
                ON CONFLICT (tenant_id, store_id, role_name) DO UPDATE
                SET description = EXCLUDED.description,
                    updated_at = EXCLUDED.updated_at;

                INSERT INTO rbac_permissions (tenant_id, store_id, relation, object_ref, condition_name, created_at)
                VALUES
                    ('default', 'store-docs-default', 'owner', 'document:*', NULL, NOW() - INTERVAL '9 days'),
                    ('default', 'store-docs-default', 'editor', 'document:*', 'business_hours', NOW() - INTERVAL '9 days'),
                    ('default', 'store-docs-default', 'viewer', 'document:*', NULL, NOW() - INTERVAL '9 days'),
                    ('default', 'store-billing-default', 'admin', 'account:*', NULL, NOW() - INTERVAL '8 days'),
                    ('default', 'store-billing-default', 'analyst', 'account:*', 'region_apac', NOW() - INTERVAL '8 days'),
                    ('default', 'store-support-default', 'manager', 'queue:*', NULL, NOW() - INTERVAL '7 days'),
                    ('default', 'store-support-default', 'assignee', 'ticket:*', NULL, NOW() - INTERVAL '7 days'),
                    ('tenant-dev', 'store-lab-tenant-dev', 'maintainer', 'project:*', NULL, NOW() - INTERVAL '6 days'),
                    ('tenant-dev', 'store-lab-tenant-dev', 'viewer', 'project:*', NULL, NOW() - INTERVAL '6 days')
                ON CONFLICT (tenant_id, store_id, relation, object_ref) DO UPDATE
                SET condition_name = EXCLUDED.condition_name;

                INSERT INTO rbac_role_permissions (tenant_id, store_id, role_name, relation, object_ref, condition_name, created_at)
                VALUES
                    ('default', 'store-docs-default', 'docs_admin', 'owner', 'document:*', NULL, NOW() - INTERVAL '9 days'),
                    ('default', 'store-docs-default', 'docs_editor', 'editor', 'document:*', 'business_hours', NOW() - INTERVAL '9 days'),
                    ('default', 'store-docs-default', 'docs_viewer', 'viewer', 'document:*', NULL, NOW() - INTERVAL '9 days'),
                    ('default', 'store-billing-default', 'billing_admin', 'admin', 'account:*', NULL, NOW() - INTERVAL '8 days'),
                    ('default', 'store-billing-default', 'billing_analyst', 'analyst', 'account:*', 'region_apac', NOW() - INTERVAL '8 days'),
                    ('default', 'store-support-default', 'support_manager', 'manager', 'queue:*', NULL, NOW() - INTERVAL '7 days'),
                    ('default', 'store-support-default', 'support_agent', 'assignee', 'ticket:*', NULL, NOW() - INTERVAL '7 days'),
                    ('tenant-dev', 'store-lab-tenant-dev', 'project_maintainer', 'maintainer', 'project:*', NULL, NOW() - INTERVAL '6 days'),
                    ('tenant-dev', 'store-lab-tenant-dev', 'project_viewer', 'viewer', 'project:*', NULL, NOW() - INTERVAL '6 days')
                ON CONFLICT (tenant_id, store_id, role_name, relation, object_ref) DO UPDATE
                SET condition_name = EXCLUDED.condition_name;

                INSERT INTO rbac_user_roles (tenant_id, store_id, user_id, role_name, created_at)
                VALUES
                    ('default', 'store-docs-default', 'user:admin', 'docs_admin', NOW() - INTERVAL '8 days'),
                    ('default', 'store-docs-default', 'user:anne', 'docs_editor', NOW() - INTERVAL '8 days'),
                    ('default', 'store-docs-default', 'user:bob', 'docs_viewer', NOW() - INTERVAL '8 days'),
                    ('default', 'store-billing-default', 'user:admin', 'billing_admin', NOW() - INTERVAL '7 days'),
                    ('default', 'store-billing-default', 'user:finance', 'billing_analyst', NOW() - INTERVAL '7 days'),
                    ('default', 'store-support-default', 'user:lead', 'support_manager', NOW() - INTERVAL '6 days'),
                    ('default', 'store-support-default', 'user:agent1', 'support_agent', NOW() - INTERVAL '6 days'),
                    ('tenant-dev', 'store-lab-tenant-dev', 'user:dev', 'project_maintainer', NOW() - INTERVAL '5 days'),
                    ('tenant-dev', 'store-lab-tenant-dev', 'user:intern', 'project_viewer', NOW() - INTERVAL '5 days')
                ON CONFLICT (tenant_id, store_id, user_id, role_name) DO NOTHING;

                INSERT INTO audit_events (id, tenant_id, store_id, action, subject, relation, object_ref, decision, reason_code, created_at)
                VALUES
                    ('30000000-0000-0000-0000-000000000001', 'default', 'store-docs-default', 'check', 'user:admin', 'owner', 'document:roadmap', 'allow', 'ALLOW_REBAC_DIRECT', NOW() - INTERVAL '2 days'),
                    ('30000000-0000-0000-0000-000000000002', 'default', 'store-docs-default', 'check', 'user:carol', 'viewer', 'document:roadmap', 'deny', 'DENY_REBAC_DIRECT', NOW() - INTERVAL '36 hours'),
                    ('30000000-0000-0000-0000-000000000003', 'default', 'store-billing-default', 'check', 'user:finance', 'analyst', 'account:acme', 'allow', 'ALLOW_REBAC_DIRECT', NOW() - INTERVAL '30 hours'),
                    ('30000000-0000-0000-0000-000000000004', 'default', 'store-support-default', 'check', 'user:agent1', 'assignee', 'ticket:INC-1001', 'allow', 'ALLOW_REBAC_DIRECT', NOW() - INTERVAL '20 hours'),
                    ('30000000-0000-0000-0000-000000000005', 'tenant-dev', 'store-lab-tenant-dev', 'check', 'user:dev', 'maintainer', 'project:aegis-lab', 'allow', 'ALLOW_REBAC_DIRECT', NOW() - INTERVAL '18 hours')
                ON CONFLICT (id) DO UPDATE
                SET decision = EXCLUDED.decision,
                    reason_code = EXCLUDED.reason_code,
                    created_at = EXCLUDED.created_at;
                """;

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
