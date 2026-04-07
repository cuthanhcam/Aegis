# API Reference  Aegis Authorization Platform

---

## Overview

All Aegis APIs are:
- **Tenant-scoped** (require `X-Tenant-Id` header)
- **RESTful** with JSON request/response
- **Versioned** under `/api/v1`
- **Authenticated** (require JWT token unless disabled)

---

## Base URL

```
http://localhost:5000/api/v1
```

---

## Authentication

### Header: X-Tenant-Id

**Required** for all permission-related calls:

```http
X-Tenant-Id: tenant-123
```

---

### Header: Authorization (Optional)

If JWT is enabled:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

## 1. Permission Check API

### POST `/check`

Evaluate whether a permission is allowed.

**Request:**

```http
POST /api/v1/check
X-Tenant-Id: tenant-123
Content-Type: application/json

{
  "subject": "user:alice",
  "relation": "editor",
  "object": "document:report-2024"
}
```

**Response (200 OK):**

```json
{
  "allowed": true,
  "decision": "ALLOW",
  "reasonCode": "ALLOW_REBAC_DIRECT"
}
```

**Response (200 OK - Denied):**

```json
{
  "allowed": false,
  "decision": "DENY",
  "reasonCode": "DENY_NOT_FOUND"
}
```

**Reason Codes:**

| Code | Meaning |
|------|---------|
| `ALLOW_REBAC_DIRECT` | Direct tuple matched in ReBAC |
| `ALLOW_RBAC_ROLE` | RBAC role permission matched |
| `DENY_EXPLICIT` | Explicit deny tuple matched |
| `DENY_NOT_FOUND` | No allow rule matched |
| `DENY_INVALID_INPUT` | Malformed request |

**Error (400 Bad Request):**

```json
{
  "error": "Invalid tuple format",
  "details": "Subject must be <type>:<id>"
}
```

---

## 2. Explain API (Debugging)

### POST `/explain`

Get detailed trace of how a permission decision was made.

**Request:**

```http
POST /api/v1/explain
X-Tenant-Id: tenant-123

{
  "subject": "user:alice",
  "relation": "editor",
  "object": "document:report"
}
```

**Response (200 OK):**

```json
{
  "allowed": true,
  "decision": "ALLOW",
  "reasonCode": "ALLOW_REBAC_DIRECT",
  "trace": [
    {
      "step": "VALIDATE_INPUT",
      "result": "SUCCESS",
      "details": "Tuple format valid"
    },
    {
      "step": "CHECK_DENY_POLICY",
      "result": "NOT_MATCHED",
      "details": "No explicit deny rules found"
    },
    {
      "step": "CHECK_REBAC_DIRECT",
      "result": "MATCHED",
      "details": "Tuple (user:alice, editor, document:report) found with effect=allow",
      "tuple": {
        "subject": "user:alice",
        "relation": "editor",
        "object": "document:report",
        "effect": "allow"
      }
    },
    {
      "step": "FINAL_DECISION",
      "result": "ALLOW"
    }
  ]
}
```

**Use Cases:**
-  **Debugging access issues**  Why was a user denied?
-  **Compliance audits**  How was this decision made?
-  **Testing authorization logic**  Validate your rules work

---

## 3. Relationships API (CRUD)

### POST `/relationships`

Create a new relationship tuple.

**Request:**

```http
POST /api/v1/relationships
X-Tenant-Id: tenant-123

{
  "subject": "user:bob",
  "relation": "viewer",
  "object": "document:report",
  "effect": "allow"
}
```

