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
| B3 | Durable data correctness | In progress | failure tests, migration report, restore drill, reconciliation report |
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

### Remaining B1 product-readiness work

- [x] Govern route versioning, OpenAPI baseline, contract diff, lifecycle fixtures, and generated-client proof.
- [x] Define the native v1 error-envelope decision and central stable-code registry.
- [x] Attach safe trace correlation and complete model-validation details to native errors.
- [ ] Complete endpoint-by-endpoint error/status documentation and negative contract coverage.
- [ ] Standardize pagination, filtering, sorting, page/batch limits, deadlines, and cancellation semantics.
- [ ] Add idempotency and optimistic-concurrency contracts to retryable mutations.
- [ ] Split broad controllers/application services around explicit use-case and transaction boundaries.
- [ ] Harden model lifecycle activation, concurrent reads, conflicts, and rollback consistency.
- [ ] Complete the B1 exit review and record the owner-deferred CI artifact-publication exception.

Request-semantics progress:

- [x] Centralize v1 page, batch, cursor-length, and filter-length limits.
- [x] Emit opaque versioned native cursors while accepting legacy numeric cursors during rolling upgrades.
- [x] Enforce relationship-change page bounds and native/compatibility batch caps.
- [x] Apply a startup-validated global request deadline and preserve cooperative cancellation.
- [x] Guard cancellation-token presence on every asynchronous controller action.
- [ ] Migrate remaining unpaged collection endpoints with explicit ordering and non-breaking response plans.
- [ ] Define endpoint-specific filters and sorting only where justified by consumer workflows.

Mutation-safety progress:

- [x] Add strong ETags and atomic revision checks to authorization-model update and delete.
- [x] Define stable HTTP 428/412 native error semantics and safe client recovery guidance.
- [x] Extend transaction-scoped preconditions to authorization-model publish and rollback.
- [ ] Add durable, tenant-scoped idempotency reservation and response replay for retryable creates/writes.

Idempotency progress:

- [x] Make authorization-model creation transactionally replayable across replicas.
- [x] Scope keys by tenant, actor, store, operation, and request fingerprint.
- [x] Prevent duplicate domain-event dispatch during response replay.
- [x] Complete the mutation risk register and select transaction-coupled candidates.
- [x] Make store creation transactionally replayable without a pre-existing resource scope.
- [ ] Extend transactional idempotency to other selected creates/writes after endpoint risk review.
- [ ] Couple durable outbox messages to idempotent resource transactions before external event delivery is production-supported.

Application-boundary progress:

- [x] Define the command use-case ownership rule in ADR 0011.
- [x] Extract store creation and idempotent replay orchestration into `CreateStoreUseCase`.
- [x] Make `StoresController.Create` depend directly on the command boundary.
- [x] Extract authorization-model DSL validation into an independent application component.
- [x] Extract model creation and idempotent replay into `CreateAuthorizationModelUseCase`.
- [x] Extract model update and delete with repository-owned revision predicates.
- [x] Extract model publish and rollback while retaining repository-owned store serialization.
- [x] Split user create/update/delete mutations after repository transaction review and remove their broad-service delegates.
- [x] Introduce a durable, versioned assertion repository with atomic replace/append and store purge semantics.
- [x] Extract assertion validation and write/replace into an explicit command boundary.
- [x] Extract assertion run against one captured definition snapshot and append completed history only after evaluation.
- [x] Extract audit-derived assertion generation with draft-only and atomic-append semantics.
- [x] Record the captured assertion definition revision in durable run history and expose it through the additive v1 contract.
- [x] Move assertion definition and run-history reads to explicit query use cases and remove the broad assertion application service.
- [x] Make PostgreSQL store deletion tenant-scoped and atomic across operational authorization state.
- [x] Add a read-only legacy violation inventory, guarded validation tool, JSON evidence, and reconciliation runbook for migration 016.
- [ ] Execute the reconciliation/validation runbook against each managed environment and retain approved reports.
- [x] Add and execute an isolated PostgreSQL logical backup/restore rehearsal with JSON evidence.
- [x] Inject a child-cascade failure and prove atomic store-delete rollback.
- [ ] Execute a staging-sized managed restore, full golden decisions, and measured RPO/RTO evidence.
- [x] Serialize migration execution, enforce immutable checksums, and bound lock/statement waits.
- [x] Terminate a visibly blocked migration connection, prove transaction/history rollback, and prove clean retry.
- [x] Add a one-shot migrator and read-only replica validation mode without changing deployment automation.
- [ ] Move managed migration authority out of ordinary application replicas after deployment design approval.
- [x] Remove authorization-model mutation delegates after production and test caller migration.
- [x] Remove temporary store-create delegates and nullable compatibility composition after caller audit.
- [x] Remove dormant model-command compatibility factories and registry-only mutation fallbacks.

