# 🛡️ Aegis Authorization Platform

![Platform](https://img.shields.io/badge/Platform-Authorization-blueviolet?logo=shield&logoColor=white)
![Authorization Model](https://img.shields.io/badge/Auth-ReBAC%20%7C%20RBAC%20%7C%20ABAC-blueviolet)
![.NET 8 | 10](https://img.shields.io/badge/.NET-8%20%7C%2010-blueviolet?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET-Core-blueviolet?logo=dotnet&logoColor=white)
![Architecture](https://img.shields.io/badge/Architecture-DDD-blueviolet)
![License](https://img.shields.io/badge/License-MIT-yellow.svg)

**Centralized, explainable authorization for modern distributed systems.**

---

## 🚀 Overview

**Aegis** is a **centralized authorization platform** designed for modern, multi-tenant applications.

It provides a dedicated authorization service that delivers **deterministic and explainable access decisions**, eliminating the need to embed permission logic across multiple services.

Aegis supports multiple authorization models:

- Relationship-Based Access Control (**ReBAC**)
- Role-Based Access Control (**RBAC**)
- Attribute-Based Access Control (**ABAC**)

Built with **Domain-Driven Design (DDD)** and clean architecture principles, Aegis ensures scalability, maintainability, and clear separation of concerns.

---

## ✨ Key Features

- 🔐 **ReBAC (Primary)** — Fine-grained permissions via tuple model
- 👥 **RBAC** — Role-based fallback for coarse-grained access
- 🧩 **ABAC** — Attribute-driven policy evaluation
- 🏢 **Multi-Tenancy** — Strict tenant isolation
- 🔍 **Explainability** — Full decision tracing (`/explain`)
- 📜 **Audit Logs** — Compliance-ready tracking
- 🌐 **RESTful API** — Simple and predictable endpoints

---

## ❓ Why Aegis?

- Combines **ReBAC + RBAC + ABAC** in a single engine
- **Deterministic evaluation** — same input, same result
- Built-in **decision explainability** for debugging and audits
- Clean separation between **authorization engine and API layer**
- Designed for **multi-tenant SaaS systems from day one**

---

## ⚡ Quick Start

### Prerequisites

- .NET 8 SDK
- PostgreSQL (or Docker)

### Run locally

```bash
 $env:JWT_SECRET = "your-local-dev-secret"
docker compose -f docker/docker-compose.yml -f docker/docker-compose.development.yml up --build
```

This starts Postgres, Redis, runs the one-shot migration container, and then starts the API.

### Test

```bash
curl -X POST http://localhost:5000/api/v1/check \
  -H "X-Tenant-Id: my-tenant" \
  -d '{"subject":"user:alice","relation":"editor","object":"document:x"}'
```

**Expected response:**

```json
{ "allowed": true }
```

### Initial Orientation: Verify ReBAC, RBAC, ABAC

Use one tenant (example: `tenant-a`) and run these in order to confirm a fresh setup behaves correctly.

1) Create an authorization model for ReBAC rewrite:

```bash
curl -X POST http://localhost:5000/api/v1/stores/tenant-a/authorization-models \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d '{
    "schemaVersion":"1.1",
    "model":"type document\n  define viewer: viewer from parent\ntype folder\n  define viewer: this"
  }'
```

2) Seed ReBAC tuples (document parent folder, and user viewer on folder):

```bash
curl -X POST http://localhost:5000/api/v1/relationships \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d '{"subject":"folder:eng","relation":"parent","object":"document:rebac-1","effect":"allow"}'

curl -X POST http://localhost:5000/api/v1/relationships \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d '{"subject":"user:bob","relation":"viewer","object":"folder:eng","effect":"allow"}'
```

3) Seed RBAC permission and assignment:

```bash
curl -X POST http://localhost:5000/api/v1/tenants/tenant-a/roles \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d '{"name":"reader","description":"Default reader role"}'

curl -X POST http://localhost:5000/api/v1/tenants/tenant-a/permissions \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d '{"relation":"viewer","object":"document:rbac-1"}'

curl -X POST http://localhost:5000/api/v1/tenants/tenant-a/permissions/assign-to-role \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d '{"roleName":"reader","relation":"viewer","object":"document:rbac-1"}'
```

4) Seed ABAC-conditioned RBAC permission:

```bash
curl -X POST http://localhost:5000/api/v1/tenants/tenant-a/permissions \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d '{"relation":"viewer","object":"document:abac-1","conditionName":"feature_enabled"}'

curl -X POST http://localhost:5000/api/v1/tenants/tenant-a/permissions/assign-to-role \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d '{"roleName":"reader","relation":"viewer","object":"document:abac-1","conditionName":"feature_enabled"}'
```

5) Run checks:

```bash
# ReBAC rewrite allow (user:bob -> folder:eng -> document:rebac-1)
curl -X POST "http://localhost:5000/api/v1/check?tenantId=tenant-a" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d '{"subject":"user:bob","relation":"viewer","object":"document:rebac-1"}'

# RBAC fallback allow
curl -X POST "http://localhost:5000/api/v1/check?tenantId=tenant-a" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d '{"subject":"user:alice","relation":"viewer","object":"document:rbac-1"}'

# ABAC condition false -> deny
curl -X POST "http://localhost:5000/api/v1/check?tenantId=tenant-a" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d '{"subject":"user:alice","relation":"viewer","object":"document:abac-1","context":{"feature_enabled":false}}'

# ABAC condition true -> allow
curl -X POST "http://localhost:5000/api/v1/check?tenantId=tenant-a" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d '{"subject":"user:alice","relation":"viewer","object":"document:abac-1","context":{"feature_enabled":true}}'
```

---

## 🧠 Authorization Model

### ReBAC (Primary)

```text
(user:alice, owner, document:report)
(team:engineering, member, user:bob)
```

### RBAC (Fallback)

```text
user:admin → role:admin → document:delete
```

### ABAC (Attributes)

```text
user.department == "finance" AND resource.type == "invoice"
```

### Evaluation Logic

```text
DENY > ReBAC ALLOW > RBAC ALLOW > ABAC > DEFAULT DENY
```

---

## 🏗️ Architecture

Layered + DDD design:

```text
API → Application → Authorization Engine → Domain → Infrastructure
```

**Highlights:**

- Authorization Engine is **pure domain logic** (no HTTP / EF)
- Deterministic and testable evaluation
- Clear separation of concerns across layers

---

## 📡 API Overview

| Endpoint         | Purpose              |
| ---------------- | -------------------- |
| `/check`         | Permission check     |
| `/explain`       | Debug authorization  |
| `/relationships` | Manage ReBAC tuples  |
| `/roles`         | RBAC role management |
| `/audit-logs`    | Audit trail          |

📚 Full documentation: `docs/`

---

## 🎯 Use Cases

- **SaaS Multi-Tenant Systems** — Tenant-isolated authorization
- **Document Collaboration** — Fine-grained sharing & ownership
- **Microservices Architecture** — Centralized access control
- **Audit & Compliance** — Traceable decision logs

---

## 🧪 Testing

```bash
dotnet test
dotnet test /p:CollectCoverage=true
```

---

## 🚢 Deployment

```bash
docker build -t aegis .
docker run -e DB_CONNECTION_STRING="..." aegis
```

Supports:

- Docker
- Kubernetes (Helm)
- AWS / Azure / GCP

---

## 📚 Documentation

- `docs/product/` — Overview & use cases
- `docs/concepts/` — Authorization models
- `docs/reference/` — API reference
- `docs/guides/` — Setup & deployment

---

## 🤝 Contributing

Contributions are welcome! See `CONTRIBUTING.md`.

---

## 📄 License

MIT License

---

## 👉 Get Started

- Product overview → `docs/product/product-overview.md`
- Development guide → `docs/guides/getting-started-development.md`
