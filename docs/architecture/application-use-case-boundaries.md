# Application use-case boundaries

## Boundary rule

An application command use case represents one externally meaningful state transition. It validates command-level input, coordinates domain objects and repositories, defines the transaction owner, decides whether post-commit domain events should be dispatched, and returns a contract DTO. It does not parse HTTP headers, inspect ASP.NET claims, select response status codes, or implement database statements.

The API adapter authenticates and authorizes the caller, resolves tenant/actor context, parses transport preconditions, computes transport fingerprints, and invokes the use case. The repository performs atomic compare-and-write or reservation-and-commit behavior. This separation keeps security context explicit without leaking framework types into Application or business orchestration into controllers.

## Current extraction map

| Command | Controller dependency | Transaction owner | Status |
| --- | --- | --- | --- |
| Store create | `CreateStoreUseCase` | Store repository for idempotent create; registry compatibility path otherwise | Extracted |
| Authorization-model validation | `AuthorizationModelValidator` through the compatibility service endpoint | None; pure application computation | Extracted |
| Authorization-model create | `CreateAuthorizationModelUseCase` | Authorization-model repository; idempotent reservation and model insert share its transaction | Extracted |
| Model update | `UpdateAuthorizationModelUseCase` | Authorization-model repository revision predicate | Extracted |
| Model delete | `DeleteAuthorizationModelUseCase` | Authorization-model repository revision predicate | Extracted |
| Model publish | `PublishAuthorizationModelUseCase` | Store-serialized authorization-model repository transaction | Extracted |
| Model rollback | `RollbackAuthorizationModelUseCase` | Store-serialized authorization-model repository transaction | Extracted |
| User mutations | Broad RBAC administration service | Provider-specific RBAC store | Pending transaction review |
| Assertion write/run/generate | Broad assertion application service | Assertion stores and audit/outbox paths | Pending transaction review |

## Store-create flow

`StoresController` retains HTTP ownership of tenant/actor resolution and idempotency-header validation. `CreateStoreUseCase` owns store-name and command-context validation, constructs the aggregate, calls the atomic repository operation, suppresses duplicate event dispatch on replay, and maps the result. `StoreAppService` handles store list/get/delete and exposes temporary create delegates only for internal migration compatibility.

## Authorization-model validation boundary

`AuthorizationModelValidator` owns deterministic validation of schema metadata, type and relation declarations, rewrite expressions, stable issue codes, source line numbers, warnings, and the capability summary. It is a stateless application component: it does not resolve stores, persist models, dispatch events, inspect HTTP context, or depend on `AuthorizationModelAppService`.

The existing validation endpoint and model mutation paths temporarily reach it through `AuthorizationModelAppService`. This preserves the public contract while removing the circular dependency risk for the next extraction: model create and update use cases can now consume the validator directly. Cancellation is checked before parsing and between model lines so large inputs remain cooperative with the request deadline.

## Authorization-model create flow

`AuthorizationModelsController` retains store-tenant authorization, actor resolution, idempotency-header parsing, request fingerprinting, HTTP status selection, and ETag emission. `CreateAuthorizationModelUseCase` validates command context and the DSL, verifies that the store exists, constructs and marks the aggregate as validated, selects the repository transaction, suppresses duplicate domain-event dispatch during replay, and maps the persisted aggregate to the public DTO.

The repository remains the atomic owner of an idempotent create: reservation lookup, fingerprint conflict detection, model insertion, and response storage complete in one transaction. The broad application service exposes temporary create delegates for internal compatibility only; new transport callers depend on the command use case directly.

## Authorization-model update and delete flows

`AuthorizationModelsController` parses the required strong `If-Match` precondition and passes its expected revision to the command. `UpdateAuthorizationModelUseCase` validates the model definition, verifies store/model identity, mutates the aggregate, and invokes the repository compare-and-write predicate. `DeleteAuthorizationModelUseCase` marks the loaded aggregate for deletion and invokes the equivalent compare-and-delete predicate.

A failed repository mutation is re-read to preserve the external distinction between a model that disappeared and one that still exists at another revision. Only the latter becomes `ConcurrencyConflictException` and HTTP 412. Domain events are dispatched only after a successful update or delete; failed and missing mutations do not emit misleading audit/event activity. The broad application service retains temporary delegates while controller callers migrate command by command.

## Authorization-model lifecycle flows

`PublishAuthorizationModelUseCase` and `RollbackAuthorizationModelUseCase` validate the target snapshot and its expected revision before asking the repository to transition lifecycle state. The production repository owns the store-scoped lock, rechecks the target revision inside the transaction, archives the previous active model, publishes the target, and returns the committed state. The partial unique database index remains defense in depth for the single-published-model invariant.

When a repository transition returns no target, each use case re-reads the model to classify a concurrent lifecycle change as a precondition conflict rather than not found. Rollback audit is written only after the repository returns a committed active model. The registry-only multi-call path remains a compatibility path for legacy/test providers and is not the production atomicity guarantee.

## Review checklist

- One command and one primary state-transition outcome.
- Tenant and actor context are explicit inputs where required.
- Repository method states the atomic boundary rather than exposing partial steps.
- Retry, replay, conflict, cancellation, and event behavior are testable without HTTP.
- Controller contains transport mapping only.
- No dependency from Application to ASP.NET, EF/Npgsql, Redis, or concrete infrastructure.
- Tracker records compatibility delegates and the condition for removing them.