Iteration 1 evidence: local locked restore, zero-warning Release build, 268 unit tests, 25 integration tests, a 53-path OpenAPI v1 export, and `develop` Actions run `31955813080` all passed for merge commit `c39e396`.

Iteration 2 evidence: the committed 53-path baseline and runtime candidate have identical SHA-256 hashes; the JSON diff report contains zero removed paths, operations, or schemas. Kiota 1.34.1 generated the TypeScript client and TypeScript 7.0.2 compiled it in strict mode with zero npm audit findings. Full backend verification passed 268 unit and 25 integration tests with zero build warnings or errors. Kiota's TypeScript target remains preview, so its version and proof dependencies are pinned and generated sources remain disposable artifacts.

The iteration 2 merge commit is `f356cd4`; `develop` Actions run `32041840093` passed. B1 remains `In progress` until lifecycle classification tests and the explicitly deferred pipeline publication gate are resolved.

Iteration 3 lifecycle evidence covers five disposable contracts: additive path and deprecated operation are accepted; removed path, operation, and schema are rejected with machine-readable reports. B1 remains `In progress` only because OpenAPI artifact publication through the unchanged pipeline is explicitly deferred by repository-owner direction.

The iteration 3 merge commit is `3f095da`; `develop` Actions run `32042633435` passed. All locally authorized B1 contract-governance gates are now evidenced.

Iteration 4 native-error evidence: additive OpenAPI diff, generated-client strict compilation, five contract lifecycle fixtures, 269 unit tests, 25 integration tests, and `develop` Actions run `32043384214` all passed for merge commit `b8f2375`.

Iteration 5 request-semantics local evidence: regenerated OpenAPI semantic diff and all five lifecycle fixtures passed; the generated TypeScript client passed strict compilation and npm audit; locked restore, zero-warning Release build, 276 unit tests, and 26 integration tests passed. `develop` merge and Actions evidence remain pending.

Iteration 5 merge evidence: feature commit `b587fa0` was merged locally into `develop` as `33de240`. Actions run `32044172154` failed at `Set up job` before checkout and before any repository command; it is retained as infrastructure evidence, not treated as a product-code failure. A traceable docs-only merge supplies the replacement verification run.

Iteration 5 Actions evidence: replacement `.NET CI` run `32044320783` passed for docs-evidence merge `653311c`, covering the unchanged request-semantics source tree. No workflow file was modified.

Iteration 6 model-concurrency local evidence: ETag/precondition endpoint coverage, additive OpenAPI semantic diff, five lifecycle fixtures, generated TypeScript strict compilation, npm audit, locked restore, zero-warning Release build, 276 unit tests, and 27 integration tests passed. `develop` merge and Actions evidence remain pending.

Iteration 6 merge evidence: feature commit `7109d94` was merged locally into `develop` as `b1c8243`; `.NET CI` run `32142650670` passed. The workflow remained unchanged.

Iteration 7 lifecycle-concurrency scope: serialize publish/rollback at the store boundary, validate the target revision inside the transaction, return the active model's new ETag, and retain durable idempotency as the remaining mutation-safety item.

