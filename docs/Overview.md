# Aegis Overview

**Aegis** is a **centralized authorization platform** that provides deterministic permission decisions for modern, multi-tenant systems.

## Quick Definition

Aegis answers this question:

> Can `subject` perform `relation` on `object` within this `tenant`?

Example:

```
Can user:alice edit document:report within tenant:acme?
 Aegis returns: { "allowed": true, "reasonCode": "ALLOW_REBAC_DIRECT" }
```

---

## Why Aegis?

### The Problem

Traditional monolithic systems scatter authorization logic throughout code:

```
App A  embedded auth logic
App B  different auth logic
App C  yet another auth logic

Result: Inconsistent decisions, hard to audit, maintenance nightmare
```

### The Aegis Solution

Centralized, decoupled authorization engine:

```
App A
App B  Aegis (single source of truth)
App C
```

**Benefits:**

- **Consistency** Same decisions across all apps
- **Auditability** Complete trail of who has access
- **Debuggability** `/explain` API shows why decisions were made
- **Scalability** Evolves with your system
- **Flexibility** ReBAC + RBAC hybrid model

---

## Key Features

### 1. Permission Checks

**Real-time permission evaluation:**

```http
POST /api/v1/check
{
  "subject": "user:alice",
  "relation": "editor",
  "object": "document:report"
}

Response:
{
  "allowed": true,
  "decision": "ALLOW",
  "reasonCode": "ALLOW_REBAC_DIRECT"
}
```

### 2. Debugging with Explain API

**Understand why a permission was granted or denied:**

```http
POST /api/v1/explain

Response:
{
  "allowed": false,
  "trace": [
    { "step": "CHECK_DENY", "result": "NOT_MATCHED" },
    { "step": "CHECK_REBAC", "result": "NOT_MATCHED" },
    { "step": "CHECK_RBAC", "result": "NOT_MATCHED" },
    { "step": "FINAL", "result": "DENY_NOT_FOUND" }
  ]
}
```

### 3. Relationship Management (ReBAC Tuples)

**Define fine-grained relationships:**

```http
POST /api/v1/relationships
{
  "subject": "user:alice",
  "relation": "owner",
  "object": "document:report",
  "effect": "allow"
}
```

### 4. Role-Based Access (RBAC Fallback)

**Define role-based permissions:**

```http
POST /api/v1/roles
{ "name": "document-editor", "description": "Can edit documents" }

POST /api/v1/roles/{roleId}/permissions/{permissionId}
```

### 5. Multi-Tenancy

**Tenant isolation built-in:**

```http
X-Tenant-Id: tenant-acme

All data is scoped by tenant
 Tenant A never sees Tenant B's relationships
```

### 6. Audit Logs

**Complete compliance trail:**

```http
GET /api/v1/audit-logs

Response:
[
  {
    "timestamp": "2026-04-07T10:30:00Z",
    "action": "RELATIONSHIP_CREATED",
    "subject": "user:alice",
    "relation": "editor",
    "object": "document:report",
    "initiatedBy": "admin:system"
  },
  ...
]
```

---

## Core Concepts at a Glance

### The Tuple Model

**Canonical tuple:** `(subject, relation, object)`

```
(user:alice, owner, document:report)
  What: Alice is the owner of the report

(team:engineering, member, user:bob)
  What: Bob is a member of the engineering team

(team:product, own, codebase:roadmap)
  What: Product team owns the roadmap codebase
```

### ReBAC (Primary Model)

_Relationship-Based Access Control_

Fine-grained permissions via relationships:

```
User A shares a document (creates relationship)
 System checks relationship for permission
 Deterministic decision (allow or deny)
```

### RBAC (Fallback Model)

_Role-Based Access Control_

Coarse-grained permissions via roles:

```
User has role:admin
 Admin role has permission:document:delete
 Therefore user can delete documents
```

### Hybrid Evaluation

Aegis checks both:

```
1. Is there an explicit DENY?  DENY (wins)
2. Is there a ReBAC ALLOW?  ALLOW
3. Is there an RBAC ALLOW?  ALLOW
4. Otherwise  DENY (default)
```

