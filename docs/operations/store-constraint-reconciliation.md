# Store constraint reconciliation runbook

Use this runbook after migration 016 has deployed and before marking its six tenant/store foreign keys as validated. The workflow is deliberately read-only until an operator supplies `-Validate`; it never repairs or deletes rows.

## Safety model

The database connection string is read only from `ConnectionStrings__Aegis`. Do not pass credentials as command-line arguments or commit them to configuration. Run the audit with a database role that can read the target tables and PostgreSQL constraint catalog. Validation additionally requires permission to alter those tables.

Always start against a restored non-production copy. Production validation should occur only after the report is clean, reconciliation decisions are approved, a current backup is verified, and the change window owner is identified.

## Generate the audit report

PowerShell:

```powershell
$env:ConnectionStrings__Aegis = '<connection string from the approved secret source>'
./eng/reconcile-store-constraints.ps1
```

The default report is written to `artifacts/database/store-constraint-reconciliation.json`. A clean audit exits with code `0`. Detected violations produce the report and exit with code `2`, which allows automation to stop without treating the database as unreachable. Invalid usage or missing configuration exits with code `64`; connection and PostgreSQL failures remain errors.

Each table entry includes:

- the table and constraint names;
- current PostgreSQL `convalidated` state;
- the complete violation count;
- at most 20 tenant/store samples for investigation.

The sample is diagnostic, not a complete repair manifest. Use the count and a separately approved, access-controlled export when every affected row must be reviewed.

## Reconcile violations

For every orphan or tenant mismatch, determine the authoritative outcome with the data owner. Typical outcomes are reassignment to an existing store, restoration of a missing parent from backup, archival, or deletion under an approved retention policy. Never infer the correct tenant from identifiers alone.

Record the ticket/change reference, table, affected row count, chosen action, reviewer, execution timestamp, and before/after report hashes. Audit and relationship-change data may have security or legal retention implications; involve the appropriate owner before mutation.

After remediation, rerun the read-only audit until `totalViolations` is zero.

## Validate constraints

Only after a clean audit:

```powershell
./eng/reconcile-store-constraints.ps1 -Validate
```

The tool audits again in the same execution. If any violation exists, it performs no `ALTER TABLE` and exits `2`. Otherwise it validates all six constraints in one PostgreSQL transaction, rereads their catalog state, and writes a report with `validationCompleted: true` and every table entry marked `validated: true`.

PostgreSQL itself remains the final concurrency guard: if an invalid legacy row appears between audit and validation, validation fails and the transaction rolls back.

## Evidence and rollback

Attach the clean pre-validation report and successful validation report to the B3 change record. Store report hashes alongside the reports; reports may contain tenant/store identifiers and must follow operational-data access controls.

Constraint validation changes catalog state but does not modify application rows. If validation fails, investigate the PostgreSQL error and rerun the read-only audit. Do not drop the enforcing `NOT VALID` constraints as a routine rollback: they already protect new writes. Dropping them requires a separate incident decision because it reopens cross-tenant integrity risk.