The database invariant is defense in depth: migration 011 deterministically archives duplicate historical published rows, then enforces at most one published authorization model per store with a partial unique index.

Iteration 7 local evidence: lifecycle precondition coverage, additive OpenAPI semantic diff, five lifecycle fixtures, generated TypeScript strict compilation, npm audit, locked restore, zero-warning Release build, 276 unit tests, and 28 integration tests passed. `develop` merge and Actions evidence remain pending.

Iteration 7 merge evidence: feature commit `c7a5a8f` was merged locally into `develop` as `6a0474a`; `.NET CI` run `32143817767` passed. The workflow remained unchanged.

Iteration 8 idempotency scope: provide the first durable, transaction-coupled replay contract for authorization-model creation without claiming generic middleware-level exactly-once behavior.

Iteration 8 local evidence: same-key replay and conflicting-payload coverage, additive OpenAPI semantic diff, five lifecycle fixtures, generated TypeScript strict compilation, npm audit, locked restore, zero-warning Release build, 276 unit tests, and 29 integration tests passed. `develop` merge and Actions evidence remain pending.

Iteration 8 merge evidence: feature commit `ef647c5` was merged locally into `develop` as `24f42d8`; `.NET CI` run `33182265773` passed. The workflow remained unchanged.

Iteration 9 store-idempotency scope: complete the mutation risk register and add transaction-coupled replay to store creation; defer user/assertion candidates until their use-case transaction ownership is explicit.

Iteration 9 local evidence: store same-key replay and payload-conflict coverage, additive OpenAPI semantic diff, five lifecycle fixtures, generated TypeScript strict compilation, npm audit, locked restore, zero-warning Release build, 276 unit tests, and 30 integration tests passed. `develop` merge and Actions evidence remain pending.

Iteration 9 merge evidence: feature commit `3b4966d` was merged locally into `develop` as `d6ed257`; `.NET CI` run `33183532413` passed. The workflow remained unchanged.

Iteration 10 application-boundary scope: extract store creation into a command-focused use case while retaining behavior-compatible service delegates during incremental migration.

Iteration 10 local evidence: two command-boundary unit tests, existing store endpoint coverage, unchanged OpenAPI semantics, five lifecycle fixtures, generated TypeScript strict compilation, npm audit, locked restore, zero-warning Release build, 278 unit tests, and 30 integration tests passed.

Iteration 10 merge evidence: feature commit `844a5e5` was merged locally into `develop` as `d271558`; `.NET CI` run `33184947735` passed. The workflow remained unchanged.

Iteration 11 application-boundary scope: extract deterministic authorization-model DSL validation from the broad application service without changing model persistence, lifecycle behavior, or the public validation contract.

Iteration 11 local evidence: 14 targeted validator, compatibility-service, and dependency-injection tests passed. Locked restore, zero-warning Release build, 281 unit tests, and 30 integration tests passed. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 11 merge evidence: feature commit `2d62cff` was merged locally into `develop` as `31dd355`; `.NET CI` run `33185735354` passed. The workflow remained unchanged.

Iteration 12 application-boundary scope: extract authorization-model creation and transaction-coupled idempotent replay while retaining the broad service delegates for compatibility.

Iteration 12 local evidence: 13 targeted command, compatibility-service, and dependency-injection unit tests plus the model-create replay endpoint integration test passed. Locked restore, zero-warning Release build, 283 unit tests, and 30 integration tests passed. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 12 merge evidence: feature commit `fc53d80` was merged locally into `develop` as `b74a29d`; `.NET CI` run `33186355615` passed. The workflow remained unchanged.

Iteration 13 application-boundary scope: extract authorization-model update and delete commands while preserving strong ETag preconditions, not-found versus stale-revision classification, and post-success event dispatch.

