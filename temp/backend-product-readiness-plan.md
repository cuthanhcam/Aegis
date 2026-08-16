# Backend product-readiness plan

> Workstream: Backend
> Release train: independent from the console, synchronized through contracts
> Non-negotiable invariants: deterministic authorization, fail-closed behavior, tenant/store isolation, auditable mutation, explainable decision paths

## Target backend shape

```text
HTTP / SDK / Events
        |
        v
API contracts and policy enforcement
        |
        v
Application use cases and transaction boundaries
        |
        v
Authorization runtime ---- Model snapshot / decision cache
        |
        v
Tenant-aware persistence ---- Outbox / audit ledger
        |
        v
PostgreSQL, Redis, identity provider, telemetry backend
```

Dependencies point inward. Domain and authorization logic never depend on ASP.NET Core, PostgreSQL, Redis, or telemetry vendors. Every request carries an immutable execution context containing correlation, tenant, store, actor, model version, deadline, and cancellation information where applicable.

## Phase B0 — Baseline and architecture guardrails

**Outcome:** the team can change the backend without silently changing security behavior.

### Work packages

- Record the ten architecture decisions listed in the assessment; assign owners and review dates.
- Inventory every endpoint, authorization policy, repository implementation, cache key, migration, background service, metric, and configuration key.
- Introduce central package management and align Microsoft package majors with the supported .NET runtime. Document upgrade cadence and LTS policy.
- Add dependency architecture tests: Domain and Authorization stay framework-independent; API cannot reach Infrastructure internals; Contracts contain serialization shapes only.
- Create a single backend verification command covering formatting, restore, build with warnings as errors, unit tests, integration tests, architecture tests, and package vulnerability checks.
- Define a decision golden corpus containing allow, deny, explicit deny, rewrite, contextual, depth-limit, missing model, malformed tuple, and cross-tenant scenarios.

### Exit criteria

- Current API and decision behavior is captured by executable tests.
- No unexplained package-major mismatch remains.
- CI publishes test, coverage, dependency, and OpenAPI artifacts.
- Known debt is recorded with severity, owner, and target phase.

## Phase B1 — Contract and application boundary

**Outcome:** APIs can evolve safely and frontend/SDK consumers have one source of truth.

### Work packages

- Establish `/api/v1` as the governed native surface; classify compatibility endpoints separately and document behavioral differences.
- Standardize Problem Details with stable error codes, trace ID, validation errors, and safe metadata. Preserve compatibility envelopes only where promised.
- Define cursor pagination, filtering, sorting, maximum page/batch sizes, request limits, and cancellation behavior.
- Add idempotency keys for retryable mutation APIs and optimistic concurrency tokens for mutable resources.
- Generate and validate OpenAPI in CI. Add breaking-change detection and consumer contract tests.
- Split broad application services and controllers around use cases. Each handler owns validation orchestration, authorization requirement, transaction boundary, and result mapping.
- Make authorization model lifecycle explicit: draft → validated → tested → published → active → superseded; activation is atomic and rollbackable.

### Exit criteria

- Every public route has version, authentication, authorization, error, retry, and idempotency documentation.
- OpenAPI diff blocks accidental breaking changes.
- Generated TypeScript client passes fixture-based contract tests.
- Model activation and rollback are integration-tested under concurrent reads.

## Phase B2 — Identity, tenancy, and security hardening

**Outcome:** production identity and isolation are enforced at more than one layer.

### Work packages

- Replace production issuance from demo credentials with external OIDC/JWKS validation. Keep demo auth behind an explicit development-only registration.
- Define human roles, service principals, scoped API credentials, credential expiry, rotation, revocation, and break-glass access.
- Replace assertion-style policy checks with named requirements and handlers that produce auditable authorization outcomes.
- Introduce a strongly typed tenant/store execution context. Repositories require scope explicitly rather than reading ambient HTTP state.
- Add database tenant constraints and evaluate PostgreSQL row-level security as a second boundary. Prove whichever strategy is selected with negative tests.
- Namespace every cache key with tenant, store, model version, and relevant subject/object inputs. Add collision and invalidation tests.
- Threat-model login, token refresh, model publication, tuple writes, graph expansion, explain output, audit export, and administrative role changes.
- Apply request/body/header limits, trusted proxy policy, strict CORS by environment, secure headers, secret-provider integration, and log redaction.

### Exit criteria

