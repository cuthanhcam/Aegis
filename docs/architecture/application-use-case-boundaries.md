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
| User create | `CreateUserUseCase` | RBAC administration repository insert | Extracted |
| User update | `UpdateUserUseCase` | RBAC administration repository update-and-return | Extracted |
| User delete | `DeleteUserUseCase` | RBAC administration repository transaction | Extracted |
| Assertion write | `WriteAssertionsUseCase` | Versioned assertion repository replace | Extracted |
| Assertion run | `RunAssertionsUseCase` | One captured definition snapshot plus append-only run store | Extracted |
| Assertion generate | `GenerateAssertionsFromAuditUseCase` | Audit query plus atomic definition append | Extracted |

## Store-create flow

`StoresController` retains HTTP ownership of tenant/actor resolution and idempotency-header validation. `CreateStoreUseCase` owns store-name and command-context validation, constructs the aggregate, calls the atomic repository operation, suppresses duplicate event dispatch on replay, and maps the result. `StoreAppService` now handles store list/get/delete only; create delegates and the nullable compatibility constructor were removed after caller migration completed.

## Authorization-model validation boundary

`AuthorizationModelValidator` owns deterministic validation of schema metadata, type and relation declarations, rewrite expressions, stable issue codes, source line numbers, warnings, and the capability summary. It is a stateless application component: it does not resolve stores, persist models, dispatch events, inspect HTTP context, or depend on `AuthorizationModelAppService`.

The existing validation endpoint and model mutation paths temporarily reach it through `AuthorizationModelAppService`. This preserves the public contract while removing the circular dependency risk for the next extraction: model create and update use cases can now consume the validator directly. Cancellation is checked before parsing and between model lines so large inputs remain cooperative with the request deadline.

## Authorization-model create flow

`AuthorizationModelsController` retains store-tenant authorization, actor resolution, idempotency-header parsing, request fingerprinting, HTTP status selection, and ETag emission. `CreateAuthorizationModelUseCase` validates command context and the DSL, verifies that the store exists, constructs and marks the aggregate as validated, selects the repository transaction, suppresses duplicate domain-event dispatch during replay, and maps the persisted aggregate to the public DTO.

The repository remains the atomic owner of an idempotent create: reservation lookup, fingerprint conflict detection, model insertion, and response storage complete in one transaction. All transport callers now depend on the command use case directly; the broad model application service no longer exposes create delegates.

## Authorization-model update and delete flows

`AuthorizationModelsController` parses the required strong `If-Match` precondition and passes its expected revision to the command. `UpdateAuthorizationModelUseCase` validates the model definition, verifies store/model identity, mutates the aggregate, and invokes the repository compare-and-write predicate. `DeleteAuthorizationModelUseCase` marks the loaded aggregate for deletion and invokes the equivalent compare-and-delete predicate.

A failed repository mutation is re-read to preserve the external distinction between a model that disappeared and one that still exists at another revision. Only the latter becomes `ConcurrencyConflictException` and HTTP 412. Domain events are dispatched only after a successful update or delete; failed and missing mutations do not emit misleading audit/event activity. Update and delete delegates were removed after caller migration completed.

## Authorization-model lifecycle flows

`PublishAuthorizationModelUseCase` and `RollbackAuthorizationModelUseCase` validate the target snapshot and its expected revision before asking the repository to transition lifecycle state. The production repository owns the store-scoped lock, rechecks the target revision inside the transaction, archives the previous active model, publishes the target, and returns the committed state. The partial unique database index remains defense in depth for the single-published-model invariant.

When a repository transition returns no target, each use case re-reads the model to classify a concurrent lifecycle change as a precondition conflict rather than not found. Rollback audit is written only after the repository returns a committed active model. Registry-only mutation fallbacks have been removed; every model command now requires the repository transaction contract through strict composition. Rollback also resolves the previous published snapshot through that repository boundary.

## Remaining model application service

`IAuthorizationModelAppService` is now read/analysis-oriented. It lists and resolves model snapshots, computes diffs, and exposes the compatibility validation endpoint. It no longer accepts create, update, delete, publish, or rollback commands, and its implementation no longer depends on event dispatch or audit infrastructure. This narrower surface prevents new callers from bypassing the explicit transaction boundaries.

`IStoreAppService` follows the same rule for creation: it retains store query and deletion behavior, while creation is available only through `CreateStoreUseCase`. The use case requires its repository and event dispatcher explicitly; nullable compatibility composition is no longer permitted.

All authorization-model command classes now follow strict composition as well. Create, update, delete, publish, and rollback require their repository and any event/audit collaborator directly. There are no private compatibility constructors, nullable persistence dependencies, or alternate multi-call mutation algorithms hidden behind the same command API.

## User mutation flows

`UsersController` retains route-tenant authorization and HTTP response mapping. Create, update, and delete now enter the Application layer through `CreateUserUseCase`, `UpdateUserUseCase`, and `DeleteUserUseCase`; the broad RBAC administration service retains query, role, and permission responsibilities but can no longer become an alternate caller for user profile mutations.

