# Aegis implementation log

This append-only log records completed iterations and their evidence. Plans describe intent; this file records what actually happened.

## 2026-08-16 — Product-readiness planning baseline

- Branch: `develop`
- Commit: `87969ad`
- Result: established phased plans, assessment, tracker, article schema, and foundational architecture/security/operations articles.
- Evidence: 13 documentation files committed and pushed; internal links and staged whitespace checks passed.
- Follow-up: begin B0 on a protected feature branch.

## 2026-08-16 — Backend foundation B0, iteration 1

- Branch: `chore/backend-foundation-b0`
- Status: Verified iteration; B0 remains In progress
- Intended result: encode architecture and frontend-lifecycle decisions; centralize package governance; add dependency tests and a reproducible backend verification command.
- Evidence: initial restore proved SDK 8.0.416 cannot consume `Aegis.slnx`; verification and CI use the supported `Aegis.sln` entry point. Central transitive pinning exposed a caching-abstractions 9/10 conflict, so the baseline pins the already-required 10.0.8 version. Locked restore succeeded for nine projects; Release build completed with zero warnings and zero errors; 265 unit tests and 23 integration tests passed, including the new dependency-policy test.
- Follow-up: inventory runtime surfaces, establish the golden decision corpus, and decide the supported .NET/Microsoft.Extensions major-version baseline before B0 can be marked Verified.

## 2026-08-16 — Backend foundation B0, iteration 2

- Branch: `chore/backend-foundation-b0`
- Status: In review
- Result: completed the maintained backend runtime inventory, accepted the all-8.x framework-extension baseline, and introduced golden decision corpus schema version 1 with direct allow, explicit deny precedence, fail-closed miss, tenant isolation, and rewrite traversal scenarios.
- Evidence: locked restore succeeded for nine projects after regenerating lock files; Release build completed with zero warnings and zero errors; 266 unit tests and 23 integration tests passed. The golden corpus test asserts decision, stable reason code, and non-empty trace for every scenario.
- Follow-up: merge the reviewed feature branch locally into `develop` and require the resulting `develop` CI run to pass before changing B0 from `In review` to `Verified`. Promotion from `develop` to `main` remains a manual owner action.

## 2026-08-16 — B0 Linux CI portability correction

- Branch: `fix/architecture-test-cross-platform`
- Status: Verified locally and in Linux reproduction
- Trigger: `develop` Actions run `31955006735` failed its Test step after the B0 merge.
- Diagnosis: the dependency-policy test passed a Windows-style `ProjectReference` value directly to `Path.GetFileNameWithoutExtension`; on Linux, backslash is not a path separator, so valid references were compared as full relative paths.
- Intended correction: normalize both Windows and Unix project-reference separators before extracting the project name, and retain explicit regression cases for both forms.
- Evidence: full Windows locked restore and Release build completed with zero warnings and zero errors; 268 unit tests and 23 integration tests passed. A clean `mcr.microsoft.com/dotnet/sdk:8.0` container built the archived merge commit with the correction overlaid and passed all 268 unit tests, including both separator regression cases.
- Follow-up: merge the hotfix locally into `develop`, push, and verify the replacement `develop` Actions run.

## 2026-08-16 — Backend foundation B0 verification

- Branch: `develop`
- Merge commit: `0e990c6`
- Status: Verified
- Result: the cross-platform architecture guard was merged locally and pushed without changing the pipeline trigger policy. GitHub Actions `.NET CI` run `31955303976` completed successfully for the merge commit.
- Evidence: locked restore, Release build, 268 unit tests, and 23 integration tests passed across the established local/Linux/Actions verification chain.
- Follow-up: start B1 contract governance from an updated `develop` using a new feature branch. Promotion from `develop` to `main` is intentionally left to the repository owner.

## 2026-08-16 — Governed contracts B1, iteration 1

- Branch: `chore/api-contract-governance-b1`
- Status: Complete
- Intended result: make the existing `/api/v1` convention executable, define compatibility policy, and provide a deterministic OpenAPI export without changing authorization behavior or the frontend.
- Implementation: ADR 0006 defines major-version, breaking-change, deprecation, compatibility-surface, and stable-error-code rules. Integration guards inspect the actual MVC action graph and Swagger document. `eng/export-openapi.ps1` exports the runtime-generated contract for future diffing and client generation.
- Pipeline constraint: no workflow file is changed. Publishing the generated artifact from CI remains an explicit B1 gate pending repository-owner approval.
- Evidence: locked restore and Release build completed with zero warnings and zero errors; 268 unit tests and 25 integration tests passed. The exporter generated a valid OpenAPI v1 JSON document containing 53 paths, while the contract guards verified every controller route and HTTP method declaration.
- Merge evidence: feature commit `0240cff` was merged locally into `develop` as `c39e396` and pushed. GitHub Actions `.NET CI` run `31955813080` completed successfully for that merge commit without any pipeline modification.
- Follow-up: add the committed contract baseline, diff report, and generated-client proof in the next B1 branch.

## 2026-08-17 — Governed contracts B1, iteration 2

- Branch: `chore/api-contract-baseline-b1`
- Status: In progress
- Result: committed the reproducible OpenAPI v1 baseline; added a machine-readable removal detector; and proved that the contract generates a strictly compiling TypeScript client without reactivating or coupling to the legacy frontend.
- Contract correction: generation exposed a name collision between Aegis `ApiError` and Kiota's runtime `ApiError`. The wire payload remains unchanged, while the OpenAPI component now uses the stable `AegisApiError` schema ID. The document also declares the deployment-neutral `/` server.
- Toolchain: repository-local Microsoft Kiota 1.34.1, `@microsoft/kiota-bundle` 1.0.0-preview.103, and TypeScript 7.0.2 are exact-pinned for reproducibility. Kiota TypeScript is preview; generated sources are ignored build evidence, not accepted frontend architecture.
- Evidence: baseline and candidate SHA-256 hashes matched; 53 paths were compared with zero removed paths, operations, or schemas. Generated-client dependency audit reported zero vulnerabilities and strict compilation passed. Locked restore, Release build, 268 unit tests, and 25 integration tests passed with zero warnings and errors.
- Pipeline constraint: workflow files remain unchanged. CI artifact publication and automatic invocation of the new contract commands still require repository-owner approval.
- Merge evidence: feature commit `8d0a610` was merged locally into `develop` as `f356cd4` and pushed. GitHub Actions `.NET CI` run `32041840093` completed successfully without modifying the workflow.
- Follow-up: add executable lifecycle fixtures that prove additive changes pass and breaking removals fail, then complete the remaining B1 release evidence.

