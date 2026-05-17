# Architecture Overview Aegis Authorization Platform

---

## System Context

```
+---------------------------------------------------------------+
| Client Applications                                           |
| Web App | Mobile App | Backend Service | Admin Portal         |
+---------------------------------------------------------------+
                               |
                               | HTTPS + JWT
                               v
+---------------------------------------------------------------+
| Aegis Authorization API (Presentation Layer)                  |
| Endpoints: /check /explain /relationships /roles              |
|            /permissions /users /audit-logs                    |
+---------------------------------------------------------------+
                               |
                               v
+---------------------------------------------------------------+
| Application Layer (Use Cases / Handlers)                      |
| CheckAuthorizationHandler | ExplainAuthorizationHandler       |
| CreateRelationshipHandler | AssignRoleToUserHandler           |
| Domain Event Processing                                       |
+---------------------------------------------------------------+
                               |
                               v
+---------------------------------------------------------------+
| Authorization Engine (Deterministic, No HTTP/EF dependency)   |
| ReBAC Evaluator | RBAC Evaluator | Decision Resolver          |
| Explain Service                                               |
+---------------------------------------------------------------+
                               |
                               v
+---------------------------------------------------------------+
| Domain Model (DDD)                                            |
| Relationship | Store | AuthorizationModel | Domain Events     |
| Value Objects: SubjectId, RelationName, ObjectId              |
+---------------------------------------------------------------+
                               |
                               v
+---------------------------------------------------------------+
| Infrastructure Layer (Persistence / Adapters)                 |
| DbContext | Repositories | RelationshipStore | RBACProvider   |
| Migrations | Audit Log Store                                  |
+---------------------------------------------------------------+
                               |
                               v
+-------------------------------+    +--------------------------+
| PostgreSQL Database           |    | Redis Cache (Optional)   |
| relationships, users, roles,  |<-->| relationship lookups     |
| permissions, models, logs     |    | acceleration             |
+-------------------------------+    +--------------------------+

```

---

## Module Dependency Graph

**Strict dependency order (no cycles):**

```
SharedKernel

Contracts

Domain

Authorization (depends ONLY on Domain)

Application

Infrastructure

Api
```

**Key Rules:**

- Authorization NEVER depends on EF, HTTP, or Application layers
- Domain NEVER depends on Infrastructure or Api
- Application NEVER depends on Infrastructure or Api
- Early layers NEVER reference later layers (no backward dependencies)

## Architecture Summary Tables

### Layer Responsibilities

| Layer          | Main Responsibility                                                    | Must Not Depend On               |
| -------------- | ---------------------------------------------------------------------- | -------------------------------- |
| SharedKernel   | Common primitives, base abstractions, cross-cutting utilities          | Infrastructure, Api              |
| Contracts      | Request/response contracts and integration DTOs                        | Infrastructure, Api              |
| Domain         | Core business rules, aggregates, value objects, domain events          | Infrastructure, Api, Application |
| Authorization  | Deterministic policy evaluation (ReBAC + RBAC resolution)              | Infrastructure, Api              |
| Application    | Use-case orchestration, command/query handlers, transaction boundaries | Api, Infrastructure              |
| Infrastructure | Persistence, external adapters, repository implementations             | Api                              |
| Api            | HTTP endpoints, middleware, auth integration, serialization            | (entry layer)                    |

### Core Aggregates At A Glance

| Aggregate          | Purpose                           | Key Fields                                  | Domain Events                                                              |
| ------------------ | --------------------------------- | ------------------------------------------- | -------------------------------------------------------------------------- |
| Relationship       | ReBAC tuple with effect control   | TenantId, Subject, Relation, Object, Effect | RelationshipUpsertedDomainEvent, RelationshipDeletedDomainEvent            |
| Store              | Authorization context boundary    | Id, Name, CreatedAt, UpdatedAt              | StoreCreatedDomainEvent, StoreDeletedDomainEvent                           |
| AuthorizationModel | Schema/model definition per store | StoreId, SchemaVersion, Model               | AuthorizationModelCreatedDomainEvent, AuthorizationModelUpdatedDomainEvent |
| User               | RBAC principal identity           | TenantId, Username, Email, Roles            | (implementation-defined)                                                   |
| Role               | RBAC permission grouping          | TenantId, Name, Permissions                 | (implementation-defined)                                                   |

### Primary Use Cases