**Response (201 Created):**

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "tenantId": "tenant-123",
  "subject": "user:bob",
  "relation": "viewer",
  "object": "document:report",
  "effect": "allow",
  "createdAt": "2026-04-07T10:30:00Z",
  "updatedAt": "2026-04-07T10:30:00Z"
}
```

**Notes:**
- `effect` defaults to `allow` if omitted
- Request is **idempotent**  creating the same tuple twice returns 201 both times

---

### GET `/relationships`

Retrieve relationships (with filtering).

**Request:**

```http
GET /api/v1/relationships?subject=user:alice&object=document:report
X-Tenant-Id: tenant-123
```

**Response (200 OK):**

```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "subject": "user:alice",
      "relation": "owner",
      "object": "document:report",
      "effect": "allow",
      "createdAt": "2026-04-07T09:00:00Z"
    },
    {
      "id": "660e8400-e29b-41d4-a716-446655440001",
      "subject": "user:alice",
      "relation": "editor",
      "object": "document:report",
      "effect": "allow",
      "createdAt": "2026-04-07T09:30:00Z"
    }
  ],
  "total": 2,
  "page": 1,
  "pageSize": 50
}
```

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `subject` | string | Filter by subject |
| `relation` | string | Filter by relation |
| `object` | string | Filter by object |
| `effect` | string | Filter by effect (allow/deny) |
| `page` | number | Page number (default: 1) |
| `pageSize` | number | Items per page (default: 50, max: 1000) |

---

### DELETE `/relationships/{id}`

Remove a relationship tuple.

**Request:**

```http
DELETE /api/v1/relationships/550e8400-e29b-41d4-a716-446655440000
X-Tenant-Id: tenant-123
```

**Response (204 No Content):**

```
[empty response]
```

---

### DELETE `/relationships`

Delete relationships by filter (batch delete).

**Request:**

```http
DELETE /api/v1/relationships?subject=user:alice&relation=viewer
X-Tenant-Id: tenant-123
```

**Response (200 OK):**

```json
{
  "deletedCount": 3,
  "message": "3 relationships deleted"
}
```

---

## 4. Users API

### POST `/users`

Create a new user.

**Request:**

```http
POST /api/v1/users
X-Tenant-Id: tenant-123

{
  "username": "alice",
  "email": "alice@company.com",
  "password": "secure-password"
}
```

**Response (201 Created):**

```json
{
  "id": "user:uuid-here",
  "tenantId": "tenant-123",
  "username": "alice",
  "email": "alice@company.com",
  "createdAt": "2026-04-07T10:30:00Z"
}
```

---

### GET `/users`

List users in the tenant.

**Request:**

```http
GET /api/v1/users?page=1&pageSize=50
X-Tenant-Id: tenant-123
```

**Response:**

```json
{
  "data": [
    {
      "id": "user-uuid-1",
      "username": "alice",
      "email": "alice@company.com"
    }
  ],
  "total": 1,
  "page": 1
}
```

---

### GET `/users/{userId}`

Get user details.

**Request:**

```http
GET /api/v1/users/user-uuid-1
X-Tenant-Id: tenant-123
```

**Response:**

```json
{
  "id": "user-uuid-1",
  "username": "alice",
  "email": "alice@company.com",
  "createdAt": "2026-04-07T08:00:00Z",
  "updatedAt": "2026-04-07T10:00:00Z"
}
```

---

## 5. Roles API (RBAC)

### POST `/roles`

Create a role.

**Request:**

```http
POST /api/v1/roles
X-Tenant-Id: tenant-123

{
  "name": "document-editor",
  "description": "Can edit documents"
}
```

**Response (201 Created):**

```json
{
  "id": "role-uuid-1",
  "tenantId": "tenant-123",
  "name": "document-editor",
  "description": "Can edit documents",
  "createdAt": "2026-04-07T10:30:00Z"
}
```

---

### GET `/roles`

List roles.

**Request:**

```http
GET /api/v1/roles
X-Tenant-Id: tenant-123
```

**Response:**

```json
{
  "data": [
    {
      "id": "role-uuid-1",
      "name": "document-editor",
      "description": "Can edit documents"
    },
    {
      "id": "role-uuid-2",
      "name": "viewer",
      "description": "Can view only"
    }
  ],
  "total": 2
}
```

---

### POST `/roles/{roleId}/permissions/{permissionId}`

Assign permission to role.

**Request:**

```http
POST /api/v1/roles/role-uuid-1/permissions/perm-uuid-1
X-Tenant-Id: tenant-123
```

**Response (201 Created):**

```json
{
  "roleId": "role-uuid-1",
  "permissionId": "perm-uuid-1"
}
```

---

## 6. Permissions API

### POST `/permissions`

Create a permission (system-level, not tenant-scoped).

**Request:**

```http
POST /api/v1/permissions