## 2026-08-17 — Governed contracts B1, iteration 3

- Branch: `test/openapi-contract-lifecycle-b1`
- Status: In progress
- Result: made the contract comparator independently callable against fixtures and added executable lifecycle classification coverage.
- Accepted fixtures: an additive `/api/v1` path and an operation marked deprecated complete without a breaking classification.
- Rejected fixtures: removing a path, HTTP operation, or component schema throws and writes a JSON report whose `breaking` field is true.
- Scope: fixtures and reports live under ignored `artifacts`; the committed OpenAPI baseline remains immutable during verification. No runtime endpoint, authorization behavior, frontend source, or workflow file changes.
- Portability correction: a post-checkout baseline uses CRLF while the generated candidate uses LF. Raw-byte hashing produced a false difference, so the comparator now hashes canonicalized JSON and remains sensitive to semantic structure instead of formatting.
- Evidence: lifecycle harness passed all five classifications; baseline and candidate semantic SHA-256 hashes matched across line endings. Generated TypeScript client strict compilation and npm audit passed. Locked restore, Release build, 268 unit tests, and 25 integration tests passed with zero build warnings or errors.
- Merge evidence: feature commit `a997c5e` was merged locally into `develop` as `3f095da` and pushed. GitHub Actions `.NET CI` run `32042633435` completed successfully without modifying the workflow.

## 2026-08-17 — Governed contracts B1, iteration 4

- Branch: `refactor/api-error-contract-b1`
- Status: In progress
- Intended result: reconcile the full B1 scope and establish a consistent, diagnosable native error contract without breaking compatibility endpoints or authorization behavior.
- Contract: ADR 0007 preserves the v1 `ApiResponse<T>` envelope and adds optional `traceId` and field-keyed validation `details`. `NativeErrorCodes` centralizes stable uppercase identifiers; compatibility endpoints keep their lowercase flat contract.
- Runtime: a global MVC result filter enriches controller errors and request logging metadata. Validation, rate limiting, and exception handling use one distributed trace identifier and safe native/compatibility code mapping.
- Verification: registry and targeted validation, rate-limit, and tenant-error tests pass. The additive OpenAPI baseline update contains only `traceId` and validation `details`; semantic diff and all five lifecycle fixtures passed. Generated TypeScript client strict compilation and npm audit passed. Locked restore, Release build, 269 unit tests, and 25 integration tests passed with zero warnings or errors.
- Merge evidence: feature commit `7abb814` was merged locally into `develop` as `b8f2375` and pushed. GitHub Actions `.NET CI` run `32043384214` completed successfully without modifying the workflow.
- Tracker correction: B1 now lists error, request semantics, mutation safety, application boundaries, and model lifecycle work instead of presenting pipeline publication as the only remaining item.

## 2026-08-17 — Governed contracts B1, iteration 5

- Branch: `feat/request-semantics-b1`
- Status: In progress
- Intended result: establish shared request-cost limits, opaque native cursors, bounded deadlines, and cancellation enforcement without breaking existing collection response shapes.
- Contract: ADR 0008 and the request-semantics reference define page size 50/100, batch cap 1,000, cursor/filter length limits, a 30-second default deadline, and conservative retry rules.
- Compatibility: native relationship changes now emit versioned opaque cursors but accept legacy numeric tokens for rolling upgrades. Compatibility reads retain their existing token format and envelope. Native and compatibility batch checks share the resource cap.
- Runtime: ASP.NET Core request-timeout middleware applies a validated 1–300 second configuration range and returns envelope-specific HTTP 504 errors. Cancellation escapes exception translation so the timeout middleware or disconnected client remains authoritative.
- Verification: targeted codec, page-limit, cursor-reuse, OpenAPI-governance, and cancellation-signature tests pass. The regenerated OpenAPI baseline passes semantic diff and all five lifecycle classifications; the generated TypeScript client passes strict compilation and npm audit. Locked restore, zero-warning Release build, 276 unit tests, and 26 integration tests pass. Actions evidence remains pending until the feature branch is merged locally into `develop` and pushed.
- Merge evidence: feature commit `b587fa0` was merged locally into `develop` as `33de240` and pushed. Actions run `32044172154` failed during `Set up job`, before checkout or any repository step; no source or pipeline correction is justified by that infrastructure-only result. A docs-only merge will provide a traceable replacement run.
- Actions evidence: replacement `.NET CI` run `32044320783` passed for docs-evidence merge `653311c`, validating the same request-semantics source tree without any pipeline modification.

## 2026-08-18 — Governed contracts B1, iteration 6

- Branch: `feat/model-concurrency-b1`
- Status: In progress
- Intended result: prevent silent lost updates for authorization-model definition changes and deletes, while documenting the durable idempotency design boundary.
- Contract: ADR 0009 requires strong ETags and `If-Match`; missing preconditions map to HTTP 428 `PRECONDITION_REQUIRED`, stale revisions to HTTP 412 `CONCURRENCY_CONFLICT`, and malformed tags to HTTP 400 validation errors.
- Persistence: migration 010 adds a positive monotonic revision. PostgreSQL uses revision predicates in the update/delete statements; the in-memory provider uses compare-and-swap behavior.
- Scope boundary: multi-row publish/rollback preconditions and durable idempotency replay remain open and are not represented as completed.
- Verification: the ETag endpoint flow passes; locked restore and zero-warning Release build pass with 276 unit tests and 27 integration tests. The additive OpenAPI baseline, semantic diff, all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass. Actions evidence remains pending until the feature branch is merged locally into `develop` and pushed.
- Merge evidence: feature commit `7109d94` was merged locally into `develop` as `b1c8243` and pushed. GitHub Actions `.NET CI` run `32142650670` passed without modifying the workflow.

