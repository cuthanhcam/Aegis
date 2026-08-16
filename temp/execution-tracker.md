# Aegis execution tracker

Use this file as a release ledger, not as a substitute for issue tracking. Link each phase item to its issue/epic and attach evidence before changing its status.

## Status vocabulary

`Not started` → `In progress` → `In review` → `Verified` or `Blocked`. “Verified” requires the phase exit evidence, not only merged code.

## Backend

| Phase | Outcome | Status | Required evidence |
| --- | --- | --- | --- |
| B0 | Baseline and guardrails | Not started | ADRs, inventory, architecture tests, golden corpus, CI report |
| B1 | Governed contracts | Not started | versioned OpenAPI, diff report, generated client, lifecycle tests |
| B2 | Identity and isolation | Not started | threat model, isolation matrix, rotation/revocation drill, SBOM |
| B3 | Durable data correctness | Not started | failure tests, migration report, restore drill, reconciliation report |
| B4 | SLO-backed operations | Not started | dashboards, load report, game-day record, runbooks |
| B5 | Releasable production system | Not started | signed artifact, promotion/canary/rollback evidence, readiness approval |
| B6 | Scale and governance | Not started | customer-backed capability RFCs and their individual gates |

## Frontend

| Phase | Outcome | Status | Required evidence |
| --- | --- | --- | --- |
| F0 | Discovery and rewrite fence | Not started | parity ledger, screenshots, browser/a11y/performance policy |
| F1 | Shell and design system | Not started | component tests, visual baselines, accessibility report |
| F2 | Read-only spine | Not started | route e2e tests, contract fixtures, performance results |
| F3 | Authoring workbench | Not started | mutation/recovery/conflict e2e tests and audit evidence |
| F4 | Decision workbench | Not started | golden trace parity, large-graph performance, text alternatives |
| F5 | Administration and cutover | Not started | signed parity ledger, pilot report, rollback window completion |
| F6 | Product leverage | Not started | approved RFC and measurable user outcome per capability |

## Cross-workstream release gates

- [ ] Supported runtime/browser matrix is published.
- [ ] OpenAPI is generated from the release candidate and produces a clean frontend client.
- [ ] Cross-tenant negative tests pass.
- [ ] Golden decision corpus passes in backend and console rendering.
- [ ] Model publish, relationship mutation, check/explain, and audit end-to-end journeys pass.
- [ ] Security, dependency, license, secret, container, and artifact provenance checks pass.
- [ ] SLO/load evidence and frontend performance budgets pass.
- [ ] Backup restore, migration, canary, and rollback rehearsals pass.
- [ ] Runbooks, operator docs, integration docs, and release notes are current.
- [ ] Named engineering, security, product, and operations owners approve production readiness.

## First implementation iteration

1. Complete the route/endpoint/configuration/cache inventory.
2. Decide runtime/package baseline and API versioning policy.
3. Create the authorization golden corpus and cross-tenant matrix.
4. Generate OpenAPI in CI and prototype the generated TypeScript client.
5. Scaffold `apps/console` and its quality command without importing legacy UI.
6. Build the token foundation and application shell.
7. Deliver one thin read-only store/model slice end to end.
8. Review measurements and refine phase estimates before expanding parallel work.
