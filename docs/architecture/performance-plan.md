# Performance Plan

Aegis authorization checks must be predictable under graph traversal, tenant isolation, and fallback policy evaluation. This document defines target performance behavior and the engineering work needed to reach it.

## Initial Targets

These are engineering targets, not current guarantees.

| Operation | Target |
| --- | --- |
| Direct check, cache hot | p50 less than 5 ms |
| Direct check, PostgreSQL | p95 less than 25 ms |
| ReBAC rewrite traversal | p95 less than 50 ms for bounded normal graphs |
| Batch check | fewer database round trips than item count |
| Explain | acceptable overhead, optimized for debugging not hot path |

## Core Principle

Authorization performance is dominated by:

- tuple index quality;
- graph fanout;
- traversal depth;
- model parsing/compilation;
- cache invalidation correctness.

Do not hide poor graph query behavior behind broad TTL caches.

## Engine Plan

### Compiled Models

Authorization models should be parsed and compiled into immutable rule graphs.

Cache key:

```text
tenant:{tenantId}:store:{storeId}:model:{modelId}
```

Compiled model cache should be invalidated on model activation.

### Traversal Budgets

Each check should enforce:

- max depth;
- max visited nodes;
- max relation fanout;
- timeout;
- cycle detection.

Budget failures should return deterministic deny with reason codes such as:

```text
DENY_MAX_DEPTH_REACHED
DENY_TRAVERSAL_BUDGET_EXCEEDED
DENY_CYCLE_DETECTED
```

### Request-Scoped Memoization

Memoize intermediate sub-checks inside a single decision request.

Memo key should include:

- tenant id;
- store id;
- subject;
- relation;
- object;
- model id;
- relevant context hash;
- contextual tuple hash.

### Batch Query Planning

Batch check should group tuple lookups by query shape.

Avoid:

```text
N checks -> N independent recursive query trees
```

Prefer:

```text
N checks -> grouped direct lookups -> grouped reverse lookups -> shared memo
```

## PostgreSQL Plan

Use typed tuple columns and indexes for:

- direct check;
- reverse object-to-subject lookup;
- subject-to-object expansion;
- change feed by revision.

Use cursor pagination for large relationship queries.

Use `EXPLAIN ANALYZE` benchmarks for representative graph shapes before claiming performance characteristics.

## Redis Plan

Redis may cache:

- compiled models;
- hot tuple lookup results;
- decision results;
- tenant/store revision checkpoints;
- idempotency key responses.

Invalidation must be revision-aware.

Recommended cache key pattern:

```text
aegis:v1:tenant:{tenantId}:store:{storeId}:rev:{revision}:check:{hash}
```

When relationship revision changes, old revision-scoped keys naturally become stale.

## Consistency Modes

Target API consistency options:

| Mode | Meaning |
| --- | --- |
| `minimize_latency` | Can use cache for current known revision. |
| `higher_consistency` | Prefer source-of-truth reads when cache is stale. |
| `at_least_revision` | Decision must observe at least a requested revision. |

## Metrics

Emit metrics for:

- decision latency;
- explanation latency;
- DB query count per decision;
- tuple rows returned per decision;
- traversal depth;
- visited node count;
- model cache hits/misses;
- tuple cache hits/misses;
- decision cache hits/misses;
- denies by reason code;
- check volume by tenant/store.

## Benchmark Scenarios

Add performance tests for:

- direct tuple allow;
- explicit deny;
- missing tuple default deny;
- nested folder/document inheritance;
- group membership;
- high-fanout groups;
- cyclic graph;
- batch check with shared subjects;
- batch check with shared objects;
- explain with rewrite path.

