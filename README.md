
# 🛡️ Aegis Authorization Platform

![Platform](https://img.shields.io/badge/Platform-Authorization-blueviolet?logo=shield\&logoColor=white)
![Authorization Model](https://img.shields.io/badge/Auth-ReBAC%20%7C%20RBAC%20%7C%20ABAC-blueviolet)
![.NET 8 | 10](https://img.shields.io/badge/.NET-8%20%7C%2010-blueviolet?logo=dotnet\&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET-Core-blueviolet?logo=dotnet\&logoColor=white)
![Architecture](https://img.shields.io/badge/Architecture-DDD-blueviolet)
![License](https://img.shields.io/badge/License-MIT-yellow.svg)
![GitHub stars](https://img.shields.io/github/stars/cuthanhcam/Aegis?style=social)

**Centralized, explainable authorization for modern distributed systems.**

---

## 🚀 Overview

**Aegis** is a **centralized authorization platform** designed for modern, multi-tenant applications.

It provides a dedicated authorization service that delivers **deterministic and explainable access decisions**, eliminating the need to embed permission logic across multiple services.

Aegis supports multiple authorization models:

* Relationship-Based Access Control (**ReBAC**)
* Role-Based Access Control (**RBAC**)
* Attribute-Based Access Control (**ABAC**)

Built with **Domain-Driven Design (DDD)** and clean architecture principles, Aegis ensures scalability, maintainability, and clear separation of concerns.

---

## ✨ Key Features

* 🔐 **ReBAC (Primary)** — Fine-grained permissions via tuple model
* 👥 **RBAC** — Role-based fallback for coarse-grained access
* 🧩 **ABAC** — Attribute-driven policy evaluation
* 🏢 **Multi-Tenancy** — Strict tenant isolation
* 🔍 **Explainability** — Full decision tracing (`/explain`)
* 📜 **Audit Logs** — Compliance-ready tracking
* 🌐 **RESTful API** — Simple and predictable endpoints

---

## ❓ Why Aegis?

* Combines **ReBAC + RBAC + ABAC** in a single engine
* **Deterministic evaluation** — same input, same result
* Built-in **decision explainability** for debugging and audits
* Clean separation between **authorization engine and API layer**
* Designed for **multi-tenant SaaS systems from day one**

---

## ⚡ Quick Start

### Prerequisites

* .NET 8 SDK
* PostgreSQL (or Docker)

### Run locally

```bash
# Start PostgreSQL
docker run --name aegis-postgres \
  -e POSTGRES_USER=aegis \
  -e POSTGRES_PASSWORD=aegis123 \
  -e POSTGRES_DB=aegis_dev \
  -p 5432:5432 -d postgres:15

# Apply migrations & run API
cd src/Aegis.Api
dotnet ef database update
dotnet run
```

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

* Authorization Engine is **pure domain logic** (no HTTP / EF)
* Deterministic and testable evaluation
* Clear separation of concerns across layers

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

* **SaaS Multi-Tenant Systems** — Tenant-isolated authorization
* **Document Collaboration** — Fine-grained sharing & ownership
* **Microservices Architecture** — Centralized access control
* **Audit & Compliance** — Traceable decision logs

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

* Docker
* Kubernetes (Helm)
* AWS / Azure / GCP

---

## 📚 Documentation

* `docs/product/` — Overview & use cases
* `docs/concepts/` — Authorization models
* `docs/reference/` — API reference
* `docs/guides/` — Setup & deployment

---

## 🤝 Contributing

Contributions are welcome! See `CONTRIBUTING.md`.

---

## 📄 License

MIT License

---

## 👉 Get Started

* Product overview → `docs/product/product-overview.md`
* Development guide → `docs/guides/getting-started-development.md`
