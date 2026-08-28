# Application use-case boundaries

## Boundary rule

An application command use case represents one externally meaningful state transition. It validates command-level input, coordinates domain objects and repositories, defines the transaction owner, decides whether post-commit domain events should be dispatched, and returns a contract DTO. It does not parse HTTP headers, inspect ASP.NET claims, select response status codes, or implement database statements.

The API adapter authenticates and authorizes the caller, resolves tenant/actor context, parses transport preconditions, computes transport fingerprints, and invokes the use case. The repository performs atomic compare-and-write or reservation-and-commit behavior. This separation keeps security context explicit without leaking framework types into Application or business orchestration into controllers.

## Current extraction map

| Command | Controller dependency | Transaction owner | Status |
| --- | --- | --- | --- |
| Store create | `CreateStoreUseCase` | Store repository for idempotent create; registry compatibility path otherwise | Extracted |
| Authorization-model create | Broad model application service | Authorization-model repository | Next: extract validator, then command |
| Model update/delete | Broad model application service | Authorization-model repository revision predicate | Planned extraction |
| Model publish/rollback | Broad model application service | Store-serialized authorization-model repository transaction | Planned extraction |
| User mutations | Broad RBAC administration service | Provider-specific RBAC store | Pending transaction review |
| Assertion write/run/generate | Broad assertion application service | Assertion stores and audit/outbox paths | Pending transaction review |

## Store-create flow

`StoresController` retains HTTP ownership of tenant/actor resolution and idempotency-header validation. `CreateStoreUseCase` owns store-name and command-context validation, constructs the aggregate, calls the atomic repository operation, suppresses duplicate event dispatch on replay, and maps the result. `StoreAppService` handles store list/get/delete and exposes temporary create delegates only for internal migration compatibility.

## Review checklist

- One command and one primary state-transition outcome.
- Tenant and actor context are explicit inputs where required.
- Repository method states the atomic boundary rather than exposing partial steps.
- Retry, replay, conflict, cancellation, and event behavior are testable without HTTP.
- Controller contains transport mapping only.
- No dependency from Application to ASP.NET, EF/Npgsql, Redis, or concrete infrastructure.
- Tracker records compatibility delegates and the condition for removing them.