- Cross-tenant read/write/check test matrix passes at HTTP, repository, database, and cache boundaries.
- Key rotation and credential revocation are demonstrated without a full deployment.
- High-risk threat-model findings are closed or explicitly accepted by an owner.
- Security scans and an SBOM are release artifacts.

## Phase B3 — Data integrity and reliable processing

**Outcome:** data changes remain correct across retries, crashes, deployments, and cache lag.

### Work packages

- Define transaction boundaries for models, relationships, audit entries, and outbox records.
- Evolve migrations to an expand/contract discipline with checksums, locking, compatibility windows, and a separate deployment step rather than uncontrolled application startup mutation.
- Add unique keys, foreign keys, tenant-aware indexes, retention indexes, and concurrency controls based on real query plans.
- Make outbox publishing idempotent; add retry backoff, poison-message handling, replay tooling, backlog metrics, and operator controls.
- Specify consistency semantics after tuple/model changes. Couple cache invalidation to committed versions and expose version data in decision traces.
- Add backup policy, point-in-time recovery objectives, restore automation, data export, retention, and deletion workflows.

### Exit criteria

- Failure-injection tests cover crash before commit, after commit/before publish, duplicate delivery, Redis loss, and database failover.
- Restore drill meets documented RPO/RTO on staging-sized data.
- Migrations are tested from the oldest supported rolling-upgrade version and on a restored production-like snapshot.
- Audit and outbox reconciliation reports show no unexplained gaps.

## Phase B4 — Observability, resilience, and SLOs

**Outcome:** operators can detect, explain, and mitigate degraded authorization behavior.

### Work packages

- Adopt OpenTelemetry for traces, metrics, and structured logs with consistent resource and correlation attributes.
- Define SLIs for check latency, availability, decision errors, stale model/cache events, graph budget exhaustion, outbox age, and dependency health.
- Establish initial SLOs from load-test evidence, not aspiration. Separate single check, batch check, explain, graph, and administration workloads.
- Enforce deadlines and cancellation through the entire call graph. Use bounded queues, bulkheads, retry budgets, and circuit breaking only where semantics are safe.
- Replace process-local-only metrics where multi-instance aggregation is required. Audit all labels for bounded cardinality and tenant-data leakage.
- Build dashboards, alerts, runbooks, and decision-trace lookup. Alerts name an owner and link to a mitigation.
- Run load, soak, spike, hot-tenant, deep-graph, and dependency-degradation tests.

### Exit criteria

- Staging meets agreed SLOs at representative data volume and concurrency.
- A responder can move from an alert to correlated request, decision, model version, and dependency evidence.
- Graceful shutdown drains requests and background work within a defined budget.
- Game-day scenarios have recorded outcomes and follow-up actions.

## Phase B5 — Release engineering and production operations

**Outcome:** releases are reproducible, promotable, reversible, and supportable.

### Work packages

- Produce minimal non-root container images, immutable version tags, health probes, resource limits, and read-only filesystem compatibility where practical.
- Add build provenance, signatures, SBOM, secret scanning, SAST, dependency policy, and container scanning.
- Separate build from environment configuration. Validate required configuration at startup without printing secrets.
- Define environment promotion, canary strategy, database/app compatibility, rollback, feature flags, and emergency disable controls.
- Create runbooks for elevated denial rate, latency, database saturation, Redis loss, outbox backlog, identity provider failure, bad model activation, and suspected tenant breach.
- Add disaster recovery and incident communication exercises.

### Exit criteria

- The same signed artifact is promoted through test, staging, and production.
- Canary and rollback exercises succeed without contract or data loss.
- Operations ownership, escalation, maintenance windows, and release evidence are documented.
- Production readiness review is approved against the definition in the assessment.

## Phase B6 — Scale and enterprise governance

**Outcome:** the service can support larger organizations and controlled ecosystem growth.

Candidate capabilities include regional topology, tenant placement, quotas, usage metering, SCIM or directory sync, enterprise SSO administration, immutable audit export, customer-managed keys, webhook delivery, policy-as-code workflows, SDK generation, and compatibility certification. Each capability requires a real customer scenario and must not weaken the B0–B5 invariants.

## Backend definition of done

A backend story is done only when code, tests, contracts, telemetry, security implications, migration/rollback behavior, and documentation change together. Authorization behavior changes additionally require golden-corpus updates and explain-trace review. No test may be made green by bypassing tenant or authorization enforcement.