## 2026-08-18 — Governed contracts B1, iteration 7

- Branch: `feat/model-lifecycle-concurrency-b1`
- Status: In progress
- Intended result: close the multi-row concurrency gap for authorization-model publish and rollback.
- Contract: both transitions require the target model's strong `If-Match`, return the active model's next ETag, and use the existing HTTP 428/412 native errors.
- Persistence: PostgreSQL locks the owning store row and validates the target revision within the same transaction that publishes the target and archives the previous active model. Migration 011 repairs historical duplicate-published rows deterministically and adds a partial unique index enforcing one published model per store. The in-memory provider applies the transition within one critical section.
- Verification: targeted lifecycle service and endpoint tests pass. Locked restore and zero-warning Release build pass with 276 unit tests and 28 integration tests. The additive OpenAPI baseline, semantic diff, all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass. Actions evidence remains pending until the feature branch is merged locally into `develop` and pushed.
- Merge evidence: feature commit `c7a5a8f` was merged locally into `develop` as `6a0474a` and pushed. GitHub Actions `.NET CI` run `32143817767` passed without modifying the workflow.

## 2026-08-28 — Governed contracts B1, iteration 8

- Branch: `feat/model-create-idempotency-b1`
- Status: In progress
- Intended result: make authorization-model creation safely replayable after ambiguous client timeouts without introducing a business-commit/response-cache gap.
- Contract: optional `Idempotency-Key`, 8–128 safe ASCII characters, 24-hour retention, same-payload HTTP 201 replay, and HTTP 409 `IDEMPOTENCY_CONFLICT` for payload reuse.
- Persistence: migration 012 adds tenant/actor/store/operation-scoped records. PostgreSQL commits reservation, model, and serialized response together; the in-memory provider mirrors semantics under one critical section.
- Scope boundary: no other mutation claims idempotency yet, and Redis is not treated as the durable replay authority.
- Verification: targeted create/replay/conflict coverage passes. Locked restore and zero-warning Release build pass with 276 unit tests and 29 integration tests. The additive OpenAPI baseline, semantic diff, all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass. Actions evidence remains pending until the feature branch is merged locally into `develop` and pushed.
- Merge evidence: feature commit `ef647c5` was merged locally into `develop` as `24f42d8` and pushed. GitHub Actions `.NET CI` run `33182265773` passed without modifying the workflow.

## 2026-08-28 — Governed contracts B1, iteration 9

- Branch: `feat/store-create-idempotency-b1`
- Status: In progress
- Intended result: review every mutation class and protect store creation, the next high-risk resource-allocation endpoint, from duplicate commits.
- Risk review: store/model creates require replay; model edits/lifecycle use concurrency; relationship/RBAC natural-key writes need parity tests; user and assertion mutations wait for explicit use-case transaction ownership; authentication retains protocol-specific defenses.
- Persistence: migration 013 adds a dedicated store-creation reservation because no resource ID exists before commit. Reservation, store insert, and response commit atomically; replays do not dispatch another domain event.
- Known boundary: resource/response replay is atomic, but the current domain-event/outbox path is not enlisted in the resource transaction. Transactional outbox persistence remains tracked rather than being implied by idempotency success.
- Verification: targeted store create/replay/conflict coverage passes. Locked restore and zero-warning Release build pass with 276 unit tests and 30 integration tests. The additive OpenAPI baseline, semantic diff, all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass. Actions evidence remains pending until the feature branch is merged locally into `develop` and pushed.
- Merge evidence: feature commit `3b4966d` was merged locally into `develop` as `d6ed257` and pushed. GitHub Actions `.NET CI` run `33183532413` passed without modifying the workflow.

## 2026-08-28 — Governed contracts B1, iteration 10

- Branch: `refactor/store-create-use-case-b1`
- Status: In progress
- Intended result: establish an explicit application command boundary without changing the public store-create contract.
- Boundary: `CreateStoreUseCase` owns validation, aggregate creation, repository transaction selection, replay-aware event dispatch, and DTO mapping. The API retains authentication/tenant/header concerns; repositories retain atomic persistence.
- Migration safety: `StoresController.Create` consumes the use case directly. Existing `IStoreAppService` create methods delegate to it temporarily so internal callers are not broken by a flag-day refactor.
- Follow-up: extract the authorization-model validator before moving model commands, then review user/assertion transaction owners.
- Verification: targeted use-case, compatibility-delegate, and existing endpoint tests pass. Locked restore and zero-warning Release build pass with 278 unit tests and 30 integration tests. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Merge evidence: feature commit `844a5e5` was merged locally into `develop` as `d271558` and pushed. GitHub Actions `.NET CI` run `33184947735` passed without modifying the workflow.

## 2026-08-28 — Governed contracts B1, iteration 11

- Branch: `refactor/authorization-model-validator-b1`
- Status: In progress
- Intended result: make authorization-model DSL validation an independent application component before extracting model commands.
- Boundary: `AuthorizationModelValidator` owns schema/type/relation validation, stable issue details, rewrite feature detection, summary generation, and cooperative cancellation. It has no repository, service, transport, or infrastructure dependency.
- Migration safety: `AuthorizationModelAppService.ValidateAsync` delegates to the validator, and existing constructors retain compatibility while the DI composition root injects the shared stateless validator.
- Contract impact: none intended; validation DTOs, stable issue codes, line numbers, warnings, and endpoint behavior remain unchanged.
- Follow-up: extract authorization-model creation and idempotent replay orchestration into a command use case, followed by update and lifecycle commands.
- Verification: 14 targeted validator, compatibility-service, and dependency-injection tests pass. Locked restore and zero-warning Release build pass with 281 unit tests and 30 integration tests. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Merge evidence: feature commit `2d62cff` was merged locally into `develop` as `31dd355` and pushed. GitHub Actions `.NET CI` run `33185735354` passed without modifying the workflow.

## 2026-08-28 — Governed contracts B1, iteration 12