---

## Architecture at a Glance

```

 HTTP API Layer (Controllers)
 (/check, /explain, /relationships)



 Application Layer (Use Cases)
 (command handling, orchestration)



 Authorization Engine
 (ReBAC + RBAC evaluation, NO HTTP/EF)



 Domain Model (DDD)
 (Relationship, Store, User entities)



 Infrastructure & Persistence
 (EF Core, PostgreSQL, audit logs)

```

---

## Data Model

### Tenants

Isolation boundary for multi-tenancy:

```sql
Tenants
 Id (UUID)
 Name
 Status (active, archived)
```

### Stores

Authorization contexts (per app, per environment):

```sql
Stores
 Id (string, ULID-like)
 Name (e.g., "document-service")
 TenantId (FK)
 CreatedAt / UpdatedAt
```

### Relationships (Core)

The permission tuples:

```sql
Relationships
 Id (UUID)
 TenantId (FK)
 Subject (string: "user:1", "team:dev")
 Relation (string: "owner", "editor", "viewer")
 Object (string: "document:10", "repo:code")
 Effect (enum: allow, deny)
 CreatedAt / UpdatedAt

UNIQUE(TenantId, Subject, Relation, Object)
```

### Users & Roles (RBAC)

For role-based fallback:

```sql
Users
 Id (UUID)
 TenantId (FK)
 Username
 Email
 PasswordHash

Roles
 Id (UUID)
 TenantId (FK)
 Name (e.g., "document-editor")
 Permissions (many-to-many join table)
```

### Audit Logs

Complete trail for compliance:

```sql
AuditLogs
 Id (UUID)
 TenantId (FK)
 Timestamp
 Action (RELATIONSHIP_CREATED, RELATIONSHIP_DELETED, etc.)
 Subject, Relation, Object
 InitiatedBy (user who made the change)
 Details (JSON)
```

---

## API Endpoints (Quick Reference)

| Endpoint         | Method          | Purpose              |
| ---------------- | --------------- | -------------------- |
| `/check`         | POST            | Check permission     |
| `/explain`       | POST            | Debug decision       |
| `/relationships` | POST/GET/DELETE | Manage tuples        |
| `/roles`         | POST/GET        | RBAC roles           |
| `/permissions`   | POST/GET        | RBAC permissions     |
| `/users`         | POST/GET        | User management      |
| `/stores`        | POST/GET        | Authorization stores |
| `/audit-logs`    | GET             | Compliance trails    |

[**Full API Reference**](reference/api-reference.md)

---

## Use Cases

### 1. Document Collaboration

```
Alice creates document
 Alice owns it (relationship created)
 Alice shares with Bob (relationship created)
 Bob edits document
 Aegis checks: (user:bob, editor, document:x)
 Edit allowed
```

### 2. SaaS Multi-Tenant

```
Customer A's tenant: isolated Relationships, Users, Roles
Customer B's tenant: isolated Relationships, Users, Roles
 Complete data isolation

Query with X-Tenant-Id: customer-a
 Only sees Customer A's data
```

### 3. Microservices

```
Payment Service: calls /check before processing refund
Reporting Service: calls /check before generating report
Admin Service: calls /check before user management
 All authorization goes through Aegis
```

### 4. Compliance & Audit

```
Auditor queries:
  GET /api/v1/audit-logs
 See complete trail of access changes
 Use /explain to understand why any decision was made
```

---

## Design Principles

1. **Deterministic** Same input same output, always
2. **Deny-by-Default** DENY wins over ALLOW
3. **Tenant-Isolated** Multi-tenancy is mandatory
4. **Decoupled** Engine independent of HTTP/EF/DB
5. **Explainable** Every decision is traceable
6. **ReBAC-First** Relationship model is primary; RBAC is fallback

---

## Next Steps

### For Understanding the System

1. Read [Core Concepts](concepts/core-concepts-tuple-model.md) Deep dive into tuples, ReBAC, RBAC
2. Review [Architecture Overview](architecture/README.md) System design and module structure
3. Check [API Reference](reference/api-reference.md) Every endpoint documented

