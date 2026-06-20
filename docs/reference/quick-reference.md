# Aegis Quick Reference

Fast commands for the current API shape. All examples assume:

```bash
BASE=http://localhost:5271/api/v1
TOKEN=<access-token>
TENANT=tenant-a
STORE=<store-id>
MODEL=<authorization-model-id>
```

Headers:

```bash
-H "Authorization: Bearer $TOKEN" \
-H "Content-Type: application/json"
```

## Tuple Format

```text
user:anne viewer document:roadmap
team:eng member user:anne
```

Subject and object values use `<type>:<id>`. Relations are plain names such as `owner`, `editor`, `viewer`, or `member`.

## Auth

```bash
curl -X POST "$BASE/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

```bash
curl "$BASE/auth/me" \
  -H "Authorization: Bearer $TOKEN"
```

## Stores

```bash
curl "$BASE/stores" \
  -H "Authorization: Bearer $TOKEN"
```

```bash
curl -X POST "$BASE/stores" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"document-service"}'
```

## Authorization Models

Create model:

```bash
curl -X POST "$BASE/stores/$STORE/authorization-models" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "schemaVersion": "1.1",
    "model": "type user\ntype document\n  define viewer: [user]"
  }'
```

Validate draft without saving:

```bash
curl -X POST "$BASE/stores/$STORE/authorization-models/validate" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "schemaVersion": "1.1",
    "model": "type user\ntype document\n  define viewer: [user]"
  }'
```

List versions:

```bash
curl "$BASE/stores/$STORE/authorization-models" \
  -H "Authorization: Bearer $TOKEN"
```

## Relationships

Write tuple:

```bash
curl -X POST "$BASE/stores/$STORE/relationships" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "subject": "user:anne",
    "relation": "viewer",
    "object": "document:roadmap",
    "effect": "allow"
  }'
```

Create explicit deny:

```bash
curl -X POST "$BASE/stores/$STORE/relationships" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "subject": "user:bob",
    "relation": "viewer",
    "object": "document:roadmap",
    "effect": "deny"
  }'
```

List tuples:

```bash
curl "$BASE/stores/$STORE/relationships?subject=user:anne&relation=viewer" \
  -H "Authorization: Bearer $TOKEN"
```

Delete tuple:

```bash
curl -X DELETE "$BASE/stores/$STORE/relationships" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "subject": "user:anne",
    "relation": "viewer",
    "object": "document:roadmap"
  }'
```

Read change feed:

```bash
curl "$BASE/stores/$STORE/relationships/changes?page_size=50" \
  -H "Authorization: Bearer $TOKEN"
```

## Native Check And Explain

Check:

```bash
curl -X POST "$BASE/stores/$STORE/check" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "user": "user:anne",
    "relation": "viewer",
    "object": "document:roadmap",
    "authorizationModelId": "'$MODEL'"
  }'
```

Explain:

```bash
curl -X POST "$BASE/stores/$STORE/explain" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "user": "user:anne",
    "relation": "viewer",
    "object": "document:roadmap",
    "authorizationModelId": "'$MODEL'"
  }'
```

Batch check:

```bash
curl -X POST "$BASE/stores/$STORE/batch-check" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {
        "user": "user:anne",
        "relation": "viewer",
        "object": "document:roadmap",
        "correlationId": "one"
      }
    ]
  }'
```

## OpenFGA-Compatible Check

Check:

```bash
curl -X POST "$BASE/stores/$STORE/check/compat" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "tuple_key": {
      "user": "user:anne",
      "relation": "viewer",
      "object": "document:roadmap"
    },
    "authorization_model_id": "'$MODEL'"
  }'
```

Batch check:

```bash
curl -X POST "$BASE/stores/$STORE/batch-check/compat" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "authorization_model_id": "'$MODEL'",
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
  }'
```

## Graph Queries

List users:

```bash
curl -X POST "$BASE/stores/$STORE/graph/list-users" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "relation": "viewer",
    "object": "document:roadmap",
    "authorizationModelId": "'$MODEL'"
  }'
```

List objects:

```bash
curl -X POST "$BASE/stores/$STORE/graph/list-objects" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "user": "user:anne",
    "relation": "viewer",
    "type": "document",
    "authorizationModelId": "'$MODEL'"
  }'
```

Expand:

```bash
curl -X POST "$BASE/stores/$STORE/graph/expand" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "relation": "viewer",
    "object": "document:roadmap",
    "authorizationModelId": "'$MODEL'"
  }'
```

OpenFGA-compatible list users:

```bash
curl -X POST "$BASE/stores/$STORE/graph/list-users/compat" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "object": { "type": "document", "id": "roadmap" },
    "relation": "viewer",
    "user_filters": [],
    "authorization_model_id": "'$MODEL'"
  }'
```

## Assertions

```bash
curl "$BASE/stores/$STORE/assertions/$MODEL" \
  -H "Authorization: Bearer $TOKEN"
```

```bash
curl -X POST "$BASE/stores/$STORE/assertions/$MODEL" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
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
  }'
```

## RBAC

Tenant roles:

```bash
curl "$BASE/tenants/$TENANT/roles" \
  -H "Authorization: Bearer $TOKEN"
```

```bash
curl -X POST "$BASE/tenants/$TENANT/roles" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"document-viewer","description":"Can view documents"}'
```

Tenant permissions:

```bash
curl "$BASE/tenants/$TENANT/permissions" \
  -H "Authorization: Bearer $TOKEN"
```

Store roles:

```bash
curl "$BASE/stores/$STORE/roles" \
  -H "Authorization: Bearer $TOKEN"
```

## Users

```bash
curl "$BASE/tenants/$TENANT/users" \
  -H "Authorization: Bearer $TOKEN"
```

```bash
curl -X POST "$BASE/tenants/$TENANT/users" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"id":"user:anne","username":"anne","email":"anne@example.com"}'
```

Assign role to user:

```bash
curl -X POST "$BASE/tenants/$TENANT/users/user:anne/roles" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"roleName":"document-viewer"}'
```

## Audit And Metrics

```bash
curl "$BASE/tenants/$TENANT/audit?action=check&decision=ALLOW" \
  -H "Authorization: Bearer $TOKEN"
```

```bash
curl "$BASE/metrics/authorization" \
  -H "Authorization: Bearer $TOKEN"
```

```bash
curl http://localhost:5271/metrics
```

## Health

```bash
curl http://localhost:5271/health/live
curl http://localhost:5271/health/ready
```

## Common Decision Codes

| Code | Meaning |
| --- | --- |
| `ALLOW_REBAC_DIRECT` | Direct relationship tuple matched. |
| `ALLOW_RBAC_ROLE` | RBAC fallback matched. |
| `DENY_EXPLICIT` | Explicit deny tuple matched. |
| `DENY_NOT_FOUND` | No allow rule matched. |
| `DENY_INVALID_INPUT` | Request tuple format was invalid. |

## Troubleshooting

- Use `/stores/{storeId}/explain` when a check surprises you.
- Verify the active store belongs to the authenticated tenant.
- Verify the model id exists in `/stores/{storeId}/authorization-models`.
- Use `/stores/{storeId}/relationships/changes` to audit tuple writes and deletes.
- Use `/api/v1/metrics/authorization` to confirm checks are reaching the engine.

## Links

- [API Reference](api-reference.md)
- [Core Concepts](../concepts/core-concepts-tuple-model.md)
- [Getting Started](../guides/getting-started-development.md)
- [Deployment Guide](../operations/deployment.md)