- Branch: `refactor/authorization-model-create-use-case-b1`
- Status: In progress
- Intended result: move authorization-model creation and durable replay orchestration behind one explicit command boundary.
- Boundary: `CreateAuthorizationModelUseCase` owns command validation, store existence, aggregate construction and validated state, repository transaction selection, replay-aware event dispatch, and DTO mapping. The API owns trusted tenant/actor derivation, header parsing, fingerprinting, status codes, and ETags.
- Transaction safety: `IAuthorizationModelRepository.AddIdempotentAsync` remains the atomic owner of reservation lookup, fingerprint conflict detection, model insert, and response replay. The use case dispatches creation events only when the repository reports a newly created aggregate.
- Migration safety: `AuthorizationModelsController.Create` consumes the use case directly. Existing `IAuthorizationModelAppService` create methods delegate temporarily so internal callers remain compatible.
- Contract impact: none intended; route, payload, status, ETag, idempotency scope, and error mapping remain unchanged.
- Follow-up: extract model update/delete commands, then store-serialized publish/rollback lifecycle commands.
- Verification: 13 targeted command, compatibility-service, and dependency-injection unit tests and one replay endpoint integration test pass. Locked restore and zero-warning Release build pass with 283 unit tests and 30 integration tests. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Merge evidence: feature commit `fc53d80` was merged locally into `develop` as `b74a29d` and pushed. GitHub Actions `.NET CI` run `33186355615` passed without modifying the workflow.

## 2026-08-28 — Governed contracts B1, iteration 13

- Branch: `refactor/authorization-model-update-delete-b1`
- Status: In progress
- Intended result: isolate authorization-model definition update and delete behind explicit optimistic-concurrency command boundaries.
- Boundary: the API owns strong `If-Match` parsing and response ETags. `UpdateAuthorizationModelUseCase` owns DSL validation, aggregate mutation, compare-and-write coordination, conflict classification, event dispatch, and DTO mapping. `DeleteAuthorizationModelUseCase` owns compare-and-delete coordination, conflict classification, and deletion-event dispatch.
- Concurrency safety: repository revision predicates remain atomic. When a mutation loses the race, the use case re-reads the model to distinguish concurrent modification from concurrent removal; only a still-existing model produces `ConcurrencyConflictException`.
- Migration safety: controller update/delete actions consume the command use cases directly. Broad-service methods remain temporary delegates for internal callers.
- Contract impact: none intended; required ETags, HTTP 428/412 behavior, not-found mapping, payloads, and tenant/store guards remain unchanged.
- Follow-up: extract store-serialized publish and rollback lifecycle commands, then remove model mutation delegates after caller review.
- Verification: 14 targeted command, compatibility-service, and dependency-injection unit tests and the strong-ETag update endpoint integration test pass. Locked restore and zero-warning Release build pass with 286 unit tests and 30 integration tests. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Merge evidence: feature commit `05c5976` was merged locally into `develop` as `1a7b5dc` and pushed. GitHub Actions `.NET CI` run `33187045638` passed without modifying the workflow.

## 2026-08-28 — Governed contracts B1, iteration 14

- Branch: `refactor/authorization-model-lifecycle-b1`
- Status: In progress
- Intended result: isolate authorization-model publish and rollback orchestration without weakening the atomic lifecycle invariant.
- Boundary: the API owns strong `If-Match` parsing and response ETags. Publish/rollback use cases own target validation, preflight revision checks, repository transition coordination, conflict classification, response mapping, and post-success rollback audit.
- Transaction safety: the production repository retains the store-scoped lock and rechecks the target revision inside the lifecycle transaction. Archiving the previous active model and publishing the target remain atomic; the partial unique index still enforces at most one published row.
- Compatibility safety: the registry-only multi-call transition remains available for legacy/test providers but is explicitly not the production atomicity boundary. Broad-service lifecycle methods remain temporary delegates.
- Contract impact: none intended; routes, required ETags, HTTP 428/412/not-found behavior, response payloads, and tenant/store guards remain unchanged.
- Follow-up: review and remove model mutation delegates after internal caller migration, then assess user/assertion transaction ownership.
- Verification: 14 targeted lifecycle, compatibility-service, and dependency-injection unit tests and two lifecycle endpoint integration tests pass. Locked restore and zero-warning Release build pass with 289 unit tests and 30 integration tests. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Merge evidence: feature commit `ad0a7ff` was merged locally into `develop` as `16c4925` and pushed. GitHub Actions `.NET CI` run `33187746559` passed without modifying the workflow.

## 2026-08-28 — Governed contracts B1, iteration 15

- Branch: `refactor/remove-model-mutation-delegates-b1`
- Status: In progress
- Intended result: close the authorization-model compatibility debt after all mutation callers migrated to explicit command boundaries.
- Caller evidence: production search finds `IAuthorizationModelAppService` only in DI and `AuthorizationModelsController`; the controller uses it for list/get/diff/validate only. Create, update, delete, publish, and rollback use cases are the exclusive HTTP mutation dependencies.
- Interface change: remove all five mutation families from `IAuthorizationModelAppService` and their concrete delegates. The implementation now depends only on registries/repository projection and `AuthorizationModelValidator`, not event dispatch or audit infrastructure.
- Test migration: obsolete service-level publish/rollback tests were removed because direct lifecycle-use-case tests cover the same behavior plus stale revisions, the single-published invariant, and rollback audit. Diff fixtures now seed through the registry and test the remaining service responsibility directly.
- Contract impact: none; the change is internal to the Application composition boundary and does not alter HTTP routes, payloads, status codes, ETags, OpenAPI, or tenant/store guards.
- Follow-up: audit and remove temporary store-create delegates, then assess user/assertion mutation transaction ownership.
- Verification: 12 targeted query-service, lifecycle-use-case, and dependency-injection tests pass. Locked restore and zero-warning Release build pass with 287 unit tests and 30 integration tests. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Merge evidence: feature commit `086a73f` was merged locally into `develop` as `d04d35b` and pushed. GitHub Actions `.NET CI` run `33188518304` passed without modifying the workflow.

## 2026-08-28 — Governed contracts B1, iteration 16