Each use case requires a non-empty tenant and user identifier before persistence. The tenant remains part of every repository key and predicate, so identical user identifiers in different tenants are independent. Create is a single insert that returns the created row. Update is a single update-and-return operation: PostgreSQL uses `UPDATE ... RETURNING`, eliminating the former mutation-then-read window in which a concurrent delete or update could change the response. The in-memory provider returns the snapshot written by the same operation.

Delete owns two related persistence effects: removing store role assignments and removing the tenant user profile. The PostgreSQL provider executes both inside an explicit transaction and determines success from the user-row deletion, not from the combined affected-row count. This prevents a stale assignment cleanup from being reported as a successful user deletion. Cache eviction occurs only after a committed mutation result. The repository, rather than the use case or controller, remains responsible for these storage-specific atomicity details.

Role and permission mutations are intentionally still on `IRbacAdminService`. They are single provider calls today, but their desired conflict, existence, and audit semantics need a separate product decision before their public orchestration surface is narrowed.

## Assertion transaction review

Assertion definitions now use `IAssertionRepository`; the process-local static dictionary and nullable compatibility composition have been removed. A snapshot is scoped by both store and authorization model and carries a monotonically increasing internal revision. Reads return revision zero with an empty collection when no set has been written. Replace writes one complete snapshot, while `AppendDistinctAsync` serializes read/deduplicate/capacity-check/write as one mutation.

The PostgreSQL provider stores one JSONB assertion set per `(store_id, authorization_model_id)` through migration 014. A transaction-scoped advisory lock serializes both first-write and existing-row mutations, avoiding the missing-row race that `SELECT ... FOR UPDATE` alone cannot prevent. The in-memory provider uses an equivalent per-key critical section. Both implementations deduplicate audit-generated assertions and enforce the 100-item capacity before committing, so a rejected append leaves the prior revision unchanged. Store purge removes definition snapshots as well as append-only run history.

`AssertionAppService` temporarily retains read, run, history, generation, and purge orchestration, all backed by the same repository snapshot. Its permission checker, definition repository, run-history store, audit store, and shared validator are mandatory dependencies. The next extraction can therefore introduce explicit run and generate use cases without inventing transaction ownership. A subsequent additive contract decision is still required before exposing the internal definition revision or recording it on public run DTOs.

Assertion replacement now enters through `WriteAssertionsUseCase` rather than the broad service. The controller retains store-tenant authorization and HTTP response mapping. The use case validates store/model scope, the 100-item command limit, tuple shapes, contextual tuples, and model type/relation references before invoking one repository replace. Failed validation never mutates or advances the assertion snapshot.

`AssertionValidator` is the single stateless owner of assertion tuple and model-reference validation. Audit generation consumes the same validator when filtering candidate events, preventing write and generated assertions from drifting into different validity rules. Write and run delegates have been removed from `IAssertionAppService`; read, history queries, generation, and purge remain until their callers are migrated.

Assertion execution now enters through `RunAssertionsUseCase`. The use case validates store/model scope, reads one immutable `AssertionSetSnapshot`, and uses that captured collection and model ID for the entire run. Concurrent replacement may create a newer repository revision, but it cannot change the assertion collection already being evaluated. Permission checks retain trace-enabled audit behavior, cancellation is observed between items, and the completed run record is appended only after every captured assertion has produced a result.

An empty snapshot is a valid executable state and produces a persisted zero-result run, preserving operational evidence that the command occurred. Invalid store/model scope and interrupted or failed evaluation do not write a misleading completed run. The controller consumes the run use case directly, and the run delegate plus permission-check dependency have been removed from the broad assertion service.

Audit-derived generation now enters through `GenerateAssertionsFromAuditUseCase`. It validates store/model scope, normalizes the optional decision filter, constrains the requested limit, queries audit events within the resolved tenant and store, retains check/explain events, maps their decisions to expectations, filters candidates through `AssertionValidator`, and deduplicates before returning the draft. Draft-only generation performs no definition mutation.

When append is requested, the use case passes the complete candidate set to the repository's serialized `AppendDistinctAsync` operation. Capacity is evaluated against the latest persisted snapshot inside that atomic boundary; overflow is mapped to the stable compatibility error and leaves revision/content unchanged. The controller consumes the use case directly, while audit and validator dependencies plus the generation delegate have been removed from `AssertionAppService`. The remaining service surface is read, run-history queries, and store purge.

## Review checklist

- One command and one primary state-transition outcome.
- Tenant and actor context are explicit inputs where required.
- Repository method states the atomic boundary rather than exposing partial steps.
- Retry, replay, conflict, cancellation, and event behavior are testable without HTTP.
- Controller contains transport mapping only.
- No dependency from Application to ASP.NET, EF/Npgsql, Redis, or concrete infrastructure.
- Tracker records compatibility delegates and the condition for removing them.
