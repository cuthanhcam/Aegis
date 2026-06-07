# Testing Plan

This document defines the test strategy needed for Aegis to become a serious OSS authorization platform.

## Goals

Testing should prove:

- authorization decisions are deterministic;
- tenant isolation cannot be bypassed;
- ReBAC semantics are stable;
- RBAC and ABAC fallback behavior is explicit;
- explain responses are useful and stable;
- PostgreSQL migrations are safe;
- Redis caching does not return stale or cross-tenant decisions.

## Test Layers

### Unit Tests

Unit tests should cover:

- value object validation;
- tuple parsing and normalization;
- relationship aggregate invariants;
- authorization model parsing;
- rewrite expression parsing;
- ReBAC evaluator behavior;
- explicit deny precedence;
- RBAC fallback;
- ABAC condition evaluation;
- explanation tree construction;
- reason code mapping.

### Application Tests

Application tests should cover:

- use-case validation;
- tenant/store resolution;
- authorization model activation;
- relationship write/delete orchestration;
- audit writes;
- cancellation token propagation;
- error envelopes;
- idempotency handling.

### Integration Tests

Integration tests should use real PostgreSQL and Redis where possible.

Coverage:

- migrations apply from empty database;
- tuple writes and reads;
- check and explain endpoints;
- batch check endpoint;
- relationship change feed;
- RBAC assignment and fallback;
- ABAC-conditioned grants;
- cache invalidation on tuple write;
- cache invalidation on model activation;
- tenant isolation across all API groups.

### Contract Tests

Contract tests should lock down public API behavior:

- OpenAPI snapshot;
- DTO shape snapshots;
- error envelope snapshots;
- reason code compatibility;
- versioned route compatibility.

### Architecture Tests

Architecture tests should prevent dependency erosion:

- Domain does not reference Infrastructure or Api.
- Authorization does not reference ASP.NET Core, Npgsql, Redis, or Api.
- Application does not reference Api.
- Infrastructure implements Application/Authorization abstractions.
- Contracts do not depend on Domain entities.

### Property-Based Tests

Property-based tests are important for graph authorization.

Properties:

- tuple order does not affect decision;
- cycles never create infinite traversal;
- explicit deny always wins;
- tenant A data never affects tenant B;
- contextual tuples do not persist;
- equivalent model rewrites produce equivalent decisions;
- model version pinning is deterministic.

### Performance Tests

Performance tests should be separate from normal CI unless they are lightweight.

Benchmark cases:

- direct allow;
- direct deny;
- default deny;
- nested relation rewrite;
- high fanout group;
- batch check shared subject;
- batch check shared object;
- explain with full trace.

## CI Policy

Recommended CI stages:

```text
1. dotnet format --verify-no-changes
2. dotnet build
3. dotnet test unit/application/authorization
4. dotnet test integration with PostgreSQL and Redis
5. architecture tests
6. OpenAPI/contract snapshot check
```

Performance benchmarks should run:

- on release branches;
- before tagged releases;
- manually for performance PRs.

## Release Gate

Before an OSS release, require:

- all tests passing;
- migration test from empty database;
- migration test from previous release database;
- no known tenant isolation regressions;
- no undocumented public API change;
- benchmark baseline updated when engine behavior changes.