- Branch: `refactor/remove-store-create-delegates-b1`
- Status: In progress
- Intended result: close the remaining store-create compatibility debt after all production callers migrated to the command boundary.
- Caller evidence: `StoresController.Create` depends directly on `CreateStoreUseCase`; no production or test caller invokes create through `IStoreAppService` or `StoreAppService`.
- Interface change: remove unscoped create, tenant-scoped create, and idempotent create from `IStoreAppService` and its implementation. The remaining service surface owns list/get/delete behavior.
- Composition safety: remove `CreateStoreUseCase.CreateCompatibility` and nullable repository/dispatcher fields. The public constructor is now the only composition path and requires both dependencies.
- Contract impact: none; HTTP route, payload, idempotency behavior, tenant/actor derivation, status codes, and store isolation remain unchanged.
- Follow-up: audit dormant compatibility factories inside the authorization-model command classes, then review user/assertion mutation transaction ownership.
- Verification: 11 targeted store-use-case, remaining store-service, and dependency-injection tests pass. Locked restore and zero-warning Release build pass with 287 unit tests and 30 integration tests. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Merge evidence: feature commit `bc3988f` was merged locally into `develop` as `7f9617d` and pushed. GitHub Actions `.NET CI` run `33189134472` passed without modifying the workflow.

## 2026-08-28 — Governed contracts B1, iteration 17

- Branch: `refactor/remove-model-command-compatibility-b1`
- Status: In progress
- Intended result: make authorization-model command dependencies and transaction ownership strict after broad-service delegate removal.
- Dead-code evidence: no caller remained for the five `CreateCompatibility` factories. Their private boolean constructors, nullable collaborators, and registry-only mutation branches were reachable only through those factories.
- Composition change: create/update/delete require repository and event dispatcher; publish requires repository; rollback requires repository and audit store. Validators and store registry remain explicit where command validation and store existence require them.
- Lifecycle consistency: rollback now reads the current published model through `IAuthorizationModelRepository.GetPublishedByStoreAsync`, so its snapshot and transition use one persistence abstraction. Production and in-memory tests follow the same orchestration algorithm.
- Contract impact: none; HTTP routes, payloads, ETags, status/error mapping, idempotency, tenant/store guards, and database invariants remain unchanged.
- Follow-up: review user and assertion mutations, identify their repository transaction owners, and extract only commands whose atomicity can be stated explicitly.
- Verification: 15 targeted authorization-model command, remaining query-service, and dependency-injection tests pass. Locked restore and zero-warning Release build pass with 287 unit tests and 30 integration tests. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Merge evidence: feature commit `b70eaa1` was merged locally into `develop` as `c6519df` and pushed. GitHub Actions `.NET CI` run `33189763090` passed without modifying the workflow.

## 2026-08-28 — Governed contracts B1, iteration 18

- Branch: `refactor/user-mutation-use-cases-b1`
- Status: Complete
- Intended result: isolate tenant user create/update/delete behind explicit commands whose repository transaction ownership can be stated and tested.
- Boundary: `UsersController` retains tenant access enforcement and HTTP mapping. The three user mutation use cases own command validation and repository coordination. `RbacAdminService` retains user queries and RBAC role/permission behavior but no longer exposes profile mutation delegates.
- Transaction safety: PostgreSQL update returns the row from `UPDATE ... RETURNING`, removing the former update/read race. Delete wraps assignment cleanup and user deletion in an explicit transaction and derives its boolean from the user deletion. Tenant predicates remain mandatory in every operation.
- Assertion review: definition state is still held in a static process-local dictionary and only run history has a durable store. Assertion command extraction is deferred until a scoped repository defines replace/append concurrency and purge semantics.
- Contract impact: none intended; routes, payloads, status/error mapping, tenant guard, and OpenAPI shape remain unchanged.
- Follow-up: add the persistent assertion-definition repository and migration before extracting assertion write/run/generate commands; review role/permission existence and conflict semantics separately.
- Verification: 7 targeted user-boundary and dependency-injection tests pass. Locked restore and zero-warning Release build pass with 290 unit tests and 30 integration tests. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Merge evidence: feature commit `75ec6c0` was merged locally into `develop` as `6b18cb1` and pushed. GitHub Actions `.NET CI` run `33191447242` passed without modifying the workflow.

## 2026-08-28 — Governed contracts B1, iteration 19

- Branch: `feat/durable-assertion-repository-b1`
- Status: Complete
- Intended result: establish a durable transaction owner for assertion definitions before extracting assertion commands.
- Persistence boundary: `IAssertionRepository` reads versioned snapshots, atomically replaces a set, atomically appends distinct assertions under a maximum, and purges all sets for one store. PostgreSQL migration 014 stores one JSONB set per store/model; the in-memory provider implements the same behavior.
- Concurrency safety: PostgreSQL takes a transaction-scoped advisory lock before reading and upserting, including the first-write case. In-memory mutations use a per-key critical section. Failed capacity checks do not advance or overwrite the current snapshot.
- Composition safety: `AssertionAppService` no longer owns static definition state or offers a partial constructor. Permission checking, definition persistence, run history, and audit querying are required dependencies.
- Contract impact: none intended; the snapshot revision remains internal, and HTTP routes, payloads, status codes, tenant/store guards, and OpenAPI remain unchanged.
- Follow-up: extract assertion write/run/generate use cases, then make an additive contract decision for recording the executed definition revision in run history.
- Verification: 15 targeted assertion/store tests and the assertion lifecycle endpoint integration test pass. Locked restore and zero-warning Release build pass with 293 unit tests and 30 integration tests. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Merge evidence: feature commit `f759d8e` was merged locally into `develop` as `c5dd3d0` and pushed. GitHub Actions `.NET CI` run `33192371166` passed without modifying the workflow.

## 2026-08-29 — Governed contracts B1, iteration 20