Iteration 13 local evidence: 14 targeted command, compatibility-service, and dependency-injection unit tests plus the strong-ETag update endpoint integration test passed. Locked restore, zero-warning Release build, 286 unit tests, and 30 integration tests passed. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 13 merge evidence: feature commit `05c5976` was merged locally into `develop` as `1a7b5dc`; `.NET CI` run `33187045638` passed. The workflow remained unchanged.

Iteration 14 application-boundary scope: extract authorization-model publish and rollback commands while preserving store-scoped transaction serialization, target revision rechecks, the single-published invariant, and post-commit rollback audit.

Iteration 14 local evidence: 14 targeted lifecycle, compatibility-service, and dependency-injection unit tests plus two lifecycle endpoint integration tests passed. Locked restore, zero-warning Release build, 289 unit tests, and 30 integration tests passed. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 14 merge evidence: feature commit `ad0a7ff` was merged locally into `develop` as `16c4925`; `.NET CI` run `33187746559` passed. The workflow remained unchanged.

Iteration 15 application-boundary scope: audit callers and remove authorization-model create/update/delete/publish/rollback delegates from the broad application-service interface and implementation.

Iteration 15 local evidence: 12 targeted query-service, lifecycle-use-case, and dependency-injection tests passed; caller search confirms production model mutations depend only on explicit command use cases. Locked restore, zero-warning Release build, 287 unit tests, and 30 integration tests passed. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 15 merge evidence: feature commit `086a73f` was merged locally into `develop` as `d04d35b`; `.NET CI` run `33188518304` passed. The workflow remained unchanged.

Iteration 16 application-boundary scope: audit store-create callers, remove create overloads from the broad store service, and make `CreateStoreUseCase` repository/event dependencies strict.

Iteration 16 local evidence: 11 targeted store-use-case, remaining store-service, and dependency-injection tests passed; caller search confirms `StoresController` creates stores only through `CreateStoreUseCase`. Locked restore, zero-warning Release build, 287 unit tests, and 30 integration tests passed. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 16 merge evidence: feature commit `bc3988f` was merged locally into `develop` as `7f9617d`; `.NET CI` run `33189134472` passed. The workflow remained unchanged.

Iteration 17 application-boundary scope: remove dormant compatibility constructors and alternate registry mutation paths from authorization-model commands so repository transaction ownership is mandatory and visible.

Iteration 17 local evidence: 15 targeted authorization-model command, query-service, and dependency-injection tests passed; dead-code search finds no compatibility factory or nullable persistence/event/audit dependency in the model command directory. Locked restore, zero-warning Release build, 287 unit tests, and 30 integration tests passed. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 17 merge evidence: feature commit `b70eaa1` was merged locally into `develop` as `c6519df`; `.NET CI` run `33189763090` passed. The workflow remained unchanged.

Iteration 18 application-boundary scope: extract tenant-scoped user create/update/delete commands and make repository mutation results reflect one atomic persistence operation.

Iteration 18 transaction review: PostgreSQL create is a single insert-returning statement. Update now uses update-returning instead of a write followed by an unrelated read. Delete now removes assignments and the user inside an explicit transaction and reports success from the user row. Assertion mutations remain deferred because definitions are process-local state and no durable repository owns replace/append concurrency.

Iteration 18 local evidence: 7 targeted user-boundary and dependency-injection tests passed. Locked restore, zero-warning Release build, 290 unit tests, and 30 integration tests passed. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 18 merge evidence: feature commit `75ec6c0` was merged locally into `develop` as `6b18cb1`; `.NET CI` run `33191447242` passed. The workflow remained unchanged.

Iteration 19 persistence scope: replace process-local assertion definitions with a store/model-scoped repository and make replacement, audit append, capacity enforcement, and purge explicit persistence operations.

Iteration 19 local evidence: 15 targeted assertion/store tests and the assertion lifecycle endpoint integration test passed. Locked restore, zero-warning Release build, 293 unit tests, and 30 integration tests passed. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 19 merge evidence: feature commit `f759d8e` was merged locally into `develop` as `c5dd3d0`; `.NET CI` run `33192371166` passed. The workflow remained unchanged.

