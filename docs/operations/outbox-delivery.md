# Operating the domain-event outbox

Aegis uses an outbox to retain domain-event delivery work after an event reaches the dispatcher. With PostgreSQL storage selected, messages are stored in `outbox_messages`; in-memory storage remains a local/test profile and is not crash durable.

## Current delivery contract

The background worker reads due, unprocessed messages in creation order, publishes them through the configured publisher, and marks successful messages as processed. A failure increments `attempt_count`, records a bounded error summary, and schedules an exponential retry capped by configuration.

```json
{
  "Outbox": {
    "BatchSize": 100,
    "PollIntervalSeconds": 10,
    "InitialRetrySeconds": 2,
    "MaximumRetrySeconds": 300
  }
}
```

Startup rejects batch sizes outside 1–1000, polling outside 1–300 seconds, retry values outside their supported bounds, or a maximum retry smaller than the initial retry. Cancellation stops the worker instead of recording a delivery failure.

The logging publisher is still a local product foundation, not an external delivery guarantee. Processed rows are retained, so an operator-approved retention policy is required before production volume grows.

## Guarantees and explicit gaps

PostgreSQL persistence now survives process restart and retains attempt/error/next-attempt state. The same store can be reconstructed and continue reading pending messages.

This iteration is not yet a transactional outbox. Several application use cases commit business state before dispatching and appending the event, so a process failure in that gap can still lose a message. Pending selection also has no lease/claim token, so running multiple workers can publish the same message concurrently. Consumers must remain idempotent, but idempotency alone does not close the producer-side gap.

Before claiming production-grade external delivery, Aegis must:

1. append business state, audit evidence, and outbox messages in the same PostgreSQL transaction;
2. claim work with `FOR UPDATE SKIP LOCKED` plus a bounded lease or equivalent single-owner protocol;
3. add poison/dead-letter policy and operator replay controls;
4. expose backlog count, oldest age, retry, and terminal-failure metrics;
5. define processed-message retention and rehearse recovery.

## Investigation

Inspect only metadata and avoid copying payloads into general logs because event JSON may contain authorization identifiers.

```sql
SELECT id, event_type, created_at, attempt_count, last_error, next_attempt_at
FROM outbox_messages
WHERE processed_at IS NULL
ORDER BY next_attempt_at, created_at
LIMIT 100;
```

A growing oldest-message age indicates publisher failure, an unhealthy worker, or retry saturation. Preserve message identifiers, timestamps, attempt counts, application revision, and sanitized errors as incident evidence. Do not mark rows processed manually without an approved reconciliation record.