| Use Case            | Input                               | Engine Path                                                | Output                                         |
| ------------------- | ----------------------------------- | ---------------------------------------------------------- | ---------------------------------------------- |
| Check Permission    | tenantId, subject, relation, object | DENY check -> ReBAC allow -> RBAC fallback -> default deny | DecisionResult (allowed, decision, reasonCode) |
| Explain Permission  | tenantId, subject, relation, object | Executes all evaluation steps with trace capture           | ExplainResult (allowed, trace[])               |
| Create Relationship | CreateRelationshipCommand           | Validate ValueObjects -> persist tuple -> dispatch event   | 201 Created + relationship payload             |

### Persistence Boundaries

| Concern                             | Primary Store | Notes                                       |
| ----------------------------------- | ------------- | ------------------------------------------- |
| Relationship tuples                 | PostgreSQL    | Composite indexes for check/list operations |
| RBAC identities and grants          | PostgreSQL    | Users, roles, permissions, junction tables  |
| Authorization schema models         | PostgreSQL    | Versioned by store                          |
| Audit trail                         | PostgreSQL    | Append-only compliance logging              |
| Read/lookup acceleration (optional) | Redis         | Cache layer for hot authorization paths     |

---

## Core Aggregates

### Aggregate: Relationship

Represents a single tuple in the authorization system.

```csharp
public sealed class Relationship : AggregateRoot<Guid>
{
    public string TenantId { get; }           // Multi-tenant isolation
    public SubjectId Subject { get; }         // user:alice, team:dev
    public RelationName Relation { get; }     // owner, editor, viewer
    public ObjectId Object { get; }           // document:x, repo:y
    public RelationshipPermissionEffect Effect { get; }  // allow / deny
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }
}
```

**Domain Events:**

- `RelationshipUpsertedDomainEvent`
- `RelationshipDeletedDomainEvent`

---

### Aggregate: Store

Represents an authorization context (per app, per environment).

```csharp
public sealed class Store : AggregateRoot<string>
{
    public string Name { get; }           // "payment-service-store"
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }
}
```

**Domain Events:**

- `StoreCreatedDomainEvent`
- `StoreDeletedDomainEvent`

---

### Aggregate: AuthorizationModel

Schema/configuration defining what relations and types are valid for a store.

```csharp
public sealed class AuthorizationModel : AggregateRoot<string>
{
    public string StoreId { get; }
    public string SchemaVersion { get; }  // "1.0.0"
    public string Model { get; }          // JSON: relations, types
    public DateTimeOffset CreatedAt { get; }
}
```

**Domain Events:**

- `AuthorizationModelCreatedDomainEvent`
- `AuthorizationModelUpdatedDomainEvent`

---

### Aggregate: User (RBAC)

User identity in the system.

```csharp
public sealed class User : AggregateRoot<Guid>
{
    public string TenantId { get; }
    public string Username { get; }
    public string Email { get; }
    public string PasswordHash { get; }
    public IReadOnlyCollection<Role> Roles { get; }
}
```

---

### Aggregate: Role (RBAC)

Collection of permissions (RBAC fallback).

```csharp
public sealed class Role : AggregateRoot<Guid>
{
    public string TenantId { get; }
    public string Name { get; }           // "document-editor"
    public IReadOnlyCollection<Permission> Permissions { get; }
}
```

---

## Use Case Flow

### Use Case: Check Permission

```
Client

   [Api/CheckController]

        [Application/CheckAuthorizationHandler]

             Parse & validate input

             [Authorization/CheckEngine]

                 1. Check for explicit DENY
                       [RelationshipStore]
                          PostgreSQL query: find (S, R, O) with effect=DENY

                 2. Check ReBAC allow
                       [RelationshipStore]
                          PostgreSQL query: find (S, R, O) with effect=ALLOW

                 3. Check RBAC allow
                       [RBACProvider]
                          PostgreSQL query: user roles & permissions

                 4. Return decision with reason code

             Create audit log entry
                   [AuditLogStore]
                      Save to PostgreSQL

             Return DecisionResult to client
                  {
                    "allowed": true,
                    "decision": "ALLOW",
                    "reasonCode": "ALLOW_REBAC_DIRECT"
                  }
```

---

### Use Case: Explain Permission