{
  "name": "document:edit",
  "description": "Can edit documents",
  "scope": "document"
}
```

**Response (201 Created):**

```json
{
  "id": "perm-uuid-1",
  "name": "document:edit",
  "description": "Can edit documents",
  "scope": "document"
}
```

---

### GET `/permissions`

List all permissions (system-level).

**Response:**

```json
{
  "data": [
    {
      "id": "perm-uuid-1",
      "name": "document:edit"
    },
    {
      "id": "perm-uuid-2",
      "name": "document:delete"
    }
  ]
}
```

---

## 7. User Roles (RBAC Assignment)

### POST `/user-roles`

Assign role to user.

**Request:**

```http
POST /api/v1/user-roles
X-Tenant-Id: tenant-123

{
  "userId": "user-uuid-1",
  "roleId": "role-uuid-1"
}
```

**Response (201 Created):**

```json
{
  "userId": "user-uuid-1",
  "roleId": "role-uuid-1",
  "createdAt": "2026-04-07T10:30:00Z"
}
```

---

### DELETE `/user-roles/{userId}/{roleId}`

Remove role from user.

**Request:**

```http
DELETE /api/v1/user-roles/user-uuid-1/role-uuid-1
X-Tenant-Id: tenant-123
```

**Response (204 No Content):**

---

## 8. Audit Logs API

### GET `/audit-logs`

Retrieve audit trail of permission changes.

**Request:**

```http
GET /api/v1/audit-logs?limit=100&offset=0
X-Tenant-Id: tenant-123
```

**Response:**

```json
{
  "data": [
    {
      "id": "audit-uuid-1",
      "timestamp": "2026-04-07T10:30:00Z",
      "action": "RELATIONSHIP_CREATED",
      "subject": "user:alice",
      "relation": "editor",
      "object": "document:report",
      "effect": "allow",
      "initiatedBy": "user:admin"
    },
    {
      "id": "audit-uuid-2",
      "timestamp": "2026-04-07T10:25:00Z",
      "action": "RELATIONSHIP_DELETED",
      "subject": "user:bob",
      "relation": "viewer",
      "object": "document:report",
      "initiatedBy": "user:admin"
    }
  ],
  "total": 2
}
```

**Audit Actions:**

| Action | Meaning |
|--------|---------|
| `RELATIONSHIP_CREATED` | Tuple was created |
| `RELATIONSHIP_UPDATED` | Tuple effect was changed |
| `RELATIONSHIP_DELETED` | Tuple was removed |
| `ROLE_ASSIGNED` | User was assigned a role |
| `ROLE_REMOVED` | Role was removed from user |
| `PERMISSION_CHECK` | Permission was checked |

---

## 9. Stores API

### POST `/stores`

Create a new authorization store.

**Request:**

```http
POST /api/v1/stores
X-Tenant-Id: tenant-123

{
  "name": "document-service-store"
}
```

**Response (201 Created):**

```json
{
  "id": "store-123",
  "name": "document-service-store",
  "tenantId": "tenant-123",
  "createdAt": "2026-04-07T10:30:00Z"
}
```

---

### GET `/stores`

List stores in tenant.

**Request:**

```http
GET /api/v1/stores
X-Tenant-Id: tenant-123
```

**Response:**

```json
{
  "data": [
    {
      "id": "store-123",
      "name": "document-service-store",
      "createdAt": "2026-04-07T08:00:00Z"
    }
  ],
  "total": 1
}
```

---

## 10. Authorization Models API

### POST `/stores/{storeId}/models`

Create/update authorization model schema for a store.

**Request:**

```http
POST /api/v1/stores/store-123/models
X-Tenant-Id: tenant-123

