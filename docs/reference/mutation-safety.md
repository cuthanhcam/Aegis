# Mutation safety

## Authorization-model mutations

Read `GET /api/v1/stores/{storeId}/authorization-models/{authorizationModelId}` and retain its strong `ETag`, for example `"3"`. Send that value unchanged in `If-Match` when calling `PUT` or `DELETE` for the same model.

```http
GET /api/v1/stores/store-1/authorization-models/model-1
ETag: "3"

PUT /api/v1/stores/store-1/authorization-models/model-1
If-Match: "3"
Content-Type: application/json
```

A successful update returns the next ETag. HTTP 428 means the precondition was omitted. HTTP 412 means another request changed the model; retrieve the current representation, review the difference, and decide whether to retry. Do not automatically replace the tag without reconciling the security policy change.

`POST .../{authorizationModelId}/publish` and `POST .../{authorizationModelId}/rollback` use the same `If-Match` contract and return the active model's new ETag. These operations update multiple rows atomically. A store-scoped database lock serializes competing lifecycle transitions, while the target revision prevents a stale command from executing after it acquires the lock.

Weak tags, wildcard tags, lists of tags, and unquoted revisions are not accepted in this first contract. These restrictions keep client and server behavior deterministic.

## Idempotency scope

Authorization-model creation accepts an optional `Idempotency-Key`. Use a unique value of 8–128 ASCII letters, digits, `.`, `:`, `_`, or `-` for each logical create and retain it until the request outcome is known.

The key is scoped to tenant, authenticated actor, store, and operation, then bound to a SHA-256 request fingerprint. The reservation, model insert, and stored response share one PostgreSQL transaction. A same-key/same-payload retry returns the original HTTP 201 model and ETag; same-key/different-payload reuse returns HTTP 409 `IDEMPOTENCY_CONFLICT`. Records have a 24-hour retention window.

Other mutations do not yet accept idempotency keys. Extending coverage requires the replay record and that mutation's business commit to share an atomic boundary. Redis can accelerate coordination only when its protocol cannot create a commit/replay gap; it is not the source of truth for this contract.

For model creation, retry with the original key and identical payload. For other mutations, retry only when the endpoint is naturally idempotent and its concurrency precondition is still current. Never blindly retry publish, rollback, or a timed-out request whose commit outcome is unknown. A concurrency precondition prevents stale state transitions; it does not provide response replay after an ambiguous timeout.
