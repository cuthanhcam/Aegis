# **Aegis – Centralized Access Control Service**

## 1. Introduction

**Aegis** is a **centralized access control system** built on top of **.NET Core**, designed to manage and enforce authorization across multiple applications and services.

The system provides:

* **RBAC Core**: Management of Users, Roles, and Permissions
* **Public APIs**: For permission validation and access control integration
* **Admin UI**: Interface for managing users, roles, permissions, and tenants
* **Audit Logging**: Tracks critical system activities
* **Multi-tenancy Support**: Isolated access control per organization
* **Microservice-ready Architecture**: Easily scalable and extensible

The goal is to deliver an **enterprise-ready**, maintainable, and scalable authorization platform.

---

## 2. System Architecture

### 2.1 Microservices Architecture (Recommended)

Aegis can be deployed as a **monolith (MVP)** or gradually evolved into a **microservices architecture**.

| Service                 | Responsibility                                  | Suggested Technology                       |
| ----------------------- | ----------------------------------------------- | ------------------------------------------ |
| **Auth Service**        | Authentication (JWT, OAuth2, Refresh Tokens)    | ASP.NET Core + IdentityServer              |
| **RBAC Service**        | Manage Users, Roles, Permissions, Multi-tenancy | ASP.NET Core Web API + EF Core             |
| **Audit Service**       | Logging, search, and filtering                  | ASP.NET Core + MongoDB/SQL + Elasticsearch |
| **API Gateway**         | Routing, authentication, rate limiting          | Ocelot / YARP                              |
| **Frontend UI**         | Admin dashboard for access management           | React + TypeScript + Ant Design            |
| **Integration Service** | External API for permission validation          | ASP.NET Core Web API                       |

> **MVP Scope**: Auth Service + RBAC Service + Frontend UI + Audit Service
> Microservices separation can be introduced during scaling.

---

### 2.2 High-Level Architecture

```
[Frontend UI] <--REST/gRPC--> [API Gateway] <---> [RBAC Service]
                                        |
                                        +--> [Auth Service]
                                        |
                                        +--> [Audit Service]
                                        |
                                        +--> [Integration Service]
```

---

## 3. Domain Model (RBAC Core)

### 3.1 Core Entities

| Entity         | Description                                      |
| -------------- | ------------------------------------------------ |
| Tenant         | Represents an organization (multi-tenant system) |
| User           | End-user belonging to a tenant                   |
| Role           | Role assigned within a tenant                    |
| Permission     | Action-based access control (global or scoped)   |
| UserRole       | Many-to-many mapping between User and Role       |
| RolePermission | Many-to-many mapping between Role and Permission |
| AuditLog       | Records system activities                        |

---

### 3.2 Simplified ERD

```
Tenant 1---* User
Tenant 1---* Role
Role *---* Permission
User *---* Role
AuditLog -> User, Tenant, Action
```

> All entities should include standard fields such as:
> `CreatedAt`, `UpdatedAt`, and `Status` for production readiness.

---

## 4. Design Patterns & Principles

* **Clean Architecture**: Separation into API / Application / Domain / Infrastructure layers
* **Repository Pattern + Unit of Work**: Data access abstraction using EF Core
* **Dependency Injection**: Built-in .NET Core DI container
* **DTO + AutoMapper**: Prevent domain leakage to API layer
* **Policy-Based Authorization**: Flexible authorization using .NET policies
* **Specification Pattern**: Advanced filtering and multi-tenant queries
* **Event-driven Architecture (Optional)**: For audit logs and integrations

---

## 5. Multi-Tenancy Strategy

* Include `TenantId` in all core tables (User, Role, Permission)
* **Soft Isolation**: Shared database, filtered by TenantId
* **Hard Isolation (Advanced)**: Separate database per tenant
* **Frontend Flow**: User logs in → selects tenant → token includes TenantId
* **Backend Enforcement**: All queries are scoped by TenantId

---

## 6. API Design

### 6.1 Authentication API

