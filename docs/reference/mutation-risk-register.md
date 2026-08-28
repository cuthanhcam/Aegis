# Mutation risk register

This register classifies the current HTTP mutation surface by retry behavior. It is a release-review input, not permission to retry every POST. Reassess an endpoint whenever its transaction boundary or response changes.

| Surface | Current behavior | Retry risk | Required direction |
| --- | --- | --- | --- |
| Store create | Allocates a new store identity | High duplicate-resource risk | Transactional replay implemented |
| Authorization-model create | Allocates a security-policy version | High duplicate-version risk | Transactional replay implemented |
| Model update/delete | Changes one version | Lost update or stale delete | Strong ETag and atomic revision predicate implemented |
| Model publish/rollback | Changes active and archived versions | Conflicting lifecycle transition | Store-serialized transaction and ETag implemented |
| Relationship upsert | Unique tuple is replaced with requested effect | Naturally convergent for an identical payload | Document retry behavior; no replay record yet |
| Relationship delete | Repeated delete converges, but response differs after the first call | Low state risk, response ambiguity | Consider stable delete response before replay storage |
| Role, permission, and assignment writes | Persistence uses natural composite identities in supported providers | Usually convergent, provider parity requires proof | Add duplicate/concurrent negative tests before declaring retry-safe |
| User create | Allocates or rejects a user identity depending on provider | Duplicate/conflict ambiguity | Candidate for transactional replay after repository boundary cleanup |
| Preset create/meta/delete | In-process application surface with incomplete durable boundary | High portability and restart risk | Defer idempotency until persistence ownership is explicit |
| Assertion write/run/generate | Writes assertion definitions, execution history, or generated data | Duplicate execution/history risk | High-priority candidate after use-case and transaction split |
| Login/refresh/logout | Credential/session protocol | Security-sensitive token rotation | Use protocol-specific replay/rotation defenses, not business idempotency records |
| Check, explain, graph, validate | HTTP POST used for read/compute semantics | No business mutation; audit may be emitted | Retry only under documented consistency/deadline rules |

## Selection rule

Add durable replay only when a timeout can leave a committed, externally visible result that the client cannot safely discover or reconstruct. The reservation and business change must commit atomically. Natural-key upserts should first receive concurrency and provider-parity tests; adding stored replay to every POST would increase retention, privacy, and operational cost without improving correctness.

The next candidates are user creation and assertion execution. Both should wait until their broad application services are split into explicit use cases with one persistence transaction owner.

Transactional replay does not make the current in-process/outbox event path atomic with the resource commit. Before external consumers depend on store/model creation events, persist the outbox message in the same database transaction and require consumer-side deduplication.
