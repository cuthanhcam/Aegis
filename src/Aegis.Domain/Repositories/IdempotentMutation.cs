using Aegis.Domain.Entities;

namespace Aegis.Domain.Repositories;

public sealed record IdempotentMutation(
    string TenantId,
    string ActorId,
    string Operation,
    string Key,
    string RequestFingerprint,
    DateTimeOffset ExpiresAt);

public sealed record IdempotentAuthorizationModelAddResult(
    AuthorizationModel AuthorizationModel,
    bool Created);

public sealed record IdempotentStoreAddResult(
    Store Store,
    bool Created);