### For Development

1. Follow [Getting Started](guides/getting-started-development.md) Local setup in 5 minutes
2. Run tests Verify everything works
3. Try API calls Use the examples

### For Deployment

1. Read [Deployment Guide](guides/deployment-operations-guide.md) Docker, K8s, cloud setup
2. Configure database PostgreSQL setup
3. Set up monitoring Application Insights
4. Plan backup strategy Disaster recovery

---

## Key Differences from Alternatives

### vs. OpenFGA

| Feature                 | Aegis          | OpenFGA    |
| ----------------------- | -------------- | ---------- |
| **Language**            | .NET / C#      | Go         |
| **Authorization Model** | ReBAC + RBAC   | ReBAC only |
| **Multi-Tenancy**       | Built-in       | Optional   |
| **Explainability**      | `/explain` API | Debug API  |
| **RBAC Support**        | Yes (fallback) | No         |

### vs. Auth0 / Okta

| Feature         | Aegis               | Auth0/Okta          |
| --------------- | ------------------- | ------------------- |
| **Focus**       | Authorization only  | Auth + Identity     |
| **Purpose**     | Fine-grained access | User authentication |
| **Deployment**  | Self-hosted         | Cloud SaaS          |
| **Tuple Model** | Yes                 | No                  |

---

## What Aegis Does NOT Do

Authenticate users (JWT is optional, you bring auth)
 Manage API keys (security is your responsibility)
 Encrypt data at-rest (use your DB encryption)
 Handle business logic (pure authorization only)

---

## Roadmap

### Phase 1 (MVP)

- ReBAC direct tuple checks
- RBAC role permissions
- Multi-tenancy
- Audit logging

### Phase 2 (Next)

- Graph traversal (transitive relationships)
- Contextual conditions (time-based, IP-based, etc.)
- Performance caching (Redis)

### Phase 3 (Future)

- UI dashboard (relationship visualization)
- Analytics (access patterns)
- Bulk operations (CSV import/export)

---

## Documentation Map

| Document                                                 | Audience                | Content                           |
| -------------------------------------------------------- | ----------------------- | --------------------------------- |
| **Overview** (this file)                                 | Everyone                | Quick conceptual overview         |
| [Product Overview](product/product-overview.md)          | Product, stakeholders   | Vision, capabilities, use cases   |
| [Core Concepts](concepts/core-concepts-tuple-model.md)   | Engineers               | Detailed tuple model, ReBAC, RBAC |
| [API Reference](reference/api-reference.md)              | Developers              | Every endpoint with examples      |
| [Getting Started](guides/getting-started-development.md) | Developers              | Local setup, development workflow |
| [Architecture](architecture/README.md)                   | Architects, senior devs | Module structure, design patterns |
| [Deployment](guides/deployment-operations-guide.md)      | DevOps, SRE             | Production setup, monitoring, ops |

---

**Ready to dive deeper?** Pick a document above based on your role and interests.

### 3.1 MVP Architecture

```text
                +----------------------+
                �     Admin UI         |
                |  (React + TS)        |
                +----------+-----------+
                           |
                           v
                +----------------------+
                �     Aegis API        |
                �   (ASP.NET Core)     |
                +----------+-----------+
                           |
        -----------------------------------------
        �                    �                   |
        v                    v                   v
   PostgreSQL           Redis (optional)     Logging
   (main DB)            (cache)              (file/ELK)
```

---

### 3.2 Future Architecture (Scalable)

```text
           +------------------+
           �   API Gateway    |
           +--------+---------+
                    |
     -----------------------------------
     �                �                |
     v                v                v
 Auth Service   Authorization     Audit Service
               Engine (Aegis)
```

---

## 4. Domain Model

### 4.1 RBAC Core

| Entity         | Description                  |
| -------------- | ---------------------------- |
| Tenant         | Organization boundary        |
| User           | System user                  |
| Role           | Group of permissions         |
| Permission     | Action (e.g., `user:create`) |
| UserRole       | User-role mapping            |
| RolePermission | Role-permission mapping      |

---

### 4.2 ReBAC (NEW Core Innovation)

