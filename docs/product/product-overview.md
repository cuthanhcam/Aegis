# Aegis Authorization Platform

**Tagline:** _Guarding access with a practical authorization engine._

---

## 1. Vision & Purpose

**Aegis** is a **centralized authorization platform** that solves a critical problem in modern application architecture:

> **How do you manage permissions at scale across multiple services while maintaining consistency, auditability, and clear ownership?**

Aegis decouples authorization logic from application code by providing:

- **Deterministic permission decisions** through a dedicated authorization engine
- **Hybrid authorization models** that support both relationship-based (ReBAC) and role-based (RBAC) access control
- **Explainability** so teams can debug access issues quickly
- **Multi-tenancy** for hosting multiple authorization contexts in a single deployment
- **Auditability** through immutable audit trails

---

## 2. The Problem Aegis Solves

### Traditional Approach (Monolithic Authorization)

```text
Application 1  Authorization logic embedded in code
Application 2  Authorization logic embedded in code
Application 3  Authorization logic embedded in code

Result: Scattered logic, inconsistent decisions, hard to maintain
```

### Aegis Approach (Centralized Authorization)

```text
Application 1
                 Aegis Authorization Engine
Application 2     (centralized, reusable, auditable)

Application 3
```

**Benefits:**

- Single source of truth for authorization
- Consistent permission decisions across all applications
- Easy to audit and debug (complete trace available)
- Reduced code duplication
- Scaling authorization independently from application logic

---

## 3. Core Capabilities

### 3.1 Permission Check API

Evaluate permissions in real-time:

```http
POST /api/v1/check
{
  "subject": "user:alice",
  "relation": "editor",
  "object": "document:report-2024"
}
```

**Response:**

```json
{
    "allowed": true,
    "decision": "ALLOW",
    "reasonCode": "ALLOW_REBAC_DIRECT"
}
```

### 3.2 Explanation API

Understand **why** a decision was made:

```http
POST /api/v1/explain
{
  "subject": "user:alice",
  "relation": "editor",
  "object": "document:report-2024"
}
```

**Response:**

```json
{
    "allowed": true,
    "trace": [
        { "step": "DENY_POLICY", "result": "NOT_MATCHED" },
        {
            "step": "REBAC_DIRECT",
            "result": "MATCHED",
            "tuple": "(user:alice, editor, document:report-2024)"
        },
        { "step": "FINAL", "result": "ALLOW" }
    ]
}
```

### 3.3 Relationship Management

Create and manage permission tuples:

```http
POST /api/v1/relationships
{
  "subject": "team:engineering",
  "relation": "owner",
  "object": "repo:aegis",
  "effect": "allow"
}
```

Supports both `allow` and explicit `deny` effects.

### 3.4 ReBAC (Relationship-Based Access Control)

Define permissions through relationships:

```text
Tuple Format: (subject, relation, object)

Examples:
 (user:alice, owner, document:report)      Alice owns this document
 (team:dev, member, user:bob)              Bob is a member of dev team
 (team:dev, owner, repo:code)              Dev team owns the code repository
```

**Use cases:**

- Resource-level access (who can edit which document)
- Team/group membership
- Hierarchical relationships

### 3.5 RBAC (Role-Based Access Control)

Fallback model for coarse-grained permissions:

```text
Model: user  role  permission

Examples:
 user:admin  role:admin  permission:document:delete
 user:viewer  role:viewer  permission:document:read
```

**Use cases:**

- System-level permissions
- Default fallback when no ReBAC tuple matches

### 3.6 Hybrid Evaluation

Aegis resolves permissions through a deterministic flow:

```text
1. Check explicit DENY rules  if matched, return DENY
2. Check ReBAC ALLOW rules  if matched, return ALLOW
3. Check RBAC ALLOW rules  if matched, return ALLOW
4. Default  return DENY

Key Principle: DENY always overrides ALLOW (explicit deny wins)
```

---

## 4. Multi-Tenant Isolation

Every permission check is **scoped by tenant**:

```http
POST /api/v1/check
X-Tenant-Id: tenant-123

{
  "subject": "user:1",
  "relation": "viewer",
  "object": "document:10"
}
```

All relationships, users, roles, and permissions are **strictly isolated** per tenant.

---

## 5. Multi-Store Support

Aegis can manage multiple **authorization stores**, enabling:

- **Per-application stores** (each microservice has its own authorization context)
- **Per-tenant stores** (each customer has isolated authorization)
- **Per-environment stores** (dev, staging, production)

```text
Store 1: "payment-service"     manages payment authorization rules
Store 2: "document-service"    manages document authorization rules
Store 3: "admin-service"       manages admin authorization rules
```

---

## 6. Auditability

Every permission decision is **traceable and auditable**:

- **Audit logs** record all relationship changes (who created/deleted what, when)
- **Decision logs** record all permission checks (who checked what, when, result)
- **Explain API** provides forensic trace of decision logic

Example audit entry:

