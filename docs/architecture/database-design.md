# Database Design - Aegis Authorization Platform

## 1. Overview

This document defines the data model for Aegis authorization, with ReBAC as the primary model and RBAC as supplemental fallback.

Design goals:

- strict tenant isolation
- read-optimized permission checks
- graph-ready tuple schema
- operational clarity for auditing/debugging

Decision posture:

- canonical tuple identity (`subject`, `relation`, `object`)
- single relationship table with `effect` (`allow` or `deny`)
- deterministic evaluation: deny before allow

---

## 2. Core Tables (RBAC)

```sql
Tenants (
    Id UUID PRIMARY KEY,
    Name TEXT NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL,
    Status INT NOT NULL
)

Users (
    Id UUID PRIMARY KEY,
    TenantId UUID NOT NULL,
    Username TEXT NOT NULL,
    Email TEXT,
    PasswordHash TEXT,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL,
    Status INT NOT NULL,
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
)

Roles (
    Id UUID PRIMARY KEY,
    TenantId UUID NOT NULL,
    Name TEXT NOT NULL,
    Description TEXT,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL,
    Status INT NOT NULL,
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
)

Permissions (
    Id UUID PRIMARY KEY,
    Name TEXT NOT NULL, -- example: document:edit
    Description TEXT,
    Scope TEXT,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL,
    Status INT NOT NULL,
    UNIQUE (Name)
)

UserRoles (
    TenantId UUID NOT NULL,
    UserId UUID NOT NULL,
    RoleId UUID NOT NULL,
    PRIMARY KEY (TenantId, UserId, RoleId),
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
)

RolePermissions (
    TenantId UUID NOT NULL,
    RoleId UUID NOT NULL,
    PermissionId UUID NOT NULL,
    PRIMARY KEY (TenantId, RoleId, PermissionId),
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id),
    FOREIGN KEY (PermissionId) REFERENCES Permissions(Id)
)
```

---

## 3. ReBAC Tuple Store (Primary)

### 3.1 Relationships

```sql
Relationships (
    Id UUID PRIMARY KEY,
    TenantId UUID NOT NULL,

    Subject TEXT NOT NULL,          -- user:1, team:dev
    Relation TEXT NOT NULL,         -- owner, editor, viewer, member
    Object TEXT NOT NULL,           -- document:10, repo:1
    Effect TEXT NOT NULL DEFAULT 'allow',

    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL,

    FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    CHECK (Effect IN ('allow', 'deny')),
    UNIQUE (TenantId, Subject, Relation, Object)
)
```

Canonical tuple:

- `(subject, relation, object)`

Examples:

- `(user:1, owner, document:10)`
- `(team:dev, member, user:1)`
- `(team:dev, owner, repo:1)`

Effect examples:

- `allow` for standard authorization tuples
- `deny` for explicit deny tuples with higher precedence at evaluation time

---

## 4. Object Namespace Convention

Required naming format:

- subject: `<type>:<id>`
- object: `<type>:<id>`

Examples:

- `user:1`
- `team:dev`
- `document:10`

This convention is mandatory for API consistency, logging, and explain output.

---

## 5. Audit and Explain Logs

```sql
AuditLogs (
    Id UUID PRIMARY KEY,
    TenantId UUID NOT NULL,
    Subject TEXT,
    Action TEXT,
    Target TEXT,
    Decision TEXT,          -- ALLOW / DENY
    ReasonCode TEXT,        -- ALLOW_REBAC_DIRECT, DENY_EXPLICIT, etc.
    Metadata JSONB,
    CreatedAt TIMESTAMP NOT NULL
)
```

---

## 6. Indexing Strategy

### 6.1 ReBAC Check (Hot Path)

```sql
CREATE INDEX idx_rel_check
ON Relationships (TenantId, Subject, Relation, Object);
```

### 6.2 Reverse Traversal (for explain/graph expansion)

```sql
CREATE INDEX idx_rel_object_lookup
ON Relationships (TenantId, Object, Relation, Subject);
```

### 6.3 Subject Expansion (for recursive traversal)

```sql
CREATE INDEX idx_rel_subject
ON Relationships (TenantId, Subject);
```

### 6.4 Optional Partial Index (hot relation)

```sql
CREATE INDEX idx_rel_owner
ON Relationships (TenantId, Subject, Object)
WHERE Relation = 'owner';
```

### 6.5 RBAC Lookup

```sql
CREATE INDEX idx_user_roles_user
ON UserRoles (TenantId, UserId);

CREATE INDEX idx_role_permissions_role
ON RolePermissions (TenantId, RoleId);

CREATE INDEX idx_permissions_name
ON Permissions (Name);
```

---

## 7. Query Patterns

### 7.1 ReBAC Direct Check

```sql
SELECT Effect
FROM Relationships
WHERE TenantId = @tenantId
    AND Subject = @subject
  AND Relation = @relation
    AND Object = @object
LIMIT 1;
```

Evaluation logic:

- if `Effect = deny` -> `DENY_EXPLICIT`
- if `Effect = allow` -> `ALLOW_REBAC_DIRECT`
- otherwise -> continue RBAC fallback

### 7.2 RBAC Fallback Check

```sql
SELECT 1
FROM UserRoles ur
JOIN RolePermissions rp
  ON ur.TenantId = rp.TenantId
 AND ur.RoleId = rp.RoleId
JOIN Permissions p
  ON rp.PermissionId = p.Id
WHERE ur.TenantId = @tenantId
  AND ur.UserId = @userId
  AND p.Name = @permission
LIMIT 1;
```

---

## 8. Multi-Tenancy Rules

Hard constraints:

- all mutable auth tables include `TenantId`
- all read/write queries include `TenantId`
- no cross-tenant joins in permission path

---

## 9. MVP vs Next Phase

### MVP

- direct tuple check
- explicit deny via `Effect = deny`
- RBAC fallback

### Next phase

- recursive graph traversal
- relation inheritance/computed relations
- policy/model versioning per tenant

---

## 10. Summary

This schema establishes a real ReBAC foundation through tuple modeling while preserving RBAC interoperability and production-safe tenant isolation.

It is intentionally aligned with canonical tuple systems so cache keys, API payloads, and query patterns share the same identity format.