| Endpoint        | Method | Request Body             | Description                 |
| --------------- | ------ | ------------------------ | --------------------------- |
| `/auth/login`   | POST   | `{ username, password }` | Authenticate and return JWT |
| `/auth/refresh` | POST   | `{ refreshToken }`       | Issue new access token      |

---

### 6.2 RBAC API

| Endpoint                                         | Method   | Description                  |
| ------------------------------------------------ | -------- | ---------------------------- |
| `/tenants/{tenantId}/users`                      | GET/POST | Retrieve/Create users        |
| `/tenants/{tenantId}/roles`                      | GET/POST | Retrieve/Create roles        |
| `/tenants/{tenantId}/roles/{roleId}/permissions` | POST     | Assign permissions to a role |

---

### 6.3 Permission Check API

| Endpoint            | Method | Request Body                   | Response                  |
| ------------------- | ------ | ------------------------------ | ------------------------- |
| `/check-permission` | POST   | `{ userId, action, resource }` | `{ allowed: true/false }` |

---

### 6.4 Audit Log API

| Endpoint                    | Method | Query Params      | Description         |
| --------------------------- | ------ | ----------------- | ------------------- |
| `/tenants/{tenantId}/audit` | GET    | filter parameters | Retrieve audit logs |

> All APIs follow RESTful principles and use JWT-based authentication.

---

## 7. Frontend (React + TypeScript)

### Pages

* Dashboard (Tenants, Users, Roles, Permissions overview)
* User Management
* Role Management
* Permission Management
* Audit Log Viewer

---

### Key Components

* `<PermissionGuard permission="xyz" />` → Conditionally render UI based on permissions
* State Management: **Redux Toolkit** or **Zustand**
* UI Framework: **Ant Design** or **Material UI**

---

## 8. Audit Logging

### Captured Events

* CRUD operations on Users, Roles, Permissions
* Optional: Permission checks

---

### Sample Schema

```json
{
  "id": "...",
  "tenantId": "...",
  "userId": "...",
  "action": "CREATE_USER",
  "target": "User#123",
  "timestamp": "...",
  "metadata": {}
}
```

* Supports filtering and search via SQL indexing or Elasticsearch

---

## 9. Microservice Communication

* **Synchronous**: REST / HTTP (e.g., RBAC → Integration Service)
* **Asynchronous**: RabbitMQ / Kafka (audit logs, notifications)
* **API Gateway**: Handles authentication, routing, and throttling

---

## 10. Security & Best Practices

* JWT Access Tokens + Refresh Tokens
* Secure password hashing (**Argon2** or **PBKDF2**)
* Role-based + Policy-based Authorization
* Rate limiting / throttling
* Optional Multi-Factor Authentication (MFA)
* Encryption at rest + TLS in transit

---

## 11. Development Roadmap

| Phase             | Scope                                                   |
| ----------------- | ------------------------------------------------------- |
| **Phase 1 (MVP)** | Auth Service, RBAC core, Admin UI, Multi-tenancy        |
| **Phase 2**       | Audit logging, Permission check API, Policy-based auth  |
| **Phase 3**       | Microservices split, API Gateway, Integration service   |
| **Phase 4**       | UI improvements, dashboards, real-time audit monitoring |
| **Phase 5**       | Production hardening, scaling, and operational readiness |

---

## 12. Entity-Relationship Diagram (ERD)

