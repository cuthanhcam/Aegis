# Aegis current-state assessment

> Assessment type: repository-level engineering review
> Confidence: high for code organization and documented behavior; medium for runtime characteristics until load, failure, and recovery tests exist

## Executive assessment

Aegis is beyond a proof of concept. Its backend already expresses the important domain boundaries of an authorization system, and the repository contains meaningful unit and integration coverage. The main gap is not feature count; it is the absence of a consistently enforced production envelope around those features. Configuration, persistence, observability, security controls, API lifecycle, and operational evidence need to become first-class architecture.

The current console proves the workflows but should not be the foundation of the next product experience. Large route components, a global stylesheet, duplicate shared components, handwritten contracts, and an oversized API client make changes expensive and inconsistent. A replacement console should preserve useful domain knowledge while rebuilding the presentation and application layers around explicit feature boundaries.

## Evidence reviewed

- Six backend projects: API, Application, Authorization, Contracts, Domain, Infrastructure, plus SharedKernel.
- Unit and integration suites covering domain aggregates, authorization stages, caching, application services, tenant endpoints, PostgreSQL/Redis integration, and model lifecycle.
- API composition, middleware, JWT configuration, CORS, rate limiting, readiness/liveness, Prometheus output, migrations, seeding, outbox, in-memory and PostgreSQL stores.
- Frontend monorepo with React 19, Vite, Turborepo, TanStack Query, Ant Design, Monaco, shared types, UI, and API-client packages.
- Product flows for stores, models, relationships, assertions, checks/explain, graph exploration, audit, presets, access, and profile.
- Forge principles: registry-like discoverability, local state where appropriate, actionable validation, keyboard access, responsive behavior, theme completeness, narrow tests, and a single quality command.
- Lens of Charlie layout DNA: quiet hierarchy, strong typography, restrained color, bounded content widths, semantic tokens, progressive disclosure, accessibility, and responsive layouts that do not hide essential information.
- learn-dotnet documentation pattern: ordered learning paths, article-sized chapters, prerequisites, mental models, runnable examples, trade-offs, code maps, completion criteria, and explicit next reading.

## Backend findings

### What is already strong

| Area | Existing signal | Why it matters |
| --- | --- | --- |
| Domain boundaries | Dedicated Domain, Authorization, Application, Infrastructure, Contracts, and API assemblies | Gives the system a credible base for dependency rules and independent testing |
| Decision model | Staged evaluators for deny, ReBAC, rewrite, context, and RBAC fallback | Makes decision order inspectable and testable |
| Isolation intent | Tenant middleware, access guard, store-scoped APIs | Establishes tenant/store scoping as a product invariant |
| Persistence | PostgreSQL implementations and embedded SQL migrations | Provides a durable path beyond development-only storage |
| Resilience primitives | Cache abstraction, outbox processing, cancellation tests | Useful foundations for predictable failure behavior |
| Operability | Liveness, readiness, request logging, metrics endpoint | Provides basic hooks for a production control plane |
| Verification | Broad unit tests plus focused integration tests | Reduces risk during hardening and contract changes |

### Product-readiness gaps

1. **Runtime policy is concentrated in composition code.** `Program.cs` owns validation, CORS, rate limiting, authentication, Swagger, health checks, and pipeline order. Extracting cohesive hosting modules will make policy review and testing easier.
2. **Package baselines are inconsistent.** Projects target .NET 8 while several Microsoft.Extensions packages are on 9/10 major versions. Even when technically resolvable, this makes compatibility and servicing intent unclear. Central package management and an explicit support policy are needed.
3. **Authentication is development-shaped.** Symmetric JWT secrets and configured demo users are useful locally but are not an enterprise identity plane. Production needs external OIDC/JWKS validation, key rotation, workload identities or scoped API credentials, session revocation semantics, and audited administration.
4. **Tenant isolation needs defense in depth.** Middleware checks are necessary but should be supplemented with tenant-aware repository contracts, database constraints or row-level security where appropriate, cache-key proofs, negative cross-tenant tests, and security review gates.
5. **API lifecycle is implicit.** A large surface exists, including compatibility routes, but versioning, idempotency, pagination/cursor stability, concurrency tokens, deprecation, generated OpenAPI, and consumer contract tests are not yet a governed system.
6. **Operational endpoints are foundations, not an observability program.** Metrics need bounded cardinality and service-level indicators; traces need decision correlation; logs need redaction policy; alerts and dashboards need ownership and runbooks.
7. **Persistence evolution needs stronger safety.** One migration stream, startup initialization, and seed behavior should mature into forward-only migration policy, compatibility windows, locking, backup/restore drills, and zero-downtime rollout tests.
8. **Scale limits are undocumented.** Maximum graph depth exists, but budgets for tuple count, batch size, request payload, fan-out, query duration, cache invalidation, and hot tenants need explicit policies and measurements.
9. **Supply-chain and release controls are not demonstrated.** Add dependency scanning, secret scanning, SBOM/provenance, container hardening, signed artifacts, release promotion, and rollback evidence.