- Branch: `refactor/assertion-write-use-case-b1`
- Status: Complete
- Intended result: make assertion replacement an explicit command and establish one validation component for persisted and audit-generated assertions.
- Boundary: `StoreAssertionsController` retains store-tenant authorization and HTTP mapping. `WriteAssertionsUseCase` owns store/model scope validation, capacity, tuple/model-reference validation, cancellation, and the repository replace call.
- Validation consistency: `AssertionValidator` is stateless and shared by write and audit generation. Invalid tuple, contextual tuple, type, or relation input is rejected before repository mutation.
- Interface cleanup: remove `WriteAsync` from `IAssertionAppService` and `AssertionAppService` after the controller and tests migrate to the command.
- Contract impact: none intended; routes, request/response payloads, error codes, tenant/store guards, and OpenAPI remain unchanged.
- Follow-up: extract run orchestration against one captured assertion snapshot, then extract audit generation and decide how snapshot revision is recorded.
- Verification: 14 targeted write, assertion-service, and dependency-injection tests plus the assertion lifecycle endpoint integration test pass. Locked restore and zero-warning Release build pass with 296 unit tests and 30 integration tests. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Merge evidence: feature commit `d3b7805` was merged locally into `develop` as `9e86376` and pushed. GitHub Actions `.NET CI` run `33257467077` passed without modifying the workflow.

## 2026-08-29 — Governed contracts B1, iteration 21

- Branch: `refactor/assertion-run-use-case-b1`
- Status: Complete
- Intended result: isolate assertion execution behind one explicit use case that evaluates a stable definition snapshot and appends only completed run history.
- Boundary: `StoreAssertionsController` retains tenant/store authorization and HTTP mapping. `RunAssertionsUseCase` owns store/model validation, snapshot capture, permission-check iteration, result aggregation, cancellation, and completed-history persistence.
- Consistency: one `AssertionSetSnapshot` is read before evaluation and its collection is used for the entire run. Concurrent definition replacement cannot alter the captured work set. The current internal revision is deliberately not added to the public run DTO in this non-contract-changing slice.
- Failure semantics: invalid scope, cancellation, or a failed permission check cannot persist a completed run record. An empty definition is valid and persists a zero-result run for operational traceability.
- Interface cleanup: remove `RunAsync` and the permission-check dependency from the broad assertion service after controller and test migration.
- Contract impact: none intended; routes, payloads, error/status mapping, tenant/store guards, and OpenAPI remain unchanged.
- Follow-up: extract audit generation and then make the additive contract decision for definition revision in run records.
- Verification: 13 targeted run, remaining assertion-service, and dependency-injection tests plus the assertion lifecycle endpoint integration test pass. Locked restore and zero-warning Release build pass with 298 unit tests and 30 integration tests. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Merge evidence: feature commit `7f2dc2f` was merged locally into `develop` as `28da9a0` and pushed. GitHub Actions `.NET CI` run `33257783985` passed without modifying the workflow.

## 2026-08-29 — Governed contracts B1, iteration 22

- Branch: `refactor/assertion-generate-use-case-b1`
- Status: Complete
- Intended result: isolate audit-derived assertion generation and its optional atomic append behind one explicit command boundary.
- Boundary: `StoreAssertionsController` retains tenant/store authorization and HTTP mapping. `GenerateAssertionsFromAuditUseCase` owns store/model validation, decision and limit validation, scoped audit querying, candidate filtering/deduplication, and optional repository append.
- Mutation safety: draft-only generation never writes. Append delegates combined-set deduplication, capacity enforcement, serialization, and revision advance to `IAssertionRepository`; a capacity rejection preserves the prior snapshot.
- Interface cleanup: remove generation from `IAssertionAppService` and remove audit/validator dependencies from `AssertionAppService`. Its remaining surface is definition read, run-history read, and purge coordination.
- Contract impact: none intended; routes, payloads, stable error codes, tenant/store guards, and OpenAPI remain unchanged.
- Follow-up: decide and implement the additive definition-revision contract for run history, then review whether read/history/purge deserve narrower query/lifecycle boundaries.
- Verification: 13 targeted generation, remaining assertion-service, and dependency-injection tests plus the assertion lifecycle endpoint integration test pass. Locked restore and zero-warning Release build pass with 300 unit tests and 30 integration tests. The runtime OpenAPI remains semantically compatible; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Merge evidence: feature commit `c873c66` was merged locally into `develop` as `6bcf734` and pushed. GitHub Actions `.NET CI` run `33258613352` passed without modifying the workflow.

## 2026-08-29 — Governed contracts B1, iteration 23

- Branch: `feat/assertion-run-definition-revision-b1`
- Status: Complete
- Intended result: make completed assertion history identify the exact definition snapshot that was evaluated.
- Persistence: migration 015 adds non-negative `definition_revision` to assertion run records and backfills existing rows with zero. PostgreSQL save/list/get operations persist and hydrate the field; new runs copy it from the single snapshot captured before evaluation.
- Contract decision: `AegisAssertionRunRecordDto` exposes additive `definition_revision` as `int64`. Zero means legacy history or no definition set written at capture time; positive values identify a repository definition revision.
- Compatibility evidence: the contract report retains 53 paths, removes no path, operation, or schema, and classifies the candidate as non-breaking. The reviewed candidate is promoted to the committed v1 baseline.
- Verification: 9 targeted unit tests and 5 assertion lifecycle integration tests pass. Locked restore and zero-warning Release build pass with 300 unit tests and 30 integration tests. The promoted runtime OpenAPI is semantically identical to its baseline; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Follow-up: review whether assertion definition reads, run-history queries, and store purge coordination should remain grouped or move to narrower query/lifecycle boundaries.
- Merge evidence: feature commit `2339325` was merged locally into `develop` as `22ded7e` and pushed. GitHub Actions `.NET CI` run `33259188992` passed without modifying the workflow.

## 2026-08-29 — Governed contracts B1, iteration 24