Iteration 20 application-boundary scope: extract assertion validation and replacement, route the controller directly to `WriteAssertionsUseCase`, and remove the broad-service write delegate.

Iteration 20 local evidence: 14 targeted write, assertion-service, and dependency-injection tests plus the assertion lifecycle endpoint integration test passed. Locked restore, zero-warning Release build, 296 unit tests, and 30 integration tests passed. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 20 merge evidence: feature commit `d3b7805` was merged locally into `develop` as `9e86376`; `.NET CI` run `33257467077` passed. The workflow remained unchanged.

Iteration 21 application-boundary scope: extract assertion execution, capture one repository snapshot per run, and remove the broad-service run delegate and permission-check dependency.

Iteration 21 local evidence: 13 targeted run, remaining assertion-service, and dependency-injection tests plus the assertion lifecycle endpoint integration test passed. Locked restore, zero-warning Release build, 298 unit tests, and 30 integration tests passed. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 21 merge evidence: feature commit `7f2dc2f` was merged locally into `develop` as `28da9a0`; `.NET CI` run `33257783985` passed. The workflow remained unchanged.

Iteration 22 application-boundary scope: extract audit-derived assertion generation, route the controller directly to the use case, and reduce the assertion service to read/history/purge responsibilities.

Iteration 22 local evidence: 13 targeted generation, remaining assertion-service, and dependency-injection tests plus the assertion lifecycle endpoint integration test passed. Locked restore, zero-warning Release build, 300 unit tests, and 30 integration tests passed. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 22 merge evidence: feature commit `c873c66` was merged locally into `develop` as `6bcf734`; `.NET CI` run `33258613352` passed. The workflow remained unchanged.

Iteration 23 contract scope: persist the captured assertion-definition revision with every completed run and expose it as additive `definition_revision` in the v1 run-history contract. Revision zero explicitly represents legacy history or a run captured before any definition set was written.

Iteration 23 local evidence: 9 targeted unit tests and 5 assertion lifecycle integration tests passed. The candidate retained all 53 paths and reported zero removed paths, operations, or schemas with `breaking: false`; the reviewed additive candidate was promoted to the committed OpenAPI baseline. Locked restore, zero-warning Release build, 300 unit tests, and 30 integration tests passed. The promoted runtime OpenAPI is semantically identical to its baseline; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 23 merge evidence: feature commit `2339325` was merged locally into `develop` as `22ded7e`; `.NET CI` run `33259188992` passed. The workflow remained unchanged.

Iteration 24 application-boundary scope: replace the remaining broad assertion read/history surface with explicit query use cases and isolate store cleanup behind a purge coordinator whose non-atomic semantics are documented.

Iteration 24 local evidence: 19 targeted query, store-deletion, and composition unit tests plus 5 assertion lifecycle integration tests passed. Caller audit finds no production dependency on `IAssertionAppService` or `AssertionAppService`. Locked restore, zero-warning Release build, 303 unit tests, and 30 integration tests passed. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 24 merge evidence: feature commit `830d10f` was merged locally into `develop` as `19aa1af`; `.NET CI` run `33259603421` passed. The workflow remained unchanged.

Iteration 25 durable-correctness scope: move production store deletion into one PostgreSQL-owned cascade transaction, preserve audit evidence, and enforce composite tenant/store ownership for new operational rows.

Iteration 25 local evidence: 9 focused store-boundary/composition unit tests pass and a PostgreSQL 16 container test proves cross-tenant no-op, atomic operational cascade, and audit retention. Locked restore, zero-warning Release build, 303 unit tests, and 31 integration tests passed. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 25 merge evidence: feature commit `23f87a2` was merged locally into `develop` as `05d9176`; `.NET CI` run `33260311533` passed. The workflow remained unchanged.

