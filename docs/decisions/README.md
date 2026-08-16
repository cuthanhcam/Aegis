# Architecture decision records

Architecture decision records explain why Aegis has a particular boundary. They are append-only historical records: supersede an accepted decision with a new ADR instead of rewriting its outcome.

| ADR                                    | Status   | Decision                                                 |
| -------------------------------------- | -------- | -------------------------------------------------------- |
| [0001](0001-modular-monolith-first.md) | Accepted | Build a modular monolith before extracting services      |
| [0002](0002-core-boundary.md)          | Accepted | Keep Aegis Core focused on authorization semantics       |
| [0003](0003-deployment-profiles.md)    | Accepted | Support all-in-one first and preserve profile boundaries |
| [0004](0004-freeze-legacy-frontend.md) | Accepted | Freeze the current frontend as a parity reference        |

## ADR template

Each record contains context, decision, consequences, constraints, validation evidence, and a supersession link when applicable. Proposed ADRs may evolve; accepted ADRs change only to correct factual errors or add implementation evidence.
