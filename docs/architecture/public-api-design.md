# Public API Design

This document defines the target public API shape for Aegis. It should guide the evolution of the current controllers and contracts while preserving compatibility where needed.

## Design Principles

- APIs are versioned under `/api/v1`.
- Tenant context is mandatory.
- Store-scoped APIs are the canonical authorization surface.
- Request and response bodies use stable DTOs from `Aegis.Contracts`.
- Decision APIs are optimized for service-to-service use.
- Management APIs prioritize safety, idempotency, and auditability.
- Explainability is a first-class API contract.

## Tenant Context

The preferred tenant context is authenticated identity plus `X-Tenant-Id`.

```http
X-Tenant-Id: tenant-a
Authorization: Bearer <token>
```

For local development, header-only tenant context may be allowed.

If both authenticated tenant and header/query tenant are present, they must match.

## Canonical Store-Scoped APIs

### Check

```http
POST /api/v1/stores/{storeId}/check
```

Request:

```json
{
  "subject": "user:alice",
  "relation": "viewer",
  "object": "document:roadmap",
  "authorizationModelId": "model_01J...",
  "consistency": "minimize_latency",
  "contextualTuples": [],
  "context": {
    "ip": "10.0.0.12",
    "feature_enabled": true
  }
}
```

Response:

```json
{
  "allowed": true,
  "decision": "ALLOW",
  "reasonCode": "ALLOW_REBAC_REWRITE",
  "tenantId": "tenant-a",
  "storeId": "store-main",
  "authorizationModelId": "model_01J...",
  "revision": 1842
}
```

### Batch Check

```http
POST /api/v1/stores/{storeId}/batch-check
```

Batch items must carry a client-supplied or server-generated `correlationId`.

### Explain

```http
POST /api/v1/stores/{storeId}/explain
```

Response:

```json
{
  "allowed": false,
  "decision": "DENY",
  "reasonCode": "DENY_NOT_FOUND",
  "tenantId": "tenant-a",
  "storeId": "store-main",
  "authorizationModelId": "model_01J...",
  "revision": 1842,
  "explanation": {
    "stage": "decision",
    "result": "DENY",
    "children": [
      {
        "stage": "deny_policy",
        "result": "NOT_MATCHED"
      },
      {
        "stage": "rebac",
        "result": "NOT_MATCHED",
        "relation": "viewer",
        "object": "document:roadmap"
      },
      {
        "stage": "rbac_fallback",
        "result": "NOT_MATCHED"
      }
    ]
  }
}
```

### Query APIs

```http
POST /api/v1/stores/{storeId}/list-users
POST /api/v1/stores/{storeId}/list-objects
POST /api/v1/stores/{storeId}/expand
```

These APIs are more expensive than `check` and should include traversal limits, pagination, and clear partial-result semantics.

## Relationship APIs

### Write Tuples

```http
POST /api/v1/stores/{storeId}/relationships/write
Idempotency-Key: write-123
```

Request:

```json
{
  "writes": [
    {
      "subject": "user:alice",
      "relation": "viewer",
      "object": "document:roadmap",
      "effect": "allow"
    }
  ],
  "deletes": []
}
```

Response:

```json
{
  "revision": 1843,
  "written": 1,
  "deleted": 0
}
```

### Read Tuples

```http
POST /api/v1/stores/{storeId}/relationships/read
```

Request:

```json
{
  "subject": "user:alice",
  "relation": "viewer",
  "object": "document:roadmap",
  "effect": "allow",
  "pageSize": 100,
  "continuationToken": null
}
```

### Change Feed

```http
GET /api/v1/stores/{storeId}/relationships/changes?afterRevision=1842&limit=1000
```

Use revision cursors instead of offset pagination.

## Authorization Model APIs

```http
POST /api/v1/stores/{storeId}/authorization-models
GET  /api/v1/stores/{storeId}/authorization-models
GET  /api/v1/stores/{storeId}/authorization-models/{modelId}
POST /api/v1/stores/{storeId}/authorization-models/{modelId}/validate
POST /api/v1/stores/{storeId}/authorization-models/{modelId}/activate
```

Activation should return the new store revision.

## Tenant and Store APIs

```http
POST /api/v1/tenants
GET  /api/v1/tenants/{tenantId}
POST /api/v1/tenants/{tenantId}/stores
GET  /api/v1/tenants/{tenantId}/stores
GET  /api/v1/tenants/{tenantId}/stores/{storeId}
```

Tenant creation may be disabled in hosted deployments and handled by an operator or control plane.

## RBAC Fallback APIs

RBAC should be scoped by tenant and store.

```http
POST /api/v1/stores/{storeId}/roles
GET  /api/v1/stores/{storeId}/roles
POST /api/v1/stores/{storeId}/roles/{roleName}/grants
POST /api/v1/stores/{storeId}/users/{userId}/roles
GET  /api/v1/stores/{storeId}/users/{userId}/roles
```

## Error Shape

Use a stable error envelope:

```json
{
  "error": {
    "code": "TENANT_REQUIRED",
    "message": "Tenant context is required.",
    "target": "X-Tenant-Id",
    "details": []
  },
  "requestId": "0HN..."
}
```

## Compatibility APIs

The current global endpoints may remain temporarily:

```http
POST /api/v1/check
POST /api/v1/explain
POST /api/v1/relationships
```

They should be documented as compatibility APIs once store-scoped endpoints are complete.