```
Client

   [Api/ExplainController]

        [Application/ExplainAuthorizationHandler]

             [Authorization/ExplainService]

                 Execute each evaluation step

                 1. VALIDATE_INPUT       Check format
                 2. CHECK_DENY_POLICY    Query (S, R, O, effect=DENY)
                 3. CHECK_REBAC_ALLOW    Query (S, R, O, effect=ALLOW)
                 4. CHECK_RBAC_ALLOW     Query roles & permissions
                 5. FINAL_DECISION       Merge signals

                 Return DecisionTrace with detailed steps

             Return ExplainResult to client
                  {
                    "allowed": false,
                    "trace": [
                      { "step": "CHECK_DENY", "result": "NOT_MATCHED" },
                      { "step": "CHECK_REBAC", "result": "NOT_MATCHED", ... },
                      { "step": "CHECK_RBAC", "result": "MATCHED", ... },
                      { "step": "FINAL", "result": "DENY" }
                    ]
                  }
```

---

### Use Case: Create Relationship

```
Client

   [Api/RelationshipsController]

        [Application/CreateRelationshipHandler]

             Parse CreateRelationshipCommand from request

             [Domain/Relationship]

                 Relationship.Create(...)
                      Validate subject format (SubjectId.TryCreate)
                      Validate relation format (RelationName.TryCreate)
                      Validate object format (ObjectId.TryCreate)
                      Raise RelationshipUpsertedDomainEvent

             [Infrastructure/RelationshipRepository]

                 Save to PostgreSQL
                     INSERT INTO relationships (...) VALUES (...)

             Process domain event

                 [DomainEventDispatcher]
                      Create AuditLogEntry & save

             Return 201 Created with relationship details
```

---

## Database Schema

### Key Tables

| Table               | Purpose                              | Key Columns                                                                            | Constraints / Indexes                                               |
| ------------------- | ------------------------------------ | -------------------------------------------------------------------------------------- | ------------------------------------------------------------------- |
| Tenants             | Tenant registry                      | Id (UUID), Name, Status, CreatedAt, UpdatedAt                                          | PK(Id)                                                              |
| Stores              | Authorization context per tenant/app | Id (string), TenantId, Name, CreatedAt, UpdatedAt                                      | FK(TenantId -> Tenants), PK(Id)                                     |
| Relationships       | Primary ReBAC tuple store            | Id, TenantId, Subject, Relation, Object, Effect, CreatedAt, UpdatedAt                  | UNIQUE(TenantId, Subject, Relation, Object), composite lookup index |
| AuthorizationModels | Model/schema definition per store    | Id, StoreId, SchemaVersion, Model(JSON), CreatedAt                                     | FK(StoreId -> Stores), PK(Id)                                       |
| Users               | RBAC principal identities            | Id, TenantId, Username, Email, PasswordHash, Status                                    | FK(TenantId -> Tenants), PK(Id)                                     |
| Roles               | RBAC role catalog                    | Id, TenantId, Name, Description, CreatedAt, UpdatedAt                                  | FK(TenantId -> Tenants), PK(Id)                                     |
| Permissions         | RBAC permission catalog              | Id, Name, Scope, CreatedAt, UpdatedAt                                                  | PK(Id), unique(Name, Scope) recommended                             |
| UserRoles           | User-role mapping                    | TenantId, UserId, RoleId                                                               | PK(TenantId, UserId, RoleId), FK to Users/Roles                     |
| RolePermissions     | Role-permission mapping              | TenantId, RoleId, PermissionId                                                         | PK(TenantId, RoleId, PermissionId), FK to Roles/Permissions         |
| AuditLogs           | Compliance and forensic trail        | Id, TenantId, Timestamp, Action, Subject, Relation, Object, InitiatedBy, Details(JSON) | PK(Id), index(TenantId, Timestamp DESC) recommended                 |

---

## Decision Flow Algorithm (Pseudocode)

```csharp
public async Task<DecisionResult> CheckAsync(
    string tenantId,
    string subject,
    string relation,
    string obj)
{
    // Step 1: Validate input
    var input = TupleInput.Parse(subject, relation, obj);
    if (!input.IsValid)
        return DecisionResult.Deny("DENY_INVALID_INPUT");

    // Step 2: Check for explicit DENY (highest priority)
    var denyTuple = await _relationshipStore.GetAsync(
        tenantId,
        input.Subject,
        input.Relation,
        input.Object,
        effect: RelationshipEffect.Deny);

    if (denyTuple != null)
        return DecisionResult.Deny("DENY_EXPLICIT");

    // Step 3: Check ReBAC allow (direct match)
    var rebacTuple = await _relationshipStore.GetAsync(
        tenantId,
        input.Subject,
        input.Relation,
        input.Object,
        effect: RelationshipEffect.Allow);

    if (rebacTuple != null)
        return DecisionResult.Allow("ALLOW_REBAC_DIRECT");

    // Step 4: Check RBAC (fallback)
    var hasRbac = await _rbacProvider.HasPermissionAsync(
        tenantId,
        input.Subject,
        input.Object);

    if (hasRbac)
        return DecisionResult.Allow("ALLOW_RBAC_ROLE");

    // Step 5: Default deny (principle of least privilege)
    return DecisionResult.Deny("DENY_NOT_FOUND");
}
```

