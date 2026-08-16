# ADR 0005: Align framework extensions with the .NET 8 baseline

- Status: Accepted
- Date: 2026-08-16
- Owners: Aegis maintainers

## Context

All projects target `net8.0`, and `global.json` pins SDK 8.0.416. Direct references previously mixed Microsoft.Extensions major versions 8, 9, and 10, making compatibility intent unclear and producing a conflict under central transitive pinning.

## Decision

Until a deliberate runtime upgrade, Microsoft.AspNetCore and Microsoft.Extensions packages use compatible 8.x releases. Versions live in `Directory.Packages.props`, lock files are committed, and CI restores in locked mode.

Runtime upgrades are dedicated changes that update SDK, target frameworks, central packages, lock files, CI, deployment images, documentation, and verification evidence together. Individual projects cannot select a newer framework major independently.

## Consequences

- Runtime and extension packages communicate one support lifecycle.
- Restore conflicts and transitive drift fail early.
- Security servicing still requires routine central-version and lock-file updates.
- APIs exclusive to newer majors require a platform upgrade or compatible design.

## Validation

`eng/verify-backend.ps1` performs locked restore, Release build, and all tests. The implementation log records SDK, warning count, and test totals for baseline changes.
