# Mutation safety

## Authorization-model update and delete

Read `GET /api/v1/stores/{storeId}/authorization-models/{authorizationModelId}` and retain its strong `ETag`, for example `"3"`. Send that value unchanged in `If-Match` when calling `PUT` or `DELETE` for the same model.

```http
GET /api/v1/stores/store-1/authorization-models/model-1
ETag: "3"

PUT /api/v1/stores/store-1/authorization-models/model-1
If-Match: "3"
Content-Type: application/json
```

A successful update returns the next ETag. HTTP 428 means the precondition was omitted. HTTP 412 means another request changed the model; retrieve the current representation, review the difference, and decide whether to retry. Do not automatically replace the tag without reconciling the security policy change.

Weak tags, wildcard tags, lists of tags, and unquoted revisions are not accepted in this first contract. These restrictions keep client and server behavior deterministic.

## Idempotency scope

Idempotency keys are not yet accepted. A correct implementation must bind the key to tenant, authenticated principal, route, method, and canonical payload hash; atomically reserve execution; replay the original status, safe headers, and body; reject key reuse with a different payload; and expire records under a documented retention policy. Redis can be used only with an atomic reservation protocol. The database remains the conservative default for multi-replica durability.

Until that contract ships, retry a mutation only when its endpoint is naturally idempotent and its concurrency precondition is still current. Never blindly retry model creation, publish, rollback, or a timed-out request whose commit outcome is unknown.
