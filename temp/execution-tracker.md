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
- [ ] Extract model publish and rollback lifecycle commands.
- [ ] Split user and assertion mutations after repository transaction review.
- [ ] Remove temporary create delegates from broad application-service interfaces after caller migration.

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

Iteration 13 local evidence: 14 targeted command, compatibility-service, and dependency-injection unit tests plus the strong-ETag update endpoint integration test passed. Locked restore, zero-warning Release build, 286 unit tests, and 30 integration tests passed. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit passed. `develop` merge and Actions evidence remain pending.

B0 is `Verified`: local Windows verification, clean Linux-container reproduction, and `develop` Actions run `31955303976` passed. Remaining improvements identified by the inventory belong to their planned B1–B4 phases.
