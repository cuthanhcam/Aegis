# Aegis API Reference

This reference reflects the live ASP.NET API surface under `/api/v1`.

## Conventions

Base URL:

```text
http://localhost:5000/api/v1
```

Most Aegis-native endpoints return a standard envelope:

```json
{
  "success": true,
  "data": {},
  "error": null
}
```

OpenFGA-compatible endpoints intentionally return direct OpenFGA-style payloads and errors, without the Aegis envelope.

Authentication:

- Management and store APIs require an authenticated user.
- Tenant-scoped APIs use the tenant claim from the JWT, usually `tenant_id` or `tid`.
- Store-scoped APIs validate that `{storeId}` belongs to the authenticated tenant.
- Development and test setups can use test auth handlers; production should use bearer tokens.

Common error envelope:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "STORE_FORBIDDEN",
    "message": "Store does not belong to the authenticated tenant."
  }
}
```

OpenFGA-compatible error:

```json
{
  "code": "store_forbidden",
  "message": "Store does not belong to the authenticated tenant."
}
```

## Auth

### POST `/auth/login`

```json
{
  "username": "admin",
  "password": "admin123"
}
```

Returns access and refresh tokens in the standard envelope.

### POST `/auth/refresh`

```json
{
  "refreshToken": "refresh-token"
}
```

### GET `/auth/me`

Returns the current authenticated profile.

### POST `/auth/logout`

```json
{
  "refreshToken": "refresh-token"
}
```

### POST `/auth/logout-all`

Revokes all sessions for the current user.

## Tenant Runtime Checks

These endpoints support legacy tenant-oriented checks. Prefer store-scoped endpoints for new integrations.

### POST `/check?tenantId={tenantId}`

```json
{
  "subject": "user:anne",
  "relation": "viewer",
  "object": "document:roadmap",
  "contextualTuples": [],
  "consistency": "fully_consistent",
  "authorizationModelId": "model-id",
  "context": {}
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

### POST `/explain?tenantId={tenantId}`

Uses the same request shape as `/check` and includes `trace` in the response.

```json
{
  "allowed": true,
  "decision": "ALLOW",
  "reasonCode": "ALLOW_REBAC_DIRECT",
  "trace": [
    {
      "step": "CHECK_REBAC_DIRECT",
      "result": "allow",
      "tuple": "user:anne#viewer@document:roadmap"
    }
  ]
}
```

## Stores

### GET `/stores`

Lists stores owned by the authenticated tenant.

### POST `/stores`

```json
{
  "name": "document-service"
}
```

### GET `/stores/{storeId}`

Returns one store if it belongs to the authenticated tenant.

### DELETE `/stores/{storeId}`

Deletes a store and purges store-scoped runtime data where supported.

## Store Checks

### POST `/stores/{storeId}/check`

```json
{
  "user": "user:anne",
  "relation": "viewer",
  "object": "document:roadmap",
  "contextualTuples": [
    {
      "subject": "user:anne",
      "relation": "member",
      "object": "team:eng",
      "effect": "allow"
    }
  ],
  "consistency": "fully_consistent",
  "authorizationModelId": "model-id",
  "context": {
    "region": "apac"
  }
}
```

### POST `/stores/{storeId}/explain`

Uses the same request shape as store check and returns trace details.

### POST `/stores/{storeId}/batch-check`

```json
{
  "items": [
    {
      "user": "user:anne",
      "relation": "viewer",
      "object": "document:roadmap",
      "correlationId": "one"
    }
  ]
}
```

Response:

```json
{
  "results": [
    {
      "correlationId": "one",
      "result": {
        "allowed": true,
        "decision": "ALLOW",
        "reasonCode": "ALLOW_REBAC_DIRECT"
      }
    }
  ]
}
```

## OpenFGA-Compatible Checks

These routes are exposed for OpenFGA-style clients. They return direct payloads.

### POST `/stores/{storeId}/check/compat`

```json
{
  "tuple_key": {
    "user": "user:anne",
    "relation": "viewer",
    "object": "document:roadmap"
  },
  "contextual_tuples": {
    "tuple_keys": [
      {
        "user": "user:anne",
        "relation": "member",
        "object": "team:eng"
      }
    ]
  },
  "authorization_model_id": "model-id",
  "consistency": "fully_consistent",
  "context": {}
}
```

Response:

```json
{
  "allowed": true
}
```

### POST `/stores/{storeId}/batch-check/compat`

```json
{
  "authorization_model_id": "model-id",
  "checks": [
    {
      "correlation_id": "one",
      "tuple_key": {
        "user": "user:anne",
        "relation": "viewer",
        "object": "document:roadmap"
      }
    }
  ]
}
```

Response:

```json
{
  "result": [
    {
      "correlation_id": "one",
      "allowed": true,
      "error": null
    }
  ]
}
```

## Relationships

### GET `/stores/{storeId}/relationships`

Query parameters:

| Name | Description |
| --- | --- |
| `subject` | Optional subject filter, for example `user:anne`. |
| `relation` | Optional relation filter. |
| `object` | Optional object filter, for example `document:roadmap`. |
| `effect` | Optional `allow` or `deny` filter. |

### POST `/stores/{storeId}/relationships`

```json
{
  "subject": "user:anne",
  "relation": "viewer",
  "object": "document:roadmap",
  "effect": "allow"
}
```

### DELETE `/stores/{storeId}/relationships`

```json
{
  "subject": "user:anne",
  "relation": "viewer",
  "object": "document:roadmap"
}
```

### GET `/stores/{storeId}/relationships/changes`

Query parameters:

| Name | Description |
| --- | --- |
| `page_size` | Optional page size. |
| `continuation_token` | Optional continuation token. |
| `type` | Optional change type filter. |

## Authorization Models

### GET `/stores/{storeId}/authorization-models`

Lists model versions for a store.

### GET `/stores/{storeId}/authorization-models/latest`

Returns the latest model for a store.

### GET `/stores/{storeId}/authorization-models/{authorizationModelId}`

Returns one model version.

### POST `/stores/{storeId}/authorization-models`

```json
{
  "schemaVersion": "1.1",
  "dsl": "type user\ntype document\n  define viewer: [user]"
}
```

### POST `/stores/{storeId}/authorization-models/validate`

Validates model DSL without creating a model.

```json
{
  "schemaVersion": "1.1",
  "dsl": "type user\ntype document\n  define viewer: [user]"
}
```

Response:

```json
{
  "valid": true,
  "errors": [],
  "warnings": []
}
```

### PUT `/stores/{storeId}/authorization-models/{authorizationModelId}`

Updates an existing model version.

### DELETE `/stores/{storeId}/authorization-models/{authorizationModelId}`

Deletes a model version.

## Graph Queries

### POST `/stores/{storeId}/graph/list-users`

```json
{
  "relation": "viewer",
  "object": "document:roadmap",
  "authorizationModelId": "model-id",
  "consistency": "fully_consistent"
}
```

Response:

```json
{
  "users": ["user:anne"]
}
```

### POST `/stores/{storeId}/graph/list-objects`

```json
{
  "user": "user:anne",
  "relation": "viewer",
  "type": "document",
  "authorizationModelId": "model-id"
}
```

Response:

```json
{
  "objects": ["document:roadmap"]
}
```

### POST `/stores/{storeId}/graph/expand`

```json
{
  "relation": "viewer",
  "object": "document:roadmap",
  "authorizationModelId": "model-id"
}
```

Response:

```json
{
  "node": "document:roadmap#viewer",
  "kind": "object",
  "users": ["user:anne"],
  "children": []
}
```

## OpenFGA-Compatible Graph Queries

### POST `/stores/{storeId}/graph/list-users/compat`

```json
{
  "object": {
    "type": "document",
    "id": "roadmap"
  },
  "relation": "viewer",
  "user_filters": [],
  "authorization_model_id": "model-id"
}
```

### POST `/stores/{storeId}/graph/list-objects/compat`

```json
{
  "user": "user:anne",
  "relation": "viewer",
  "type": "document",
  "authorization_model_id": "model-id"
}
```

### POST `/stores/{storeId}/graph/expand/compat`

```json
{
  "tuple_key": {
    "user": "",
    "relation": "viewer",
    "object": "document:roadmap"
  },
  "authorization_model_id": "model-id"
}
```

Response:

```json
{
  "tree": {
    "node": "document:roadmap#viewer",
    "kind": "object",
    "users": ["user:anne"],
    "children": []
  }
}
```

## Assertions

### GET `/stores/{storeId}/assertions/{authorizationModelId}`

Returns OpenFGA-compatible assertions for a model.

### POST `/stores/{storeId}/assertions/{authorizationModelId}`

```json
{
  "assertions": [
    {
      "tuple_key": {
        "user": "user:anne",
        "relation": "viewer",
        "object": "document:roadmap"
      },
      "expectation": true
    }
  ]
}
```

## Tenant RBAC

### GET `/tenants/{tenantId}/roles`

### POST `/tenants/{tenantId}/roles`

```json
{
  "name": "document-viewer",
  "description": "Can view documents"
}
```

### POST `/tenants/{tenantId}/roles/assign-permission`

```json
{
  "roleName": "document-viewer",
  "relation": "viewer",
  "object": "document:*"
}
```

### GET `/tenants/{tenantId}/permissions`

### GET `/tenants/{tenantId}/permissions/resolve?relation={relation}&object={object}`

### POST `/tenants/{tenantId}/permissions`

```json
{
  "relation": "viewer",
  "object": "document:*",
  "conditionName": "business_hours"
}
```

### POST `/tenants/{tenantId}/permissions/assign-to-role`

```json
{
  "roleName": "document-viewer",
  "relation": "viewer",
  "object": "document:*"
}
```

## Store RBAC

Store-scoped RBAC routes use the authenticated tenant and validate store ownership.

### GET `/stores/{storeId}/roles`

### POST `/stores/{storeId}/roles`

### POST `/stores/{storeId}/roles/assign-permission`

### GET `/stores/{storeId}/permissions`

### GET `/stores/{storeId}/permissions/resolve?relation={relation}&object={object}`

### POST `/stores/{storeId}/permissions`

### POST `/stores/{storeId}/permissions/assign-to-role`

### GET `/stores/{storeId}/users/{userId}/roles`

### POST `/stores/{storeId}/users/{userId}/roles`

## Tenant Users

### GET `/tenants/{tenantId}/users`

### POST `/tenants/{tenantId}/users`

```json
{
  "id": "user:anne",
  "username": "anne",
  "email": "anne@example.com"
}
```

### PUT `/tenants/{tenantId}/users/{userId}`

### DELETE `/tenants/{tenantId}/users/{userId}`

### GET `/tenants/{tenantId}/users/{userId}/roles`

### POST `/tenants/{tenantId}/users/{userId}/roles`

```json
{
  "roleName": "document-viewer"
}
```

## Audit

### GET `/tenants/{tenantId}/audit`

Query parameters:

| Name | Description |
| --- | --- |
| `action` | Optional action filter, for example `check` or `explain`. |
| `decision` | Optional decision filter, for example `ALLOW` or `DENY`. |

## Presets

### GET `/tenants/{tenantId}/presets`

Query parameters:

| Name | Description |
| --- | --- |
| `storeId` | Optional store filter. |
| `source` | Optional preset source such as `test-console` or `assertions`. |
| `scope` | Optional scope filter. |

### POST `/tenants/{tenantId}/presets`

### DELETE `/tenants/{tenantId}/presets`

### GET `/tenants/{tenantId}/presets/meta`

### PUT `/tenants/{tenantId}/presets/meta`

## Metrics And Health

### GET `/metrics`

Returns Prometheus-formatted authorization metrics.

### GET `/api/v1/metrics/authorization`

Returns the same authorization metrics through the versioned API route.

### GET `/health/live`

Liveness probe.

### GET `/health/ready`

Readiness probe including configured infrastructure health checks.

## Frontend Coverage

The admin dashboard currently exposes:

- Store lifecycle and active store selection.
- Authorization model CRUD and draft validation.
- Relationship tuple CRUD and change feed.
- Native check, explain, batch-check, OpenFGA-compatible check, and OpenFGA-compatible batch-check.
- Explain trace timeline and batch correlation tables.
- List-users, list-objects, and expand graph exploration with tree visualization.
- Assertions, access management, audit, presets, profile, and onboarding flows.

## OpenAPI

Swagger is available in development at:

```text
http://localhost:5000/swagger
```