## Frontend findings

### Useful assets to retain

- The domain route inventory is comprehensive and maps well to user jobs.
- Feature folders already provide an initial bounded-context vocabulary.
- TanStack Query, lazy routes, Monaco, workspace packages, and typed domain models are suitable building blocks.
- Existing screens and flows are valuable as executable discovery references and parity fixtures.

### Why a rewrite is justified

| Signal | Observed shape | Consequence |
| --- | --- | --- |
| Styling concentration | `app/styles.css` is roughly 2,000 lines | Token, component, layout, and page behavior are coupled |
| Route complexity | Test Console is roughly 1,100 lines; Models and MainLayout exceed 680 lines | State, orchestration, rendering, and error behavior are hard to reason about |
| Client concentration | Shared API client exceeds 600 lines | Endpoint ownership and contract evolution are unclear |
| Duplicate primitives | Parallel `JsonEditor`, `JsonDiffView`, `ProtectedRoute`, `AccessGate`, `EmptyState`, and skeleton paths | Consumers can select inconsistent implementations |
| UI dependency dominance | Ant Design configuration and components shape the experience | Aegis lacks its own coherent interaction and visual language |
| Missing quality loop | Workspace has no unified test command and no visible component/e2e/accessibility gates | Regressions can pass typecheck/build |

The rewrite should be a new application shell and design system, with old routes used for comparison until replacement slices are accepted. A big-bang deletion would discard domain learning and increase risk.

## Product DNA to carry forward

From Forge, carry forward fast feedback, strong validation, keyboard-first workflows, one coherent workspace, explicit registries, and a `check` command that proves the product. Adapt “local-first” carefully: authorization data is server-owned, but drafts, filters, layout preference, and safe editor state may remain local until explicitly submitted.

From Lens of Charlie, carry forward restraint rather than imitation: a neutral canvas, deliberate typography, bounded line lengths, spacious hierarchy, semantic tokens, visible focus, reduced-motion support, and layouts that reveal complexity progressively. Aegis remains an operations console, so density must be adjustable and tables/graphs must optimize scanning rather than resemble editorial pages.

From learn-dotnet, carry forward documentation as a guided knowledge system: every article states audience, prerequisites, mental model, working examples, failure modes, operational implications, and completion criteria.

## Target architecture decisions

The following decisions should be captured as ADRs during Phase 0:

1. Supported .NET version and package alignment policy.
2. OIDC provider boundary and machine-to-machine credential model.
3. Tenant isolation enforcement strategy from HTTP through database and cache.
4. API versioning, compatibility, idempotency, and pagination standards.
5. Authorization model lifecycle: draft, validate, test, publish, activate, rollback.
6. Consistency contract for relationship writes and decision caches.
7. Observability schema for request, tenant-safe correlation, decision, model, and trace identifiers.
8. Frontend framework, route/data conventions, generated-client pipeline, and design-system ownership.
9. Deployment topology and migration ownership.
10. Data retention, audit immutability, export, and deletion policy.

## Risk register

| Risk | Impact | Early control |
| --- | --- | --- |
| Cross-tenant access through repository/cache mismatch | Critical | Isolation test matrix, tenant-aware interfaces, cache namespace tests |
| Incorrect decision after model or tuple mutation | Critical | Versioned snapshots, invalidation contract, deterministic replay tests |
| Console rewrite diverges from API | High | Generated client, schema checks, contract fixtures, route parity ledger |
| Big-bang migration delays user value | High | Vertical slices behind a replacement shell and explicit cutover gates |
| Audit records leak sensitive context | High | Allowlisted schema, redaction tests, access controls, retention policy |
| Database migration blocks or corrupts rollout | High | Expand/contract rules, migration lock, staging restore rehearsal |
| Graph query causes unbounded work | High | Budgets, cancellation, depth/fan-out caps, load and adversarial tests |
| Visual polish masks inaccessible workflows | Medium | Automated and manual accessibility gates per slice |

## Definition of product-ready

Aegis is product-ready when a release candidate can demonstrate all of the following:

- deterministic decisions and explain traces across supported evaluation paths;
- automated proof that one tenant cannot observe or mutate another tenant's data;
- versioned, documented contracts with compatibility tests and generated clients;
- safe identity integration, secret/key rotation, least privilege, and administrative audit;
- measured SLOs under representative load and controlled dependency failure;
- backup restoration, migration, rollback, and incident runbooks exercised in a staging environment;
- a keyboard-accessible, responsive console with consistent loading, empty, error, stale, and destructive-action states;
- signed, scanned, reproducible release artifacts promoted through environments;
- documentation that supports evaluation, integration, operation, troubleshooting, and incident response.
