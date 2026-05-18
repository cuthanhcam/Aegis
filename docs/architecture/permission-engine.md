# Permission Engine - Aegis Authorization Core

## 1. Overview

This document defines the authorization evaluation model and execution flow for Aegis.

Aegis is designed as an authorization engine that supports:

- ReBAC (primary model)
- RBAC (fallback model)
- Tenant isolation
- Explainability for debugging

The current implementation target is **MVP ReBAC v0** (direct tuple evaluation), with a clear path to graph traversal.

---

## 2. Core Question

Can subject S perform relation R on object O?

Tuple format:

- subject: `<type>:<id>` (example: `user:1`, `team:dev`)
- relation: `viewer`, `editor`, `owner`
- object: `<type>:<id>` (example: `document:10`, `project:5`)

Canonical tuple:

- `(subject, relation, object)`

---

## 3. Authorization Models

### 3.1 ReBAC (Primary)

Model:

- `subject -> relation -> object`

Examples:

- `(user:1, owner, document:10)`
- `(team:dev, member, user:1)`
- `(team:dev, owner, repo:1)`

### 3.2 RBAC (Fallback)

Model:

- `user -> role -> permission`

Example:

- `user:1 -> role:admin -> permission:document:edit`

### 3.3 Hybrid Strategy

Decision principle:

1. Evaluate explicit deny policies first.
2. Evaluate ReBAC allow rules.
3. Evaluate RBAC allow rules.
4. Default deny.

Conflict rule:

- `deny` overrides `allow`
- ReBAC and RBAC are independent signals
- no forced mapping from relation to permission string

---

## 4. Scope and Maturity

### 4.1 MVP Scope (implemented first)

- Direct tuple check only
- No recursive traversal
- Deterministic, low-latency evaluation path

### 4.2 Planned Scope (next iterations)

- Recursive graph traversal
- Relation inheritance/composition
- Contextual conditions

---

## 5. Engine Interfaces

Use clear boundaries to keep the engine extensible:

- `ICheckEngine`: executes policy evaluation
- `IRelationshipStore`: reads/writes tuples
- `IRbacProvider`: checks role-based permissions
- `IExplainService`: returns evaluation trace

---

## 6. Evaluation Flow

High-level execution:

1. Parse and validate input tuple
2. Resolve tenant context
3. Evaluate deny rules
4. Evaluate ReBAC direct allow
5. Evaluate RBAC allow
6. Return final decision with reason code

Decision outputs:

- `ALLOW`
- `DENY_EXPLICIT`
- `DENY_NOT_FOUND`
- `DENY_INVALID_INPUT`

---

## 7. Pseudocode (MVP)

```csharp
public async Task<DecisionResult> CheckAsync(
    string tenantId,
    string subject,
    string relation,
    string obj)
{
    var input = TupleInput.Parse(subject, relation, obj);
    if (!input.IsValid)
        return DecisionResult.Deny("DENY_INVALID_INPUT");

    var rebacEffect = await _relationshipStore.GetEffectAsync(
        tenantId,
        input.Subject,
        input.Relation,
        input.Object);

    if (rebacEffect == RelationshipEffect.Deny)
        return DecisionResult.Deny("DENY_EXPLICIT");

    if (rebacEffect == RelationshipEffect.Allow)
        return DecisionResult.Allow("ALLOW_REBAC_DIRECT");

    var hasRbac = await _rbacProvider.HasPermissionAsync(
        tenantId,
        input.Subject,
        input.Object,
        input.Relation);

    if (hasRbac)
        return DecisionResult.Allow("ALLOW_RBAC_ROLE");

    return DecisionResult.Deny("DENY_NOT_FOUND");
}
```

---

## 8. Recursion Design (Planned)

When recursive relationship checks are enabled:

- Use bounded BFS/DFS
- Enforce `maxDepth` (default: `5`)
- Track visited nodes to avoid cycles
- Stop early on explicit deny

Pseudo rules:

- if `depth > maxDepth` then `DENY_NOT_FOUND`
- if node visited then skip

---

## 9. Caching Strategy

Recommended cache key:

- `tenant:{tenantId}:subject:{subject}:relation:{relation}:object:{object}:v:{version}`

Examples:

- `tenant:t1:subject:user:1:relation:viewer:object:document:10:v:42`

Guidelines:

- Include `tenantId` always
- Include policy/model version for safe invalidation
- Cache both allow and deny for short TTL

---

## 10. Explainability

Provide an explain mode for debugging and support.

Sample explanation steps:

1. `REBAC_EFFECT`: not found
2. `REBAC_DIRECT`: matched tuple `(user:1, owner, document:10)`
3. `FINAL`: `ALLOW`

This is required for incident analysis and policy troubleshooting.

---

## 11. Multi-Tenancy Enforcement

Hard requirements:

- Every check requires tenant context
- Every data query must filter by `TenantId`
- Cross-tenant tuple references are rejected

---

## 12. Summary

Aegis permission engine is positioned as:

- authorization-first
- ReBAC-primary with RBAC fallback
- explicit, deterministic conflict handling
- practical MVP now, graph-ready architecture next
