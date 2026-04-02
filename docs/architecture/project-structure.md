# Project Structure Blueprint - Aegis Authorization Platform

## 1. Purpose

This document defines a production-ready project structure for Aegis as an authorization platform, with ReBAC as a first-class engine capability and RBAC as a complementary model.

It is designed to help teams:

- start implementation immediately without major structural refactors
- keep authorization engine logic isolated from transport and persistence concerns
- scale from MVP to graph-aware authorization safely

---

## 2. Design Targets

The structure is optimized for these outcomes:

- clear engine boundaries
- deterministic permission checks
- tenant-safe data paths
- extensible architecture for explainability and recursion

Platform mindset:

- not just Web API + business logic
- an authorization platform that can serve multiple services

---

## 3. Recommended Repository Layout

```text
Aegis/
├── src/
├── tests/
├── docs/
├── docker/
├── tools/
└── Aegis.sln
```

Notes:

- `docs/` keeps architecture, API, and operational decisions
- `tools/` contains scripts and local ops helpers
- `docker/` contains local runtime dependencies and compose assets

---

## 4. Source Layout

```text
src/
├── Aegis.Api/              # HTTP entry point
├── Aegis.Application/      # Use cases and orchestration
├── Aegis.Domain/           # Core domain entities/value objects
├── Aegis.Infrastructure/   # DB/cache/external implementations
├── Aegis.Authorization/    # Authorization engine (ReBAC core)
├── Aegis.Contracts/        # DTOs and API contracts
└── Aegis.SharedKernel/     # Common primitives and cross-cutting types
```

---

## 5. Authorization Module (Critical Boundary)

`Aegis.Authorization` is the heart of the system.

### 5.1 Internal layout

```text
Aegis.Authorization/
├── Core/
│   ├── Models/
│   │   ├── Subject.cs
│   │   ├── ObjectRef.cs
│   │   ├── RelationshipTuple.cs    # (subject, relation, object)
│   │   ├── CheckRequest.cs
│   │   └── DecisionResult.cs
│   ├── Interfaces/
│   │   ├── IAuthorizationEngine.cs
│   │   ├── IRelationshipStore.cs
│   │   ├── IRbacProvider.cs
│   │   ├── IDenyPolicyProvider.cs
│   │   └── IExplainService.cs
│   ├── Engine/
│   │   ├── AuthorizationEngine.cs
│   │   ├── CheckEvaluator.cs
│   │   └── RelationResolver.cs
│   └── Exceptions/
├── ReBAC/
│   ├── Evaluators/
│   │   ├── DirectRelationEvaluator.cs
│   │   └── RecursiveEvaluator.cs   # planned
│   └── Graph/
│       └── RelationshipGraph.cs
├── RBAC/
│   └── RbacPermissionEvaluator.cs
└── Caching/
      └── AuthorizationCache.cs
```

### 5.2 Non-negotiable rule

The authorization engine must not depend on:

- EF Core
- SQL/Redis client details
- HTTP framework concerns

Engine responsibility:

- evaluate input tuple and return decision

---

## 6. Domain Layer

```text
Aegis.Domain/
├── Entities/
│   ├── Tenant.cs
│   ├── User.cs
│   ├── Role.cs
│   └── Permission.cs
├── ValueObjects/
│   ├── SubjectId.cs
│   └── ObjectId.cs
├── Enums/
└── Interfaces/
```

Guideline:

- keep business invariants and domain types here
- avoid embedding engine traversal logic in domain entities

---

## 7. Application Layer

```text
Aegis.Application/
├── Interfaces/
│   ├── IAuthService.cs
│   ├── IRelationshipService.cs
│   └── IPermissionService.cs
├── Features/
│   ├── Auth/
│   ├── Users/
│   ├── Roles/
│   ├── Permissions/
│   └── Relationships/
│       ├── Commands/
│       └── Queries/
├── Services/
│   └── PermissionAppService.cs
└── Behaviors/
```

Application rule:

- Application orchestrates use cases
- Application delegates permission evaluation to `IAuthorizationEngine`

---

## 8. Infrastructure Layer

```text
Aegis.Infrastructure/
├── Persistence/
│   ├── AegisDbContext.cs
│   ├── Configurations/
│   ├── Migrations/
│   └── Repositories/
├── Authorization/
│   ├── EfRelationshipStore.cs
│   ├── RbacProvider.cs
│   └── DenyPolicyProvider.cs
├── Caching/
│   └── RedisCacheProvider.cs
├── Identity/
│   └── JwtProvider.cs
└── DependencyInjection.cs
```

Infrastructure rule:

- implement interfaces owned by `Aegis.Authorization` or `Aegis.Application`

---

## 9. API Layer

```text
Aegis.Api/
├── Controllers/
│   ├── CheckController.cs
│   ├── ExplainController.cs
│   ├── RelationshipsController.cs
│   ├── RolesController.cs
│   └── PermissionsController.cs
├── Middlewares/
│   ├── TenantMiddleware.cs
│   └── ExceptionMiddleware.cs
├── Filters/
├── Extensions/
├── Program.cs
└── appsettings.json
```

API rule:

- transport only
- no authorization algorithm in controllers

---

## 10. Test Layout

```text
tests/
├── Aegis.UnitTests/
│   ├── Authorization/
│   └── Domain/
└── Aegis.IntegrationTests/
      ├── ApiTests/
      └── PersistenceTests/
```

Test priorities:

- direct ReBAC tuple checks
- deny precedence over allow
- RBAC fallback behavior
- explain trace correctness

---

## 11. Supporting Folders

```text
docker/
├── docker-compose.yml
├── postgres/
└── redis/

tools/
├── scripts/
│   ├── migrate.ps1
│   └── run-local.ps1
└── seed-data/
```

---

## 12. Dependency Direction

Target dependency flow:

```text
Api -> Application -> Authorization
                    -> Domain
Infrastructure -> implements interfaces from Authorization/Application
```

Key intent:

- authorization engine remains stable while transport and storage can evolve

---

## 13. Runtime Check Flow

```text
Client -> API /check
      -> Application use case
      -> AuthorizationEngine.CheckAsync
      -> DenyPolicyProvider
      -> RelationshipStore (ReBAC)
      -> RbacProvider (fallback)
      -> DecisionResult + reason
```

Decision order:

1. explicit deny
2. ReBAC allow
3. RBAC allow
4. default deny

---

## 14. Naming and Data Conventions

Tuple format:

- subject: `<type>:<id>`
- relation: `owner|editor|viewer|member|...`
- object: `<type>:<id>`

Examples:

- `(user:1, owner, document:10)`
- `(team:dev, member, user:1)`

Cache key recommendation:

- `tenant:{tenantId}:subject:{subject}:relation:{relation}:object:{object}:v:{version}`

---

## 15. Implementation Plan (Practical)

### Phase 1: Foundation

- create project boundaries and interfaces
- implement direct tuple storage and `/check`

### Phase 2: Hybrid authorization

- add RBAC provider fallback
- finalize deny precedence

### Phase 3: Explainability and operations

- implement `/explain`
- add audit events and reason codes

### Phase 4: Performance and graph readiness

- add cache versioning
- introduce bounded recursive traversal design

---

## 16. Common Pitfalls to Avoid

- putting permission logic inside controllers/services
- coupling engine to EF entities
- forcing relation-to-permission string mapping
- omitting tenant scope from cache keys
- enabling recursion without max-depth and cycle checks

---

## 17. Adoption Notes

This blueprint is intentionally strict on boundaries so the team can:

- ship an MVP quickly
- keep architecture open for OpenFGA-like capabilities later
- avoid expensive refactors when moving from direct checks to graph-aware checks

---

## 18. Related Documents

- `docs/Overview.md`
- `docs/architecture/permission-engine.md`
- `docs/architecture/database-design.md`
- `docs/architecture/api-spec.md`
