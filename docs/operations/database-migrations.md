# Database migration execution

Aegis migrations are forward-only, embedded SQL resources. Applied files are immutable release inputs: correct a defect with a new migration instead of editing history that may already exist in another environment.

## Execution guarantees

At PostgreSQL startup initialization, the runner:

1. opens one dedicated connection;
2. acquires the Aegis session advisory lock;
3. creates or upgrades `schema_migrations`;
4. compares applied history with embedded migration names and SHA-256 checksums;
5. applies each pending migration and its history row in one transaction;
6. explicitly releases the advisory lock in `finally`.

Concurrent Aegis instances may start together, but only one migrates at a time. Later instances reread history after acquiring the lock and do not replay completed work.

Checksums are computed after normalizing line endings to LF, preventing Windows/Linux checkout differences from looking like SQL changes. A checksum mismatch or an applied migration missing from the build fails startup. Do not bypass this guard by rewriting history.

## Upgrading pre-checksum history

Older databases have migration names and timestamps but no checksum column. The runner adds the nullable column while holding the migration lock and fills each null checksum from the same-named embedded resource. Unknown names still fail. After bootstrap, every subsequent startup enforces the stored value.

Capture a backup and migration-history export before first deploying checksum enforcement to a managed environment. Review that every applied name exists in the release artifact.

## Deadlines

Default configuration:

```json
{
  "Database": {
    "Migrations": {
      "LockTimeoutSeconds": 30,
      "StatementTimeoutSeconds": 120
    }
  }
}
```

The lock timeout bounds how long an instance waits behind another migrator. The statement timeout is applied through the database command timeout for history operations and migration SQL. Both values must be positive. Size them from rehearsed migration duration, not ordinary request latency.

Timeout, cancellation, SQL failure, or connection interruption propagates as startup failure. A migration transaction that did not commit has no history row and PostgreSQL rolls back its changes. On retry, the runner starts from the last committed migration.

This rollback guarantee has executable PostgreSQL 16 coverage. The test makes migration 016 pending, holds an exclusive table lock until its transaction is visibly blocked, terminates that exact migration backend through `pg_stat_activity`, and verifies that no migration-016 history row was committed. Releasing the blocker and rerunning then applies the migration exactly once. This is database-level failure evidence; it does not replace a managed-environment rehearsal with the production connection path and deployment identity.

## Operational response

For a lock timeout, first identify the session holding the advisory lock and determine whether it is actively migrating. Do not terminate it solely because another instance is waiting. Session locks must be released explicitly because pooled connection disposal may retain the physical session; the runner does this in `finally`, and PostgreSQL releases it if the physical session ends.

For checksum drift, compare the deployed artifact, repository migration, stored checksum, release provenance, and prior database evidence. Restore the intended immutable artifact or add a corrective migration. Updating `schema_migrations` to silence the error requires a separately approved incident procedure and destroys provenance.

For SQL failure, preserve the database error, migration name, artifact revision, and pre-deployment backup. Verify that the failing migration has no history row and inspect transaction state on an isolated restore before retrying.

## Remaining production gate

Application startup still owns migration authority. Production deployment should eventually run the same runner as a separately authorized migration job before serving traffic, leaving ordinary replicas with schema-readiness checks but no DDL permission. That cutover requires deployment and rollback design and is not claimed by the current change.
