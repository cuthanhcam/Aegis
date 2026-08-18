# Architecture decision records

Architecture decision records explain why Aegis has a particular boundary. They are append-only historical records: supersede an accepted decision with a new ADR instead of rewriting its outcome.

| ADR                                     | Status   | Decision                                                 |
| --------------------------------------- | -------- | -------------------------------------------------------- |
| [0001](0001-modular-monolith-first.md)  | Accepted | Build a modular monolith before extracting services      |
| [0002](0002-core-boundary.md)           | Accepted | Keep Aegis Core focused on authorization semantics       |
| [0003](0003-deployment-profiles.md)     | Accepted | Support all-in-one first and preserve profile boundaries |
| [0004](0004-freeze-legacy-frontend.md)  | Accepted | Freeze the current frontend as a parity reference        |
| [0005](0005-dotnet-package-baseline.md) | Accepted | Align framework extensions with the .NET 8 baseline      |
| [0006](0006-govern-public-api-contracts.md) | Accepted | Govern versioning and compatibility of public API contracts |
| [0007](0007-native-error-envelope.md) | Accepted | Preserve and enrich the native v1 error envelope |
| [0008](0008-govern-request-semantics.md) | Accepted | Govern request limits, cursors, deadlines, and cancellation |
| [0009](0009-authorization-model-optimistic-concurrency.md) | Accepted | Protect authorization-model edits with strong entity tags |

## ADR template

Each record contains context, decision, consequences, constraints, validation evidence, and a supersession link when applicable. Proposed ADRs may evolve; accepted ADRs change only to correct factual errors or add implementation evidence.