Iteration 26 durable-correctness scope: add an operator-safe inventory and validation workflow for the six staged tenant/store foreign keys introduced by migration 016.

Iteration 26 local evidence: Release build includes the new database-admin tool with zero warnings/errors. PostgreSQL 16 container coverage injects a legacy orphan, proves audit sampling and validation refusal, reconciles it, then validates all six constraints transactionally. Missing secret configuration returns the documented usage exit code without exposing credentials. Locked restore, 303 unit tests, and 31 integration tests passed. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 26 merge evidence: feature commit `253cb2f` was merged locally into `develop` as `93e8582`; `.NET CI` run `33260956484` passed. The workflow remained unchanged.

Iteration 27 durable-correctness scope: make PostgreSQL logical backup/restore compatibility repeatable and prove store-delete rollback under an injected cascade failure.

Iteration 27 local evidence: three isolated PostgreSQL 16 restore rehearsals passed. The final run restored the deterministic operational/audit fixture, validated exact counts and authorization tuple, produced a clean/validated reconciliation report, recorded a dump hash, and completed in 13.827 seconds before removing the dump and containers. Focused container failure injection proves a raised child delete rolls store, relationship, and assertion state back. Locked restore, zero-warning Release build, 303 unit tests, and 31 integration tests passed. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 27 merge evidence: feature commit `1bdd4a4` was merged locally into `develop` as `951da3d`; `.NET CI` run `33261885697` passed. The workflow remained unchanged.

Iteration 28 durable-correctness scope: harden embedded PostgreSQL migration execution against concurrent startup, immutable-history drift, unbounded waits, and interrupted statements.

Iteration 28 local evidence: PostgreSQL 16 container coverage runs four migration callers concurrently and produces exactly 16 unique checksummed history rows, proves a held advisory lock yields the configured timeout, explicitly releases the pooled-session lock, bootstraps a legacy null checksum, and proves checksum drift fails closed. Locked restore, zero-warning Release build, 303 unit tests, and 32 integration tests passed. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 28 merge evidence: feature commit `e3cf1e7` was merged locally into `develop` as `d80ff0b`; `.NET CI` run `33262928783` passed. The workflow remained unchanged.

Iteration 29 durable-correctness scope: prove migration recovery under physical connection termination rather than relying only on transaction design. PostgreSQL 16 coverage makes migration 016 pending, blocks its first DDL statement, discovers the exact active backend through `pg_stat_activity`, terminates it, verifies that the transaction left no success marker, and proves the subsequent retry records the migration exactly once. Managed-environment interruption rehearsal and separation of DDL authority remain explicit deployment gates.

Iteration 29 local evidence: the focused PostgreSQL 16 interruption test passed. Locked restore, zero-warning Release build, 303 unit tests, and 32 integration tests passed. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

Iteration 29 merge evidence: feature commit `9aa0ed0` was merged locally into `develop` as `01afe97`; `.NET CI` run `33263431325` passed. The workflow remained unchanged.

Iteration 30 durable-correctness scope: establish a deployable code boundary between schema mutation and ordinary API startup. `Aegis.Migrator` is a one-shot executable and PowerShell operator entry point using the hardened runner; API startup supports backward-compatible `Apply` and read-only `Validate` modes. Validation fails closed for absent history, pending/unknown migrations, missing checksums, and drift, and cannot run development seeding. Managed identity/grant separation and deployment ordering remain unclaimed environment gates.

Iteration 30 local evidence: missing migrator credentials return exit code 64 without exposing a connection string. Focused PostgreSQL 16 coverage passes for complete-schema validation and fail-closed pending, null-checksum, drift, lock-timeout, and connection-interruption paths. Locked restore, zero-warning Release build, 303 unit tests, and 32 integration tests passed. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed.

B0 is `Verified`: local Windows verification, clean Linux-container reproduction, and `develop` Actions run `31955303976` passed. Remaining improvements identified by the inventory belong to their planned B1–B4 phases.
