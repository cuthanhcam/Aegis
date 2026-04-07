# Aegis  Authorization Platform

**Tagline:** *Guarding access with a practical authorization engine.*

---

##  What is Aegis?

**Aegis** is a **centralized authorization platform** that provides deterministic permission decisions for modern, multi-tenant applications.

Instead of scattering authorization logic throughout your codebase, Aegis decouples it into a dedicated microservice that all your applications rely on.

### Core Features

 **ReBAC (Relationship-Based Access Control)**  Fine-grained permissions via tuple model `(subject, relation, object)`  
 **RBAC (Role-Based Access Control)**  Coarse-grained role-based fallback  
 **Multi-Tenancy**  Strict tenant isolation for SaaS systems  
 **Explainability**  Full decision trace for debugging and audits  
 **Auditability**  Complete audit logs for compliance  
 **RESTful API**  Simple, predictable endpoint design  

---

##  Documentation Navigation

### For Product Stakeholders

Start here to understand **what** Aegis does:

 [**Product Overview**](docs/product/product-overview.md)  
 Vision, capabilities, use cases, design principles

 [**Core Concepts**](docs/concepts/core-concepts-tuple-model.md)  
 Tuple model, ReBAC vs RBAC, authorization flow

### For Developers

Ready to build and integrate? Start here:

 [**Getting Started**](docs/guides/getting-started-development.md)  
 Local setup, development workflow, testing

 [**API Reference**](docs/reference/api-reference.md)  
 Complete endpoint documentation with examples

 [**Architecture**](docs/architecture/project-structure.md)  
 Module structure, dependency flow, design patterns

### For Operations & DevOps

Deploying to production? Check these:

 [**Deployment Guide**](docs/guides/deployment-operations-guide.md)  
 Docker, Kubernetes, cloud platforms, infrastructure

 [**Technical Architecture**](docs/architecture/)  
 Database schema, API spec, permission engine details

---

##  Quick Start (5 minutes)

### Prerequisites
- **.NET 8 SDK**
- **PostgreSQL 14+** (or Docker)

### 1. Clone & Navigate

```bash
cd D:\Workspace\Aegis
git clone <repo>
```

### 2. Configure Database

```bash
# Using Docker
docker run --name aegis-postgres \
  -e POSTGRES_USER=aegis \
  -e POSTGRES_PASSWORD=aegis123 \
  -e POSTGRES_DB=aegis_dev \
  -p 5432:5432 \
  -d postgres:15
```

### 3. Run Migrations

```bash
cd src/Aegis.Api
dotnet ef database update
```

### 4. Start the API

```bash
dotnet run
```

### 5. Test a Permission Check

```bash
# Create a relationship
curl -X POST http://localhost:5000/api/v1/relationships \
  -H "X-Tenant-Id: my-tenant" \
  -d '{"subject":"user:alice","relation":"editor","object":"document:x"}'

# Check permission
curl -X POST http://localhost:5000/api/v1/check \
  -H "X-Tenant-Id: my-tenant" \
  -d '{"subject":"user:alice","relation":"editor","object":"document:x"}'

# Response: { "allowed": true, "decision": "ALLOW", ... }
```

 [**Full Getting Started Guide**](docs/guides/getting-started-development.md)

---

##  Architecture

Aegis is a **layered, DDD-driven system**:

```

 API Layer (HTTP Controllers)       /check, /explain, /relationships

 Application Layer (Use Cases)      Orchestration, command/query handling

 Authorization Engine               ReBAC + RBAC deterministic evaluation (NO HTTP/EF)

 Domain Model (DDD)                 Entities, ValueObjects, events, repository interfaces

 Infrastructure (Persistence)       DbContext, Repository impl, adapters

```

**Key Projects:**

| Project | Purpose |
|---------|---------|
| `Aegis.SharedKernel` | Base primitives (Entity, AggregateRoot, ValueObject) |
| `Aegis.Domain` | Relationship, Store, AuthorizationModel entities |
| `Aegis.Authorization` | ReBAC + RBAC evaluation engine (NO EF, NO HTTP) |
| `Aegis.Application` | Use cases, Application services |
| `Aegis.Infrastructure` | DbContext, Repositories, EF implementations |
| `Aegis.Api` | HTTP controllers, middleware, routing |
| `Aegis.UnitTests` | Domain & Authorization logic tests |
| `Aegis.IntegrationTests` | API & persistence integration tests |

 [**Architecture Details**](docs/architecture/project-structure.md)

---

##  Authorization Models

### ReBAC (Primary)

Define permissions via relationships:

```
(user:alice, owner, document:report)
(team:engineering, member, user:bob)
(team:product, owner, repo:roadmap)
```

**Use for:** Fine-grained resource ownership, team membership, hierarchies

### RBAC (Fallback)

Define permissions via roles:

```
user:admin  role:admin  permission:document:delete
user:viewer  role:viewer  permission:document:read
```

**Use for:** System-level permissions, legacy role schemes

### Hybrid Evaluation

```
1. Check explicit DENY rules       if matched, return DENY
2. Check ReBAC ALLOW rules         if matched, return ALLOW
3. Check RBAC ALLOW rules          if matched, return ALLOW
4. Default                         return DENY

Key: DENY always overrides ALLOW (principle of least privilege)
```

 [**Detailed Concept Guide**](docs/concepts/core-concepts-tuple-model.md)

