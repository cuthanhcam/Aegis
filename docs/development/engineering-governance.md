---
title: Engineering governance and delivery workflow
description: How Aegis records decisions, protects develop, verifies changes, and prevents unfinished work from disappearing.
category: guides
audience: [backend-engineer, frontend-engineer, platform-engineer]
status: published
last_updated: 2026-08-16
---

# Engineering governance and delivery workflow

Aegis treats documentation and verification as implementation. This workflow preserves what was decided, what changed, what evidence exists, and what remains.

## Branch policy

`develop` is the integration baseline and should not receive ordinary implementation commits directly:

```powershell
git switch develop
git pull --ff-only origin develop
git switch -c <type>/<short-description>
```

Use focused prefixes such as `feat/`, `fix/`, `chore/`, `docs/`, or `refactor/`. Keep unrelated user changes out of the branch commit. Merge only after gates pass and the branch can be reverted without unrelated rollback.

## Sources of truth

| Question                          | Source                                     |
| --------------------------------- | ------------------------------------------ |
| Why does this architecture exist? | Accepted ADR under `docs/decisions`        |
| What is durable system behavior?  | Article under `docs` plus executable tests |
| What phase is active?             | `temp/execution-tracker.md`                |
| What happened in an iteration?    | `temp/implementation-log.md`               |
| What is required for production?  | Workstream plans and release checklist     |

Planning documents are committed intentionally despite the general `temp/` ignore rule. Only named planning/tracking files belong in version control; generated artifacts and copied repositories do not.

## Change protocol

1. Record or reference the governing ADR before changing a boundary.
2. Mark the tracker phase `In progress` and name intended evidence.
3. Implement the smallest coherent slice.
4. Update contracts, tests, operational implications, and articles together.
5. Run narrow checks, then the workstream verification command.
6. Append a log entry with branch, result, evidence, and follow-up.
7. Mark `Verified` only when every exit criterion has evidence.

## Backend verification

Package versions live in `Directory.Packages.props`; project files do not choose independent versions. Lock files make restore drift visible. Run:

```powershell
./eng/verify-backend.ps1
```

This performs locked restore, Release build with warnings as errors, and all solution tests. Architecture tests enforce production project references. Integration tests may require Docker for PostgreSQL and Redis test containers.

The supported .NET 8 SDK currently uses `Aegis.sln` for CLI verification. `Aegis.slnx` remains repository metadata until the pinned SDK is deliberately upgraded and CI proves direct `.slnx` support.

## Frontend lifecycle

The current dashboard is legacy-frozen under ADR 0004. Its build commands preserve reference usability but are not a product-ready quality gate. A future console begins in a separate application boundary after backend contracts stabilize.

## Evidence and exceptions

Evidence is reproducible output: test counts, contract diffs, load configuration/results, threat-model closure, restore duration, accessibility reports, canary results, or incident drills. Documentation or scaffolding alone cannot complete a phase.

Security invariants—determinism, fail-closed behavior, isolation, auditable mutation, and authorized explain output—cannot be bypassed. Other temporary exceptions record owner, reason, expiry, risk, and removal issue.