{
  "schemaVersion": "1.0.0",
  "model": {
    "types": ["user", "team", "document"],
    "relations": {
      "owner": { "types": ["user", "team"] },
      "editor": { "types": ["user", "team"] },
      "viewer": { "types": ["user", "team"] }
    }
  }
}
```

**Response:**

```json
{
  "id": "model-uuid-1",
  "storeId": "store-123",
  "schemaVersion": "1.0.0",
  "createdAt": "2026-04-07T10:30:00Z"
}
```

---

### GET `/stores/{storeId}/models`

Get latest model for a store.

**Response:**

```json
{
  "id": "model-uuid-1",
  "storeId": "store-123",
  "schemaVersion": "1.0.0",
  "model": { ... }
}
```

---

## Error Responses

### 400 Bad Request

```json
{
  "error": "INVALID_REQUEST",
  "message": "Invalid tuple format",
  "details": "Subject must be formatted as <type>:<id>"
}
```

### 401 Unauthorized

```json
{
  "error": "UNAUTHORIZED",
  "message": "Missing or invalid JWT token"
}
```

### 403 Forbidden

```json
{
  "error": "FORBIDDEN",
  "message": "You do not have permission to access this resource"
}
```

### 404 Not Found

```json
{
  "error": "NOT_FOUND",
  "message": "Relationship not found"
}
```

### 500 Internal Server Error

```json
{
  "error": "INTERNAL_ERROR",
  "message": "An unexpected error occurred",
  "requestId": "550e8400-e29b-41d4-a716-446655440000"
}
```

---

## Rate Limiting

Aegis implements rate limiting to prevent abuse:

```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 2026-04-07T11:30:00Z
```

If rate limited (429):

```json
{
  "error": "RATE_LIMITED",
  "message": "Too many requests",
  "retryAfter": 60
}
```

---

## Pagination

List endpoints support pagination:

```http
GET /api/v1/relationships?page=2&pageSize=50
```

**Response:**

```json
{
  "data": [...],
  "pagination": {
    "page": 2,
    "pageSize": 50,
    "total": 250,
    "hasMore": true
  }
}
```

---

## Idempotency

Aegis provides **idempotent** operations for safety:

```http
POST /api/v1/relationships
Idempotency-Key: unique-key-123

{
  "subject": "user:alice",
  "relation": "editor",
  "object": "document:x"
}
```

Sending the same request with the same `Idempotency-Key` returns the same response, even if called multiple times.

---

## OpenAPI/Swagger

Access interactive API documentation:

```
http://localhost:5000/swagger
```

---

## Examples

### Example 1: Complete Permission Check Workflow

```bash
# 1. Create a relationship
curl -X POST http://localhost:5000/api/v1/relationships \
  -H "X-Tenant-Id: tenant-123" \
  -H "Content-Type: application/json" \
  -d '{
    "subject": "user:alice",
    "relation": "editor",
    "object": "document:report"
  }'

# 2. Check permission
curl -X POST http://localhost:5000/api/v1/check \
  -H "X-Tenant-Id: tenant-123" \
  -d '{
    "subject": "user:alice",
    "relation": "editor",
    "object": "document:report"
  }'

# Response: { "allowed": true, "decision": "ALLOW", ... }

# 3. Explain the decision
curl -X POST http://localhost:5000/api/v1/explain \
  -H "X-Tenant-Id: tenant-123" \
  -d '{
    "subject": "user:alice",
    "relation": "editor",
    "object": "document:report"
  }'
```

---

## Next Steps

-  Review [Core Concepts](../concepts/core-concepts-tuple-model.md)
-  See [Getting Started](../guides/getting-started-development.md)
-  Check [Deployment Guide](../guides/deployment-operations-guide.md)
