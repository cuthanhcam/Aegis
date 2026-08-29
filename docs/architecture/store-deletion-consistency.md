# Store deletion consistency

Store deletion is a tenant-scoped lifecycle operation, not a collection of unrelated cleanup calls. A successful response must mean that the store and every piece of operational authorization state owned by it disappeared together. A failed or unauthorized request must leave that state usable and unchanged.

## Consistency boundary

PostgreSQL is the transaction owner for durable store deletion. `PostgresStoreDeletionRepository` issues one predicate-protected statement:

```sql
DELETE FROM stores
WHERE tenant_id = @tenant_id
  AND id = @store_id;
```

Migration 016 connects store-owned tables to the composite `(tenant_id, id)` store identity with `ON DELETE CASCADE`. PostgreSQL executes the parent delete and every cascade in one transaction. There is no application-visible interval in which assertions are gone while relationships or the store still exist.

The composite foreign keys also reject new child rows whose tenant and store do not belong together. Checking only `store_id` would cascade correctly but would not protect the tenant-isolation invariant.

## Deleted and retained data

The atomic cascade removes:

- authorization models and their assertion sets;
- assertion run history;
- relationships and relationship change-feed rows;
- store-scoped RBAC roles, permissions, role-permission assignments, and user-role assignments;
- store-scoped idempotency records.

Tenant user profiles remain because they are tenant-owned, not store-owned. Audit events also remain intentionally. They are historical security evidence and keep the former `store_id` as an immutable correlation value even after the live store disappears. A future retention policy may archive or expire those events, but store deletion must not silently erase them.

## Tenant and failure semantics

The delete predicate includes both tenant and store identifiers. A request carrying another tenant returns `false` and changes no child data. If PostgreSQL cannot complete any cascade, the statement fails and its transaction rolls back; callers must not convert that exception into a successful deletion response.

The in-memory provider implements the same success, not-found, tenant-isolation, and cleanup outcomes behind `IStoreDeletionRepository`. It serializes deletions, but it is an ephemeral development provider and does not claim crash durability or rollback after an injected mid-operation failure. Production durability evidence therefore comes from the PostgreSQL container test.

## Migration rollout

Migration 016 adds new foreign keys as `NOT VALID`. PostgreSQL still enforces them for new inserts and updates, while deployment is not blocked by an unknown legacy orphan created before the constraints existed. This is a deliberate two-stage rollout:

1. deploy the enforcing constraints and stop new tenant/store mismatches;
2. inventory legacy violations and reconcile each row under an approved data policy;
3. run `VALIDATE CONSTRAINT` for every store foreign key;
4. record the validation report as B3 migration evidence.

The executable workflow and exit-code contract are defined in the [Store Constraint Reconciliation Runbook](../operations/store-constraint-reconciliation.md).

Do not delete legacy rows automatically during migration. Relationships, assignments, and change history may be security-relevant data; reconciliation needs an explicit owner and audit trail.

## Verification strategy

The container integration test starts PostgreSQL 16 and Redis, runs all migrations, seeds each major store-owned category, attempts a cross-tenant delete, and then performs the valid delete. Direct database queries prove that the parent store and operational children are absent while the audit event remains.

Injected transaction failure coverage now proves that a failed child cascade rolls the parent and previously visited children back. The repository backup/restore drill proves logical backup compatibility on a deterministic local fixture. Managed-environment constraint reports, staging-sized restore/RPO/RTO evidence, and broader reconciliation remain required before the durable-data phase can exit.
