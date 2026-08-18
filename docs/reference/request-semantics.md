# API request semantics

This reference defines how Aegis v1 consumers page, batch, cancel, and retry requests. Endpoint-specific documentation may narrow these limits but cannot silently exceed them.

## Pagination

Paged native endpoints use `page_size` and `continuation_token`.

| Rule | v1 value |
| --- | --- |
| Default page size | 50 |
| Maximum page size | 100 |
| Maximum continuation-token length | 512 characters |

A continuation token is opaque. Persist and return it exactly as received; do not decode, increment, compare, or use it across a different tenant, store, filter set, or endpoint. A missing or null token means there is no next page.

During rolling upgrades, the server accepts numeric tokens emitted by older Aegis versions. New responses use a versioned opaque encoding. Legacy acceptance is transitional server behavior, not permission for clients to construct offsets.

`GET /stores/{storeId}/relationships/changes` is the first governed native paged endpoint. Its optional `type` filter is limited to 128 characters. Other list endpoints currently retain their v1 collection shape and server-defined ordering; they will migrate individually to avoid breaking clients.

## Batch limits

Native and compatibility batch-check requests accept 1–1,000 items. Empty batches and requests over 1,000 items fail validation before authorization evaluation begins. A batch limit protects capacity; it does not promise atomic mutation semantics because batch check is read-only and each compatibility result may carry its own evaluation error.

## Deadlines and cancellation

The host default request deadline is 30 seconds and is configured through `RequestTimeouts:DefaultSeconds`. Startup rejects values outside 1–300 seconds. When the deadline expires:

- native routes return HTTP 504 and `REQUEST_TIMEOUT`;
- compatibility routes return HTTP 504 and `request_timeout`;
- the response includes safe trace correlation according to its envelope;
- `HttpContext.RequestAborted` is cancelled and propagated through controller, application, repository, and provider calls.

Cancellation is cooperative. Code performing CPU work or calling a dependency must observe the supplied token. A timeout does not make a mutation safe to retry: the server may have committed work before the caller received the response.

## Filtering and sorting

Filters are endpoint-specific and combine conjunctively unless an endpoint states otherwise. Unknown query parameters are not contractual features. Sorting is server-defined on endpoints that do not document a sort parameter; clients must not depend on incidental in-memory or database ordering.

## Retry rules

Read-only requests may be retried with bounded backoff when the caller's own deadline permits. Mutations must not be automatically retried until that endpoint documents an idempotency key or optimistic-concurrency contract. HTTP 429 and 504 alone do not prove a write was unapplied.