```json
{
    "timestamp": "2026-04-07T10:30:00Z",
    "action": "RELATIONSHIP_CREATED",
    "subject": "user:alice",
    "relation": "editor",
    "object": "document:report",
    "createdBy": "admin:system",
    "tenantId": "tenant-123"
}
```

---

## 7. Authentication Integration (Optional)

Aegis supports **JWT-based authentication** for API access:

```http
POST /api/v1/auth/login
{
  "username": "user",
  "password": "password"
}

Response:
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "...",
  "expiresIn": 3600
}
```

---

## 8. Domain Model Overview

### Core Entities

| Entity                 | Purpose                                        |
| ---------------------- | ---------------------------------------------- |
| **Tenant**             | Isolation boundary for multi-tenancy           |
| **Store**              | Authorization context (per app, per env, etc.) |
| **Relationship**       | A single tuple in the authorization system     |
| **AuthorizationModel** | Schema/configuration for a store               |
| **User**               | Identity in the system                         |
| **Role**               | Collections of permissions (RBAC)              |
| **Permission**         | A grantable capability                         |

### Value Objects

| Object           | Purpose                                                         |
| ---------------- | --------------------------------------------------------------- |
| **SubjectId**    | Typed identifier: `<type>:<id>` (e.g., `user:1`, `team:dev`)    |
| **ObjectId**     | Typed identifier: `<type>:<id>` (e.g., `document:10`)           |
| **RelationName** | Named relationship: `owner`, `editor`, `viewer`, `member`, etc. |

---

## 9. Key Design Principles

### 9.1 Deterministic Decisions

Every permission check produces the **same result** given the same input and state. No randomness, no ordering issues.

### 9.2 Explicit Deny Precedence

Denial always wins. This follows the **principle of least privilege**:

```
DENY > ALLOW (regardless of where ALLOW came from)
```

### 9.3 Tenant Isolation

Multi-tenancy is **not optional**. Every data access path checks tenant context.

### 9.4 Engine/Application Separation

The authorization engine is **decoupled** from transport, persistence, and application logic. Swap implementations without affecting decision logic.

### 9.5 Explainability First

Every decision must be traceable for:

- Support debugging
- Security incident investigation
- Compliance audits

### 9.6 ReBAC Primary, RBAC Fallback

**ReBAC is the recommended model** for modern applicationsit's more expressive. RBAC is a fallback for legacy systems or simple role-based schemes.

---

## 10. Architectural Layers

Aegis is structured as a **layered, DDD-driven architecture**:

```text

   API Layer (HTTP)             /check, /explain, /relationships

  Application Layer             Use cases, orchestration

  Authorization Engine          Core decision logic (ReBAC + RBAC)

  Domain Model                  Entities, ValueObjects, Events

  Infrastructure Layer          DB, persistence, adapters

```

**Benefit:** Clear boundaries make the system testable, scalable, and maintainable.

---

## 11. Technology Stack

- **.NET 8** Modern, performant, cross-platform runtime
- **ASP.NET Core** Web API framework with minimal hosting
- **Entity Framework Core** ORM for data access
- **PostgreSQL** Primary data store (recommended for production)
- **xUnit + Testcontainers** Comprehensive testing strategy

---

## 12. Use Cases

### Use Case 1: Document Collaboration Platform

```text
One user creates a document  grants editor role to team members
 Aegis checks permission before allowing edits
 Audit log records all permission changes
```

### Use Case 2: SaaS Multi-Tenant System

```text
Each customer has isolated Store and relationships
 Customer A's staff can only access Customer A's data
 Authorization rules are customer-specific
```

### Use Case 3: Microservices Authorization

```text
Payment Service  calls Aegis /check before processing refund
Reporting Service  calls Aegis /check before generating report
Admin Service  calls Aegis /check before user management
 All authorization decisions go through Aegis
```

### Use Case 4: Compliance & Auditability

```text
Auditor queries Aegis audit logs to prove:
- Who had access to what resource
- When permissions were granted/revoked
- Why a permission decision was made (via /explain)
```

---

## 13. What Aegis Does NOT Do

Aegis is **pure authorization**. It does NOT:

- Authenticate users (you bring JWT tokens)
- Manage API keys (you implement key management separately)
- Encrypt data (at-rest encryption is your responsibility)
- Validate business logic (it only checks permissions)

---

## 14. Next Steps

1. **Understand the Tuple Model** Read `../concepts/core-concepts-tuple-model.md`
2. **Learn the API** Read `../reference/api-reference.md`
3. **Set Up Locally** Follow `../guides/getting-started-development.md`
4. **Deploy to Production** Follow `../guides/deployment-operations-guide.md`

---

## 15. Support & Contribution

- **Questions?** Open a GitHub Discussion
- **Found a bug?** Open a GitHub Issue
- **Want to contribute?** See `CONTRIBUTING.md`

---

**Aegis: Where authorization is clear, auditable, and decoupled.**
