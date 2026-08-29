# PostgreSQL backup and restore drill

Backups are credible only when they can be restored and the restored authorization state is verified. This runbook defines the repository rehearsal and the additional evidence required before claiming recovery readiness for a managed environment.

## Repository rehearsal

Run the isolated drill from the repository root:

```powershell
./eng/test-postgres-restore.ps1
```

The script requires a running Docker engine and creates two uniquely named `postgres:16-alpine` containers. It does not connect to an existing database. The source container receives every committed migration plus a deterministic fixture containing a store, published model, allow relationship, assertion set, and audit event.

The drill then:

1. creates a custom-format logical backup with `pg_dump`;
2. hashes the dump with SHA-256;
3. restores it into a separate empty PostgreSQL container with `pg_restore --exit-on-error`;
4. verifies the expected row counts and exact authorization fixture;
5. runs store-constraint reconciliation and validation against the restored database;
6. writes JSON evidence and removes the temporary dump and containers.

The default evidence file is `artifacts/database/postgres-restore-drill.json`; the linked reconciliation report is written beside it. These artifacts are ignored by Git because operational identifiers and environment evidence should be retained in the approved evidence system, not committed casually.

## What the local drill proves

The repository rehearsal proves that the current migration set, PostgreSQL major version, logical dump format, restore command, core store-owned rows, audit retention, and migration-016 validation are mutually compatible. It records wall-clock duration but does not establish a production RTO because the dataset is intentionally small and local.

The exact restored tuple is a structural authorization fixture. It does not replace the full golden decision corpus through a running restored Aegis API; that remains required for a staging-sized managed-environment drill.

## Failure injection

Container integration coverage installs a temporary PostgreSQL trigger that raises during relationship cascade deletion. The attempted store delete must throw, after which the store, relationship, and assertion state are queried and proven intact. Removing the trigger allows the same delete to succeed. This is the transaction rollback evidence for the known partial-cleanup failure mode.

## Managed-environment drill

Before executing against managed data, record:

- approved source backup or snapshot and its retention classification;
- isolated target project/account, network boundary, encryption, and access owner;
- declared RPO and RTO targets;
- expected dataset size and backup timestamp;
- PostgreSQL server/extension compatibility;
- who may access restored tenant data and when it must be destroyed.

Restore only into an isolated target with production traffic disabled. Verify schema migration history, table counts by tenant/store, reconciliation reports, audit/outbox continuity, and the complete golden decision corpus through the application. Measure backup age for RPO and elapsed restore plus verification time for RTO.

Do not print connection strings, upload dumps to repository artifacts, or leave restored customer data after the approved window. Evidence should contain hashes, counts, timings, versions, command/tool revisions, approvals, and redacted failure details—not secrets or unrestricted policy data.

## Failure response

If dump, restore, count verification, reconciliation, or validation fails, the drill fails. Preserve logs and hashes, keep the source backup immutable, and investigate the first failing stage. Do not repair the only backup in place. Create a new isolated attempt after the recovery plan is reviewed.

The local script always attempts to remove its exact GUID-named containers and temporary dump in `finally`. If the host terminates abruptly, list containers whose names start with `aegis-restore-`, confirm their creation context, and remove only those drill resources.