- Branch: `refactor/assertion-query-lifecycle-boundaries-b1`
- Status: Complete
- Intended result: finish the assertion HTTP boundary split without overstating store-purge consistency.
- Query boundaries: definition read, run-history list, and run-detail lookup now use `ReadAssertionsUseCase`, `ListAssertionRunsUseCase`, and `GetAssertionRunUseCase`. `AssertionScopeGuard` preserves store/model validation and compatibility error behavior; the controller retains tenant authorization and HTTP mapping.
- Interface cleanup: remove `IAssertionAppService` and its broad implementation after all HTTP callers migrate. Composition registers explicit query use cases directly.
- Lifecycle boundary: store deletion depends on `AssertionStorePurgeCoordinator`. It performs definition and run-history cleanup sequentially and is deliberately not described as atomic; transactional/recoverable cleanup remains B3 work.
- Contract impact: none intended; routes, payloads, status/error mapping, tenant/store guards, and OpenAPI remain unchanged.
- Verification: 19 targeted query, store-deletion, and composition unit tests plus 5 assertion lifecycle integration tests pass. Locked restore and zero-warning Release build pass with 303 unit tests and 30 integration tests. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Follow-up: design one durable store-deletion transaction or recoverable workflow spanning assertion, relationship, RBAC, and store state.
- Merge evidence: feature commit `830d10f` was merged locally into `develop` as `19aa1af` and pushed. GitHub Actions `.NET CI` run `33259603421` passed without modifying the workflow.

## 2026-08-29 — Durable data correctness B3, iteration 25

- Branch: `feat/atomic-store-deletion-b3`
- Status: Complete
- Intended result: eliminate partial production cleanup when a tenant-scoped store is deleted.
- Transaction owner: `PostgresStoreDeletionRepository` performs one tenant/store-predicated parent delete. Migration 016 adds composite tenant/store foreign keys with cascade semantics for relationships, change rows, and store RBAC; existing model, assertion, run-history, and idempotency foreign keys complete the operational cascade.
- Isolation: composite foreign keys reject new cross-tenant child ownership, and an incorrect tenant predicate deletes neither the store nor its children.
- Retention: tenant user profiles and audit events do not cascade. Audit keeps the former store ID as historical security evidence.
- Rollout safety: new constraints are `NOT VALID`, so new writes are enforced without silently deleting or blocking deployment on unknown legacy orphans. Inventory, reconciliation, and explicit validation remain tracked B3 evidence.
- Provider semantics: in-memory deletion is serialized and functionally equivalent but is not crash-durable. PostgreSQL is the production atomicity boundary.
- Verification: 9 focused store-boundary/composition unit tests pass; a PostgreSQL 16 container test runs migrations and proves cross-tenant no-op, atomic cascade across seeded operational tables, and audit retention. Locked restore and zero-warning Release build pass with 303 unit tests and 31 integration tests. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Follow-up: validate legacy foreign keys, add injected-failure evidence, and perform backup/restore plus reconciliation drills.
- Merge evidence: feature commit `23f87a2` was merged locally into `develop` as `05d9176` and pushed. GitHub Actions `.NET CI` run `33260311533` passed without modifying the workflow.

## 2026-08-29 — Durable data correctness B3, iteration 26

- Branch: `feat/store-constraint-reconciliation-b3`
- Status: Complete
- Intended result: make legacy integrity discovery and migration-016 validation repeatable, reviewable, and safe by default.
- Tooling: `Aegis.DatabaseAdmin` reads credentials only from `ConnectionStrings__Aegis`, emits a JSON report, returns `2` for discovered violations, and performs no mutation unless `--validate` is explicit. The PowerShell entry point restores in locked mode and writes evidence under `artifacts/database` by default.
- Audit contract: report every target table's constraint status and total orphan/mismatched tenant-store count, with at most 20 identifier samples. The tool never guesses repairs or deletes data.
- Validation guard: validation is refused when any violation exists. With a clean audit, all six constraints validate inside one transaction and their catalog state is reread into the report.
- Operational guidance: the runbook defines secret handling, restored-copy rehearsal, reconciliation ownership, exit codes, evidence retention, and failure response.
- Verification: zero-warning Release build passes with the database-admin tool included. PostgreSQL 16 container coverage injects one legacy orphan by bypassing triggers, proves detection and validation refusal, removes the test orphan, and proves transactional validation of all six constraints. Missing secret configuration returns exit code 64 without exposing credentials. Locked restore, 303 unit tests, and 31 integration tests pass. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Follow-up: execute the runbook per managed environment, then add injected-failure and backup/restore evidence.
- Merge evidence: feature commit `253cb2f` was merged locally into `develop` as `93e8582` and pushed. GitHub Actions `.NET CI` run `33260956484` passed without modifying the workflow.

## 2026-08-29 — Durable data correctness B3, iteration 27

- Branch: `feat/postgres-restore-drill-b3`
- Status: Complete
- Intended result: turn logical backup compatibility and atomic rollback from assumptions into repeatable evidence.
- Restore automation: `eng/test-postgres-restore.ps1` creates isolated GUID-named PostgreSQL 16 source/target containers, applies committed migrations, seeds a deterministic authorization fixture, creates and hashes a custom-format dump, restores with exit-on-error, verifies exact counts/fixture, and runs constraint reconciliation plus validation.
- Data safety: no existing database is contacted; credentials are ephemeral; the dump lives only under ignored artifacts and is deleted in `finally`; exact drill containers are forcibly removed. JSON retains versions, timings, counts, dump hash, and linked reconciliation evidence.
- Failure evidence: the PostgreSQL integration test installs a temporary trigger that raises during relationship cascade deletion. The parent delete throws and store, relationship, and assertion state remain intact; after trigger removal the atomic delete succeeds.
- Scope honesty: the final 13.827-second local synthetic restore is compatibility evidence, not a production RTO. A staging-sized managed restore must still run full application golden decisions and measure declared RPO/RTO.
- Contract impact: none; runtime HTTP/OpenAPI behavior is unchanged.
- Verification: three isolated restore rehearsals and the focused PostgreSQL failure-injection test pass. Locked restore and zero-warning Release build pass with 303 unit tests and 31 integration tests. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Follow-up: execute the managed restore runbook, retain approved evidence, and expand failure testing to migration/connection interruption scenarios.
- Merge evidence: feature commit `1bdd4a4` was merged locally into `develop` as `951da3d` and pushed. GitHub Actions `.NET CI` run `33261885697` passed without modifying the workflow.

## 2026-08-29 — Durable data correctness B3, iteration 28

