---
title: Deterministic authorization decisions
description: How snapshots, ordered evaluation, bounded graph work, caches, and traces produce repeatable Aegis decisions.
category: concepts
audience: [application-developer, backend-engineer, security-engineer]
status: published
last_updated: 2026-08-16
series: authorization-foundations
order: 2
---

# Deterministic authorization decisions

An authorization service earns trust when the same effective input produces the same answer and enough evidence exists to explain why. Determinism is therefore a system property, not merely a pure evaluator function.

## The effective input

```text
tenant + store + subject + relation + object
+ immutable authorization model version
+ relationship snapshot or consistency token
+ approved contextual attributes
+ engine semantics and bounded-work policy
```

If one value changes, a different result may be legitimate. If all are identical, evaluation order, caching, database ordering, concurrency, or instance choice must not change the answer.

## Example

```http
POST /stores/store-docs/check
Authorization: Bearer <token>
Content-Type: application/json

{
  "subject": "user:alice",
  "relation": "viewer",
  "object": "document:quarterly-plan"
}
```

If the active model says an editor is a viewer and the store contains `document:quarterly-plan#editor@user:alice`, Aegis can allow by resolving the `viewer` rewrite to `editor`. The decision identifies the model used; explain describes safe rewrite and tuple evidence without exposing unrelated graph data.

## Ordered evaluation

Aegis has distinct evaluators for deny policy, direct ReBAC, rewrites, context, and RBAC fallback. Their order is security behavior. An explicit deny cannot be bypassed because a later stage finds an allow. Changing precedence requires a compatibility decision, golden-test update, and release note.

Results should distinguish allow, completed deny, and indeterminate/error. Indeterminate work—invalid model, dependency failure, or exhausted budget—never becomes allow. Whether clients receive deny or an error is a contract choice, but enforcement remains fail closed.

## Snapshots, graphs, and budgets

Resolve “latest model” once and evaluate one immutable version. Publication changes an active pointer only after validation and assertions. Rollback activates a prior published version rather than editing history.

Graphs can contain cycles and adversarial fan-out. Track visited semantic work, enforce depth, node/tuple, context, deadline, and batch budgets, and expose truncation. A depth limit alone does not protect a single level with enormous breadth.

| Budget       | Protects against                 |
| ------------ | -------------------------------- |
| Depth        | Cyclic or deeply nested rewrites |
| Nodes/tuples | Broad groups and hot objects     |
| Context size | Request amplification            |
| Deadline     | Slow dependencies and total work |
| Batch size   | Capacity monopolization          |

## Cache correctness

A cache key represents every decision input: tenant, store, subject, relation, object, model version, and canonical permitted context. Relationship revision may also be required. Cache is optimization, never authority; hit and miss paths run the same golden corpus, and duplicate invalidation events remain safe.

## Explain safely

Explain can leak membership, graph shape, context, or policy. Apply the same scope authorization as check and filter detail by privilege. A useful trace answers: final decision, model version, ordered stages, safe evidence, bounded-work/cache involvement, and correlation ID.

## Testing strategy

Golden cases include direct allow/deny, explicit deny precedence, rewrite operations, context, cycles, graph limits, missing definitions, publication races, cache parity, cancellation, and cross-tenant attempts. Property tests cover declared set semantics; integration tests cover snapshots and invalidation; load tests prove bounded behavior.

## Verification checklist

- [ ] One immutable model version is used per decision.
- [ ] Stage order and precedence are documented and tested.
- [ ] Cycles, breadth, depth, deadlines, and batches are bounded.
- [ ] Cache identity covers every decision input and scope.
- [ ] Explain is authorized and redacted.
- [ ] The corpus covers cached/uncached and supported storage paths.

## Continue reading

Continue with [Tenant and store isolation](./tenant-store-isolation.md), then [API integration](../guides/api-integration.md).
