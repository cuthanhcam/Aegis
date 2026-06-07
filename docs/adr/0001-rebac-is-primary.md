# ADR 0001: ReBAC Is the Primary Authorization Model

## Status

Accepted

## Context

Aegis supports ReBAC, RBAC, and ABAC. Without a clear priority, authorization semantics can become difficult to explain, test, and optimize.

## Decision

ReBAC is the primary authorization model.

RBAC and ABAC remain supported as fallback or secondary policy stages.

The default decision order is:

```text
explicit deny -> ReBAC allow -> RBAC fallback -> ABAC condition checks -> default deny
```

## Consequences

- ReBAC graph correctness has the highest priority.
- RBAC must not obscure ReBAC decision paths.
- Explainability must identify which stage produced the final decision.
- Performance work should optimize tuple lookup and graph traversal first.