- Branch: `feat/migration-execution-safety-b3`
- Status: Complete
- Intended result: make database startup fail safely and diagnostically under concurrent migration, drift, timeout, and interrupted SQL conditions.
- Serialization: one fixed PostgreSQL session advisory lock protects history inspection and migration execution. Competing instances poll until the configured deadline, reread committed history after acquisition, and explicitly unlock in `finally`.
- Provenance: `schema_migrations` now records normalized SHA-256 values. Applied files are immutable; mismatches and applied names absent from the build fail startup. Legacy null checksums bootstrap once from same-named embedded resources while locked.
- Atomicity: each SQL resource and its history insert remain in one transaction. A statement failure, cancellation, or connection loss cannot commit a success marker independently of schema work.
- Deadlines: `Database:Migrations:LockTimeoutSeconds` defaults to 30 and `StatementTimeoutSeconds` to 120; non-positive values are rejected.
- Operational guidance: the migration article covers pre-checksum rollout, pooled-session lock behavior, drift/SQL failure response, and the remaining need to separate managed DDL authority from application replicas.
- Verification: PostgreSQL 16 coverage runs four concurrent migrators and observes 16 unique checksummed rows, forces lock timeout, releases the manually held pooled-session lock, bootstraps a legacy null checksum, corrupts one checksum, and verifies fail-closed drift detection. Locked restore and zero-warning Release build pass with 303 unit tests and 32 integration tests. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Follow-up: test connection interruption against an intentionally long transaction and design the separate managed migration job.
- Merge evidence: feature commit `e3cf1e7` was merged locally into `develop` as `d80ff0b` and pushed. GitHub Actions `.NET CI` run `33262928783` passed without modifying the workflow.

## 2026-08-29 — Durable data correctness B3, iteration 29

- Branch: `test/migration-connection-interruption-b3`
- Status: Complete
- Intended result: turn the migration runner's connection-loss rollback claim into deterministic PostgreSQL evidence.
- Failure injection: make migration 016 pending, hold an exclusive lock on `stores`, observe the blocked migration PID through its unique DDL text in `pg_stat_activity`, and terminate only that backend.
- Required assertions: the interrupted runner fails, migration 016 has no committed history marker, and a clean retry records it exactly once before checksum drift enforcement is tested.
- Scope boundary: local PostgreSQL 16 evidence only. Managed-environment rehearsal and a separately authorized migration job remain pending deployment decisions.
- Verification: the focused PostgreSQL 16 interruption test passes. Locked restore and zero-warning Release build pass with 303 unit tests and 32 integration tests. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Follow-up: design and approve the separately authorized migration job, then rehearse interruption and recovery through the managed deployment identity.
- Merge evidence: feature commit `9aa0ed0` was merged locally into `develop` as `01afe97` and pushed. GitHub Actions `.NET CI` run `33263431325` passed without modifying the workflow.

## 2026-08-29 — Durable data correctness B3, iteration 30

- Branch: `feat/separate-migration-authority-b3`
- Status: Complete
- Intended result: separate schema mutation capability from the normal API replica startup path while retaining simple monolith/local operation.
- Runtime modes: `Database:Migrations:Mode=Apply` remains the default; `Validate` performs read-only schema-history/completeness/checksum verification and rejects development seeding.
- Operator boundary: `Aegis.Migrator` plus `eng/migrate-database.ps1` provides a one-shot process that reads credentials from `ConnectionStrings__Aegis`, applies migrations, validates the final schema, and exits without hosting HTTP or seed behavior.
- Safety evidence: missing migrator credentials return usage exit code 64. PostgreSQL 16 coverage proves validate success on a complete schema and failure on pending history, missing checksum, and checksum drift while existing interruption/retry evidence remains intact.
- Scope boundary: repository code and runbook only. No pipeline, deployment resource, database grant, or managed environment has been changed.
- Verification: missing-secret exit behavior and the focused PostgreSQL 16 validation/interruption test pass. Locked restore and zero-warning Release build pass with 303 unit tests and 32 integration tests. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Follow-up: provision distinct managed migration/runtime identities, apply least-privilege grants, order migrator before `Validate` replicas, and retain a rollback rehearsal before claiming the environment cutover.
- Merge evidence: feature commit `9dd669f` was merged locally into `develop` as `c973e58` and pushed. GitHub Actions `.NET CI` run `33264009438` passed without modifying the workflow.

## 2026-08-30 — Durable data correctness B3, iteration 31

- Branch: `feat/durable-postgres-outbox-b3`
- Status: Complete
- Architecture direction: reaffirm ADR 0001. Aegis remains a modular monolith; Aspire, orchestration, and microservice extraction require measured business/operational evidence and are not near-term goals.
- Intended result: ensure PostgreSQL outbox work and retry state survive process recreation while retaining the simple single-service deployment profile.
- Persistence: migration 017 adds durable JSON payload, event time, creation time, attempt/error, next-attempt, and processed state with a due-message partial index.
- Runtime: PostgreSQL composition selects `PostgresDomainEventOutboxStore`; local/test profiles retain the in-memory implementation. The worker consumes validated batch/poll/retry options and propagates requested cancellation.
- Retry: failed delivery records a bounded 4,000-character error and exponential next-attempt delay capped by configuration; success clears the error and records completion.
- Scope honesty: business mutation and outbox append are not yet one transaction, and messages are not claimed with leases. Logging publication, poison handling, backlog telemetry, replay, and retention remain pending.
- Verification: focused PostgreSQL 16 tests prove the 17-migration history and durable outbox append, store reconstruction, delayed retry state, error clearing, and completion. Locked restore and zero-warning Release build pass with 303 unit tests and 33 integration tests. Runtime OpenAPI remains semantically identical; all five lifecycle fixtures, generated TypeScript strict compilation, and npm audit pass.
- Follow-up: design transaction ownership for business state plus outbox append before adding claim leases and operational delivery controls.
- Merge evidence: feature commit `d14113b` was merged locally into `develop` as `c7addd0` and pushed. GitHub Actions `.NET CI` run `33264519329` passed without modifying the workflow.
