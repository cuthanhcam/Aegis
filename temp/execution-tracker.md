# Aegis execution tracker

Use this file as a release ledger, not as a substitute for issue tracking. Link each phase item to its issue/epic and attach evidence before changing its status.

## Status vocabulary

`Not started` → `In progress` → `In review` → `Verified` or `Blocked`. “Verified” requires the phase exit evidence, not only merged code.

## Backend

| Phase | Outcome | Status | Required evidence |
| --- | --- | --- | --- |
| B0 | Baseline and guardrails | Verified | ADRs, inventory, architecture tests, golden corpus, CI report |
| B1 | Governed contracts | In progress | versioned OpenAPI, diff report, generated client, lifecycle tests |
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

## Active iteration: B0 foundation guardrails

- [x] Accept modular-monolith-first architecture decision.
- [x] Define the authorization-focused Core boundary.
- [x] Define all-in-one and future deployment profiles.
- [x] Freeze the legacy frontend without deleting parity knowledge.
- [x] Centralize NuGet version declarations.
- [x] Add an executable production-project dependency policy.
- [x] Add the backend verification command and locked-restore CI policy.
- [x] Generate and commit package lock files.
- [x] Pass Release build and complete solution tests.
- [x] Inventory endpoints, configuration, cache keys, migrations, background services, and metrics.
- [x] Establish the golden decision corpus.
- [x] Resolve the .NET/Microsoft.Extensions baseline through ADR 0005 and an all-8.x framework package policy.
- [x] Confirm the `develop` CI run passes after the local feature-branch merge is pushed.

## Active iteration: B1 contract governance

- [x] Accept the native API versioning, compatibility, and error-code policy in ADR 0006.
- [x] Add executable guards for versioned controller routes and explicit HTTP methods.
- [x] Add a repeatable OpenAPI v1 export command sourced from the executable application graph.
- [x] Commit a reviewed OpenAPI baseline and produce a machine-readable breaking-change diff.
- [x] Generate and compile a TypeScript client without beginning the console rewrite.
- [x] Add lifecycle tests for additive, deprecated, and breaking contract changes.
- [ ] Publish the OpenAPI artifact from CI after repository-owner approval to modify the pipeline.

B1 remains `In progress`. This slice establishes policy and the export mechanism; it does not change runtime authorization behavior or the frozen frontend.

Iteration 1 evidence: local locked restore, zero-warning Release build, 268 unit tests, 25 integration tests, a 53-path OpenAPI v1 export, and `develop` Actions run `31955813080` all passed for merge commit `c39e396`.

Iteration 2 evidence: the committed 53-path baseline and runtime candidate have identical SHA-256 hashes; the JSON diff report contains zero removed paths, operations, or schemas. Kiota 1.34.1 generated the TypeScript client and TypeScript 7.0.2 compiled it in strict mode with zero npm audit findings. Full backend verification passed 268 unit and 25 integration tests with zero build warnings or errors. Kiota's TypeScript target remains preview, so its version and proof dependencies are pinned and generated sources remain disposable artifacts.

The iteration 2 merge commit is `f356cd4`; `develop` Actions run `32041840093` passed. B1 remains `In progress` until lifecycle classification tests and the explicitly deferred pipeline publication gate are resolved.

Iteration 3 lifecycle evidence covers five disposable contracts: additive path and deprecated operation are accepted; removed path, operation, and schema are rejected with machine-readable reports. B1 remains `In progress` only because OpenAPI artifact publication through the unchanged pipeline is explicitly deferred by repository-owner direction.

B0 is `Verified`: local Windows verification, clean Linux-container reproduction, and `develop` Actions run `31955303976` passed. Remaining improvements identified by the inventory belong to their planned B1–B4 phases.