---

## Properties & Guarantees

### Determinism

Every permission check with identical input + state = identical output.

- No randomness
- No ordering effects
- No race conditions (MVCC in PostgreSQL)

### Consistency

Authorization decision never violates the explicit deny rule.

```
if explicit_deny_found()  return DENY
else if allow_found()  return ALLOW
else  return DENY (default)
```

### Isolation

Every check is tenant-scoped; no tenant ever sees another tenant's data.

```sql
WHERE tenant_id = @tenantId
```

### Auditability

Every change is logged with timestamp, actor, and details.

```sql
INSERT INTO audit_logs (id, tenant_id, timestamp, action, subject, relation, object, initiated_by)
VALUES (...)
```

---

## Performance Characteristics

| Operation           | Complexity | Index Support                                            |
| ------------------- | ---------- | -------------------------------------------------------- |
| Check permission    | O(1\*)     | Composite index: (tenant_id, subject, relation, object)  |
| Explain decision    | O(1)       | Same index as check                                      |
| Create relationship | O(1)       | INSERT + audit log                                       |
| Delete relationship | O(1)       | DELETE by PK                                             |
| List relationships  | O(n)       | Filtered indexes for (tenant, subject), (tenant, object) |
| RBAC lookup         | O(m)       | Foreign key joins, indexed                               |

\*O(1) refers to database queries (single index lookup), not overall system latency.

---

## Security Model

### Tenant Isolation

```
Every query: WHERE tenant_id = @tenantId
                AND <other filters>
```

No tenant can see another tenant's data **by design**.

### Explicit Deny Precedence

```
DENY > ALLOW (always, regardless of source)
```

Follows principle of least privilege.

### Audit Trail

```
Every permission change -> captured in audit_logs
Every permission check -> optionally logged (configurable)
Full forensic trail for compliance
```

### JWT Authentication (Optional)

```
Authorization: Bearer <JWT>
 Sub (subject claim)
 Aud (audience claim)
 Iss (issuer claim)
 Exp (expiration)
```

---

## Extensibility Points

### Add New Relation Type

1. Update `AuthorizationModel` schema
2. Add validation to `RelationName` ValueObject
3. Test in integration tests
4. Deploy new schema version

### Add Custom Condition Evaluation (Future)

```csharp
// Example: time-based access (deprecated after X date)
interface IConditionEvaluator
{
    Task<bool> EvaluateAsync(Relationship rel, EvaluationContext ctx);
}
```

### Add Graph Traversal (Next Phase)

```csharp
// Example: transitive relationships
// (user:bob, member, team:eng) AND (team:eng, owner, repo:code)
//  implies user:bob can access repo:code
interface IGraphTraversal
{
    Task<bool> CanReachAsync(SubjectId from, ObjectId to, Relation via);
}
```

---

## Testing Strategy

### Unit Tests (Aegis.UnitTests)

- Domain entity behavior (Relationship.Create validations)
- ValueObject rules (SubjectId format)
- Authorization logic (ReBAC vs RBAC decisions)
- Use case orchestration (handler contracts)

### Integration Tests (Aegis.IntegrationTests)

- Full HTTP API endpoint tests
- Database persistence
- Multi-tenant isolation
- Audit logging
- End-to-end scenarios

---

## Deployment Topology

### Development

```
Localhost
- Aegis.Api (dotnet run)
- PostgreSQL (local or Docker)
```

### Staging/Production

```
Kubernetes Cluster
 Aegis Pod (replicated, stateless)
 Aegis Pod                             Load Balancer (HTTP/HTTPS)
 Aegis Pod

 PostgreSQL Primary  PostgreSQL Replica(s) (read-only)
                            (optional for scaling)

 Redis Cache (optional, for relationship lookup caching)
```

---

## Conclusion

Aegis is designed with **clear separation of concerns**, **deterministic behavior**, **strict tenant isolation**, and **comprehensive auditability**. These properties make it suitable for mission-critical authorization in multi-tenant systems.

For more details:

- [Product Overview](../product/product-overview.md)
- [API Reference](../reference/api-reference.md)
- [Core Concepts](../concepts/core-concepts-tuple-model.md)
