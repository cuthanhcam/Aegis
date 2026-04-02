# API Specification - Aegis Authorization Platform

## 1. Overview

This document defines the public authorization API contract for Aegis.

Principles:

- RESTful and versioned (`/api/v1`)
- tenant-aware
- deterministic decisions
- explainable responses

---

## 2. Authentication

### POST /api/v1/auth/login

Request:

```json
{
  "username": "user",
  "password": "password"
}
```

Response:

```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "expiresIn": 3600
}
```

---

## 3. Authorization Decision API

### POST /api/v1/check

Evaluates whether `(subject, relation, object)` is allowed under tenant context.

Request:

```json
{
  "subject": "user:1",
  "relation": "viewer",
  "object": "document:10"
}
```

Response:

```json
{
  "allowed": true,
  "decision": "ALLOW",
  "reasonCode": "ALLOW_REBAC_DIRECT"
}
```

Evaluation order:

1. explicit deny rules
2. ReBAC allow
3. RBAC allow
4. default deny

---

## 4. Explain API

### POST /api/v1/explain

Returns a trace that explains how the decision was made.

Request:

```json
{
  "subject": "user:1",
  "relation": "viewer",
  "object": "document:10"
}
```

Response:

```json
{
  "allowed": true,
  "decision": "ALLOW",
  "reasonCode": "ALLOW_REBAC_DIRECT",
  "trace": [
    {
      "step": "DENY_POLICY",
      "result": "NOT_MATCHED"
    },
    {
      "step": "REBAC_DIRECT",
      "result": "MATCHED",
      "tuple": "(user:1, viewer, document:10)"
    },
    {
      "step": "FINAL",
      "result": "ALLOW"
    }
  ]
}
```

---

## 5. Relationships API

### POST /api/v1/relationships

Creates a tuple.

Request:

```json
{
  "subject": "team:dev",
  "relation": "owner",
  "object": "repo:1",
  "effect": "allow"
}
```

Notes:

- `effect` supports `allow` and `deny`
- omit `effect` to use default `allow`

### DELETE /api/v1/relationships

Removes a tuple.

Request:

```json
{
  "subject": "team:dev",
  "relation": "owner",
  "object": "repo:1"
}
```

### GET /api/v1/relationships

Query tuples.

Example:

```http
GET /api/v1/relationships?subject=user:1&relation=viewer&object=document:10
```

Filter by effect:

```http
GET /api/v1/relationships?subject=user:1&relation=viewer&object=document:10&effect=deny
```

---

## 6. RBAC APIs

### Roles

```http
GET  /api/v1/roles
POST /api/v1/roles
```

### Permissions

```http
GET  /api/v1/permissions
POST /api/v1/permissions
```

### Assign Role

```http
POST /api/v1/users/{userId}/roles
```

---

## 7. Explicit Deny Representation

Explicit deny is represented as a relationship tuple with `effect = deny`.

Example:

```json
{
  "subject": "user:1",
  "relation": "viewer",
  "object": "document:10",
  "effect": "deny"
}
```

Evaluation precedence remains:

1. `deny` effect tuples
2. `allow` effect tuples
3. RBAC fallback
4. default deny

---

## 8. Audit API

### GET /api/v1/audit

Query decision logs.

```http
GET /api/v1/audit?action=check&decision=DENY
```

---

## 9. Resource Naming Conventions

Required format:

- subject: `<type>:<id>`
- object: `<type>:<id>`

Examples:

- `user:1`
- `team:dev`
- `document:10`

---

## 10. Response Envelope

Success:

```json
{
  "success": true,
  "data": {}
}
```

Error:

```json
{
  "success": false,
  "error": {
    "code": "FORBIDDEN",
    "message": "Access denied"
  }
}
```

---

## 11. Security and Tenant Enforcement

- JWT required for protected endpoints
- tenant context extracted from token/claims
- all tuple operations are tenant-scoped
- cross-tenant tuple references are rejected

---

## 12. Versioning

Current:

- `/api/v1/...`

Future:

- `/api/v2/...`

---

## 13. Future Extensions

- batch check API (`/check:batch`)
- graph traversal query API
- model schema endpoint for computed relations
- conditional policies (ABAC-like constraints)
