# ADR 0011: Move mutation transaction ownership into explicit use cases

- Status: Accepted
- Date: 2026-08-28

## Context

Several Aegis application services combine unrelated reads, writes, validation, cleanup, audit, and persistence-provider fallbacks. Adding concurrency and idempotency directly to these broad classes makes transaction ownership difficult to review and encourages controllers to depend on an entire administration surface for one command.

## Decision

Security-relevant or replayable mutations are extracted incrementally into command-focused application use cases. A use case owns input validation, aggregate creation or loading, the repository transaction call, domain-event dispatch decision, and response mapping for one business command. Controllers may consume the explicit use case alongside a query-oriented application service during migration.

`CreateStoreUseCase` is the first extracted boundary. `StoresController.Create` calls it directly. `StoreAppService` retains delegating create methods temporarily for internal compatibility, but no longer owns create transaction logic.

Use cases depend on application/domain abstractions, never ASP.NET types. HTTP concerns such as parsing `Idempotency-Key`, resolving claims, and formatting envelopes remain in the API layer. Atomic reservation and resource persistence remain repository responsibilities.

## Consequences

The extraction is intentionally incremental; large flag-day interface changes would add risk without improving authorization correctness. Compatibility delegates can be removed after all internal callers and tests use the command boundary.

The authorization-model migration completed this condition in B1 iteration 15. Caller audit confirmed that all HTTP mutations use explicit command boundaries, so create, update, delete, publish, and rollback were removed from `IAuthorizationModelAppService`. The remaining service surface is limited to query, diff, and validation behavior. Store-create delegates remain separately tracked until their own caller audit is complete.

Authorization-model creation is the next candidate, but its DSL validator must first become an independent application component to avoid a circular dependency on `AuthorizationModelAppService`. User and assertion commands follow after their persistence transaction ownership is explicit.
