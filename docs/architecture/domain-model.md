# Domain Model

This document defines the target domain model for Aegis. It is intentionally stricter than the current implementation so the backend can evolve toward a platform-grade design without losing the existing learning-project functionality.

## Core Concepts

| Concept             | Meaning                                                          |
| ------------------- | ---------------------------------------------------------------- |
| Tenant              | Hard isolation boundary for customers or environments.           |
| Store               | Authorization namespace owned by a tenant.                       |
| Subject             | Entity requesting access, such as `user:alice` or `team:eng`.    |
| Object              | Resource being accessed, such as `document:roadmap`.             |
| Relation            | Named relationship between subject and object, such as `viewer`. |
| Tuple               | ReBAC fact: `(subject, relation, object)`.                       |
| Authorization Model | Versioned schema and rewrite rules for a store.                  |
| Decision            | Deterministic result of evaluating one permission request.       |
| Explanation         | Structured proof showing why a decision was made.                |

## Aggregates

### Tenant

Tenant owns isolation, lifecycle, and store membership.

Fields:

- `TenantId`
- `Slug`
- `DisplayName`
- `Status`
- `CreatedAt`
- `UpdatedAt`

Invariants:

- tenant id is globally unique;
- tenant slug is unique;
- disabled tenants cannot serve authorization decisions.

### AuthorizationStore

Store is the namespace for model, graph, fallback policies, and audit data.

Fields:

- `StoreId`
- `TenantId`
- `Name`
- `CurrentAuthorizationModelId`
- `CreatedAt`
- `UpdatedAt`

Invariants:

- store belongs to exactly one tenant;
- store name is unique within a tenant;
- active model must belong to the same tenant and store.

### AuthorizationModel

Authorization model defines resource types, relations, and rewrite rules.

Fields:

- `AuthorizationModelId`
- `TenantId`
- `StoreId`
- `SchemaVersion`
- `Source`
- `ParsedDefinition`
- `Status`
- `Revision`
- `CreatedAt`
- `ActivatedAt`

Statuses:

- `Draft`
- `Validated`
- `Active`
- `Deprecated`

Invariants:

- only validated models can become active;
- only one active model per store;
- model activation creates a new store revision;
- model source and parsed definition must be semantically equivalent.

### RelationshipTuple

Relationship tuple is the primary ReBAC fact.

Fields:

- `TupleId`
- `TenantId`
- `StoreId`
- `SubjectType`
- `SubjectId`
- `SubjectRelation`
- `Relation`
- `ObjectType`
- `ObjectId`
- `Effect`
- `ConditionName`
- `ConditionContext`
- `Revision`
- `CreatedAt`
- `UpdatedAt`
- `ExpiresAt`

Effects:

- `Allow`
- `Deny`

Invariants:

- subject and object must be typed references;
- relation must exist in the active authorization model, unless validation is explicitly bypassed for compatibility;
- tuple uniqueness is scoped by tenant, store, subject, relation, object, and optional subject relation;
- explicit deny has decision precedence over allow.

### RelationshipChange

Relationship change provides a durable change feed for cache invalidation and watch APIs.

Fields:

- `ChangeId`
- `TenantId`
- `StoreId`
- `Revision`
- `Operation`
- `Tuple`
- `Actor`
- `CreatedAt`

Invariants:

- revisions are monotonic per store;
- change feed is append-only;
- clients page by revision cursor, not offset.

### RbacRole

Role is a fallback grouping for coarse-grained permissions.

Fields:

- `TenantId`
- `StoreId`
- `RoleName`
- `Description`
- `CreatedAt`
- `UpdatedAt`

Invariants:

- role name is unique inside tenant/store;
- role grants cannot cross tenant/store boundaries.

### RbacGrant

Grant connects role to a relation/object pattern.

Fields:

- `TenantId`
- `StoreId`
- `RoleName`
- `Relation`
- `ObjectPattern`
- `ConditionName`
- `CreatedAt`

Invariants:

- grant belongs to an existing role;
- grant can be conditional;
- grant is evaluated after ReBAC unless explicitly configured otherwise.

### AuditEvent

Audit event records operational history.

Fields:

- `AuditEventId`
- `TenantId`
- `StoreId`
- `Action`
- `Actor`
- `Subject`
- `Relation`
- `Object`
- `Decision`
- `ReasonCode`
- `ExplanationId`
- `Metadata`
- `CreatedAt`

Invariants:

- audit events are append-only;
- audit rows must not be mutated for normal correction flows;
- sensitive request context should be redacted or hashed when configured.

## Value Objects

| Value Object            | Responsibility                                              |
| ----------------------- | ----------------------------------------------------------- |
| `TenantId`              | Non-empty stable tenant identifier.                         |
| `StoreId`               | Non-empty store identifier scoped to a tenant.              |
| `TypedReference`        | Parses and validates `<type>:<id>`.                         |
| `SubjectReference`      | Subject-specific typed reference, optionally userset-aware. |
| `ObjectReference`       | Object-specific typed reference.                            |
| `RelationName`          | Relation identifier with syntax validation.                 |
| `AuthorizationRevision` | Monotonic revision number.                                  |
| `DecisionReasonCode`    | Stable public reason code.                                  |

## Decision Model

Decision result should include:

- `Allowed`
- `Decision`
- `ReasonCode`
- `TenantId`
- `StoreId`
- `AuthorizationModelId`
- `Revision`
- `Explanation`

The decision model is part of the public contract and should remain backward compatible inside an API version.