| Entity       | Description                              |
| ------------ | ---------------------------------------- |
| Relationship | Defines access between user and resource |

---

### Relationship Model

```text
(user, relation, object)
```

Example:

```text
user:1 owner document:10
user:2 viewer document:10
```

---

### Suggested Schema

```text
Relationships
- Id
- TenantId
- Subject (e.g., user:1, team:dev)
- Relation (owner, viewer, editor, member)
- Object (e.g., document:10, repo:1)
- Effect (allow | deny)
- CreatedAt
```

---

## 5. Authorization Model

### Hybrid Model (RBAC + ReBAC)

Aegis evaluates permissions using:

```text
1. Evaluate explicit deny tuples first
2. Evaluate ReBAC allow tuples
3. Evaluate RBAC allow fallback
4. Default deny
```

---

### Evaluation Flow

```text
1. Check explicit deny
2. Check ReBAC direct allow
3. Check RBAC allow fallback
4. Return final decision + reason code
```

---

### Future Extensions

- Conditional access (ABAC-lite)
- Hierarchical relationships (group user)
- Permission composition

---

## 6. API Design

### 6.1 Authentication

| Endpoint        | Method | Description   |
| --------------- | ------ | ------------- |
| `/auth/login`   | POST   | Issue JWT     |
| `/auth/refresh` | POST   | Refresh token |

---

### 6.2 Authorization (Core)

#### Check Permission

```http
POST /check
```

```json
{
    "user": "user:1",
    "relation": "viewer",
    "object": "document:10"
}
```

Response:

```json
{
    "allowed": true
}
```

---

#### Manage Relationships

```http
POST /relationships
```

---

### 6.3 RBAC APIs

- `/roles`
- `/permissions`
- `/users`

---

### 6.4 Audit API

- `/audit`

---

## 7. Design Principles

- **Clean Architecture**
- **Separation of Concerns**
- **API-first design**
- **Extensibility (ReBAC-first mindset)**
- **Multi-tenant safety**

---

## 8. Technology Stack

### Backend

- .NET (ASP.NET Core)
- EF Core

### Database

- PostgreSQL (primary)
- Redis (cache, optional)

### Frontend

- React + TypeScript

---

## 9. Deployment Strategy

### Minimum Setup

- API Server (.NET)
- PostgreSQL

---

### Recommended Setup

- Docker Compose
- Redis
- Reverse Proxy (Nginx)

---

### Future

- Kubernetes
- Horizontal scaling
- Observability (Prometheus, Grafana)

---

## 10. Development Roadmap

| Phase       | Scope                            |
| ----------- | -------------------------------- |
| **Phase 1** | RBAC + Basic Auth                |
| **Phase 2** | ReBAC (relationships + `/check`) |
| **Phase 3** | Hybrid engine (RBAC + ReBAC)     |
| **Phase 4** | Audit logging + UI               |
| **Phase 5** | Performance + caching            |
| **Phase 6** | SDK + integrations               |

---

## 11. Key Design Decisions

1. **Authorization-first system**
2. **OpenFGA-inspired relationship model**
3. **Hybrid RBAC + ReBAC approach**
4. **Centralized permission evaluation**
5. **Tenant-aware architecture**

---

## 12. Key Insights

> Traditional RBAC is not sufficient for modern systems.

Aegis solves this by:

- Introducing **relationship-based access**
- Supporting **resource-level permissions**
- Enabling **centralized authorization across services**

---

## 13. Architecture Document Map

Use this section as the entry point for implementation-level architecture details:

- `docs/architecture/project-structure.md` - production-ready project structure and module boundaries
- `docs/architecture/permission-engine.md` - evaluation model, conflict rules, and engine contracts
- `docs/architecture/database-design.md` - tuple store schema, indexing, and hot-path queries
- `docs/reference/api-reference.md` - canonical endpoint contracts, examples, and response model

---

## 14. Notes for Development

- Always scope queries by `TenantId`
- Permission check is a **hot path optimize early**
- Avoid over-engineering (no microservices in MVP)
- Keep permission model stable (backward compatibility)