---

##  API Endpoints (Overview)

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/v1/check` | POST | Check permission (is user allowed?) |
| `/api/v1/explain` | POST | Debug decision (why was access denied?) |
| `/api/v1/relationships` | POST/GET/DELETE | Create/read/remove tuples |
| `/api/v1/roles` | POST/GET | RBAC role management |
| `/api/v1/permissions` | POST/GET | Permission definitions |
| `/api/v1/users` | POST/GET | User management |
| `/api/v1/stores` | POST/GET | Authorization store (per app/env) |
| `/api/v1/audit-logs` | GET | Compliance audit trail |

 [**Complete API Reference**](docs/reference/api-reference.md)

---

##  Deployment

### Docker

```bash
docker build -t aegis:latest .
docker run -e DB_CONNECTION_STRING="Host=postgres;..." aegis:latest
```

### Kubernetes

```bash
helm install aegis ./helm \
  --set image.tag=latest \
  --set db.host=postgres-svc
```

### Cloud Platforms

- **AWS ECS/RDS**  See [Deployment Guide](docs/guides/deployment-operations-guide.md#4-docker-deployment)
- **Azure Container Instances/PostgreSQL**  See Deployment Guide
- **Google Cloud Run + Cloud SQL**  See Deployment Guide

 [**Full Deployment & Operations Guide**](docs/guides/deployment-operations-guide.md)

---

##  Testing

```bash
# Unit tests (domain, authorization logic)
dotnet test tests/Aegis.UnitTests

# Integration tests (API, persistence)
dotnet test tests/Aegis.IntegrationTests

# All tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true
```

---

##  Technology Stack

- **.NET 8**  Modern, performant runtime
- **ASP.NET Core**  Web API framework
- **Entity Framework Core**  ORM
- **PostgreSQL**  Primary data store
- **xUnit + Testcontainers**  Testing
- **Application Insights**  Monitoring (optional)

---

##  Use Cases

### 1. Document Collaboration Platform

Alice creates a document  grants access to team  Aegis checks before allowing edits

### 2. SaaS Multi-Tenant System

Customer A has isolated authorization context  data strictly compartmentalized

### 3. Microservices Authorization

Payment Service  calls Aegis /check  Reporting Service  calls Aegis /check

### 4. Compliance & Audit

Auditor queries Aegis logs to prove who had access when  /explain shows exactly why

 [**More Examples in Product Overview**](docs/product/product-overview.md#12-use-cases)

---

##  Design Principles

1. **Deterministic**  Same input always produces same output
2. **Explicit Deny Precedence**  DENY overrides ALLOW (least privilege)
3. **Tenant Isolation**  Multi-tenancy is mandatory, not optional
4. **Engine/Application Separation**  Authorization engine decoupled from transport
5. **Explainability**  Every decision must be traceable
6. **ReBAC First**  Recommended model; RBAC is fallback

 [**Full Principles in Product Overview**](docs/product/product-overview.md#9-key-design-principles)

---

##  Project Status

-  Documentation complete (product, architecture, API, deployment)
-  Domain model validated (DDD architecture)
-  Authorization engine designed (ReBAC + RBAC)
-  API contracts defined (RESTful, deterministic)
-  Database schema finalized (multi-tenant, indexed)
-  Implementation ready to begin (from documented blueprint)
-  Tests & CI/CD pipeline (next phase)

---

##  Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

##  Documentation Index

| Document | Audience | Content |
|----------|----------|---------|
| [Product Overview](docs/product/product-overview.md) | Product, stakeholders | Vision, capabilities, use cases |
| [Core Concepts](docs/concepts/core-concepts-tuple-model.md) | Engineers, architects | Tuple model, ReBAC, RBAC, evaluation |
| [API Reference](docs/reference/api-reference.md) | Developers, integrators | Endpoint documentation, examples |
| [Getting Started](docs/guides/getting-started-development.md) | Developers | Local setup, development workflow |
| [Deployment Guide](docs/guides/deployment-operations-guide.md) | DevOps, SRE | Infra, monitoring, operations |
| [Architecture](docs/architecture/) | Architects, senior devs | Project structure, patterns, design |

---

##  Local Development

```bash
# 1. Clone & navigate
cd D:\Workspace\Aegis

# 2. Start PostgreSQL
docker-compose up -d postgres

# 3. Apply migrations
dotnet ef database update -p src/Aegis.Api

# 4. Run API
cd src/Aegis.Api && dotnet run

# 5. Run tests
dotnet test --configuration Release
```

 [**Detailed Getting Started**](docs/guides/getting-started-development.md)

---

##  Support

- **Questions?** Open a GitHub Discussion
- **Bug Report?** Open a GitHub Issue
- **Contributing?** See [CONTRIBUTING.md](CONTRIBUTING.md)

---

##  License

This project is licensed under [LICENSE](LICENSE).

---

**Ready to get started?**  
 [Start with Product Overview](docs/product/product-overview.md) or [Jump to Getting Started](docs/guides/getting-started-development.md)
