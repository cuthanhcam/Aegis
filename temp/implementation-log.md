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
- Status: In progress
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