```text
+------------------+         +------------------+        +------------------+
|      Tenant      | 1-----* |       User       | *----* |      Role        |
+------------------+         +------------------+        +------------------+
| TenantId (PK)    |         | UserId (PK)      |        | RoleId (PK)      |
| Name             |         | TenantId (FK)    |        | TenantId (FK)    |
| CreatedAt        |         | Username         |        | Name             |
| UpdatedAt        |         | Email            |        | Description      |
| Status           |         | PasswordHash     |        | CreatedAt        |
+------------------+         | CreatedAt        |        | UpdatedAt        |
                             | UpdatedAt        |        | Status           |
                             | Status           |        +------------------+
                             +------------------+
                                   |
                                   |
                                   * 
                              +------------------+
                              |    UserRole      |
                              +------------------+
                              | UserId (FK, PK)  |
                              | RoleId (FK, PK)  |
                              +------------------+

+------------------+         +------------------+
|   Permission     | *-----* |   RolePermission |
+------------------+         +------------------+
| PermissionId (PK)|         | RoleId (FK, PK)  |
| Name             |         | PermissionId(FK,PK)|
| Description      |         +------------------+
| Scope (Global/Tenant) |
| CreatedAt        |
| UpdatedAt        |
| Status           |
+------------------+

+------------------+
|    AuditLog      |
+------------------+
| AuditLogId (PK)  |
| TenantId (FK)    |
| UserId (FK)      |
| Action           |
| Target           |
| Timestamp        |
| Metadata         |
+------------------+
```

> This ERD includes **Tenant, User, Role, Permission, mapping tables, and AuditLog**.
> All entities are multi-tenant aware and production-ready with timestamps and status fields.

---

## 13. Microservice Architecture Diagram

```text
                                      +-------------------+
                                      |   Frontend UI     |
                                      | (React + TS)      |
                                      +---------+---------+
                                                |
                                                v
                                        +-----------------+
                                        |   API Gateway   |
                                        | (Ocelot / YARP) |
                                        +--------+--------+
                                                 |
       ---------------------------------------------------------------------------------
       |                    |                       |                      |
       v                    v                       v                      v
+---------------+     +---------------+       +---------------+     +----------------+
| Auth Service  |     | RBAC Service  |       | Audit Service |     | Integration    |
| JWT/OAuth2    |     | Users/Roles   |       | Logs storage  |     | Service        |
| IdentityServer|     | Permissions   |       | Elastic/SQL   |     | Check-permission|
+---------------+     +---------------+       +---------------+     +----------------+
```

> **Flow**: Frontend → API Gateway → respective microservices (Auth, RBAC, Audit, Integration).
> All services enforce **TenantId scoping** for multi-tenancy.

---

## 14. API Flow Diagram

```text
[Frontend UI] 
    |
    | POST /auth/login { username, password }
    v
[Auth Service] ---> returns JWT + TenantId
    |
    v
[Frontend stores JWT]
    |
    | POST /check-permission { userId, action, resource }
    v
[Integration Service / RBAC Service]
    |
    | Evaluate permission
    v
[RBAC Service / Database]
    |
    | Return { allowed: true/false }
    v
[Frontend UI]
    |
    | Render feature according to permission
```

**Audit Flow**:

```text
[RBAC / Integration Service]
    |
    | Emit event (User CRUD / Role assignment / Permission check)
    v
[Audit Service] -> Store log in SQL/MongoDB/Elasticsearch
```

---

## 15. Sequence Diagram: Permission Check Flow

```text
User -> Frontend UI: Trigger action (e.g., Delete Post)
Frontend UI -> API Gateway: POST /check-permission
API Gateway -> Integration Service: Forward request
Integration Service -> RBAC Service: Evaluate user permissions
RBAC Service -> Database: Query UserRole and RolePermission
Database -> RBAC Service: Return roles and permissions
RBAC Service -> Integration Service: Return allowed=true/false
Integration Service -> API Gateway: Return allowed=true/false
API Gateway -> Frontend UI: Return allowed=true/false
Frontend UI -> User: Allow or deny action based on result
RBAC/Integration Service -> Audit Service: Log permission check event
```

> This sequence shows **end-to-end flow** from the user triggering an action to permission evaluation and audit logging.

---

## 16. Notes for Development Team

1. **Tenant-aware enforcement**: Every request and database query is scoped by `TenantId`.
2. **Centralized permission check**: RBAC logic lives in RBAC Service / Integration Service API.
3. **Audit logging**: Asynchronous for low-latency requests.
4. **Frontend Guard**: `<PermissionGuard permission="xyz" />` handles conditional rendering.
5. **JWT tokens**: Include `UserId` and `TenantId` for service authorization.
