# **Aegis – Authorization Platform (RBAC + ReBAC)**

## 1. Introduction

**Aegis** is a **centralized authorization platform** built with **.NET**, designed to provide **fine-grained access control** across multiple applications and services.

Inspired by systems like OpenFGA, Aegis evolves beyond traditional RBAC into a **hybrid authorization engine** that supports:

* **RBAC (Role-Based Access Control)** for coarse-grained permissions
* **ReBAC (Relationship-Based Access Control)** for fine-grained, resource-level access

---

## 🎯 Core Vision

> Aegis is not just an access control system —
> it is a **centralized authorization service** that other systems rely on.

---

## 2. System Capabilities

### 🔐 Authentication (Optional Layer)

* JWT-based authentication
* Access & Refresh tokens
* Tenant-aware identity

---

### 🔥 Authorization Engine (Core)

Aegis provides a **centralized permission evaluation engine**:

```text
Can user U perform relation R on resource X?
```

Supports:

#### 1. RBAC

```text
user → role → permission
```

#### 2. ReBAC (OpenFGA-inspired)

```text
user → relation → resource
```

Example:

```text
user:1 owner document:10
user:2 viewer document:10
```

---

### 🌐 Public APIs

* `/check` → Evaluate permission
* `/relationships` → Manage relationships
* `/roles` → RBAC management
* `/permissions` → Permission definitions

---

### 🖥 Admin UI

* Manage users, roles, permissions
* Assign relationships (ReBAC)
* View audit logs

---

### 📊 Audit Logging

* Track:

  * Permission checks
  * Role assignments
  * Relationship changes

---

### 🏢 Multi-Tenancy

* Tenant-based isolation
* All data scoped by `TenantId`
* Supports:

  * Shared DB (MVP)
  * Isolated DB (advanced)

---

## 3. Architecture Overview

### 3.1 MVP Architecture

```text
                +----------------------+
                |     Admin UI         |
                |  (React + TS)        |
                +----------+-----------+
                           |
                           v
                +----------------------+
                |     Aegis API        |
                |   (ASP.NET Core)     |
                +----------+-----------+
                           |
        -----------------------------------------
        |                    |                   |
        v                    v                   v
   PostgreSQL           Redis (optional)     Logging
   (main DB)            (cache)              (file/ELK)
```

---

### 3.2 Future Architecture (Scalable)

```text
           +------------------+
           |   API Gateway    |
           +--------+---------+
                    |
     -----------------------------------
     |                |                |
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

### 4.2 ReBAC (NEW – Core Innovation)

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

* Conditional access (ABAC-lite)
* Hierarchical relationships (group → user)
* Permission composition

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

* `/roles`
* `/permissions`
* `/users`

---

### 6.4 Audit API

* `/audit`

---

## 7. Design Principles

* **Clean Architecture**
* **Separation of Concerns**
* **API-first design**
* **Extensibility (ReBAC-first mindset)**
* **Multi-tenant safety**

---

## 8. Technology Stack

### Backend

* .NET (ASP.NET Core)
* EF Core

### Database

* PostgreSQL (primary)
* Redis (cache, optional)

### Frontend

* React + TypeScript

---

## 9. Deployment Strategy

### Minimum Setup

* API Server (.NET)
* PostgreSQL

---

### Recommended Setup

* Docker Compose
* Redis
* Reverse Proxy (Nginx)

---

### Future

* Kubernetes
* Horizontal scaling
* Observability (Prometheus, Grafana)

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

* Introducing **relationship-based access**
* Supporting **resource-level permissions**
* Enabling **centralized authorization across services**

---

## 13. Architecture Document Map

Use this section as the entry point for implementation-level architecture details:

- `docs/architecture/project-structure.md` - production-ready project structure and module boundaries
- `docs/architecture/permission-engine.md` - evaluation model, conflict rules, and engine contracts
- `docs/architecture/database-design.md` - tuple store schema, indexing, and hot-path queries
- `docs/architecture/api-spec.md` - endpoint contracts, check/explain APIs, and response model

---

## 14. Notes for Development

* Always scope queries by `TenantId`
* Permission check is a **hot path → optimize early**
* Avoid over-engineering (no microservices in MVP)
* Keep permission model stable (backward compatibility)