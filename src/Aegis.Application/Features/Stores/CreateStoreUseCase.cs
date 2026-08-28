using Aegis.Application.DomainEvents;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Domain.Entities;
using Aegis.Domain.Repositories;

namespace Aegis.Application.Features.Stores;

public sealed class CreateStoreUseCase
{
    private readonly IStoreRegistry _storeRegistry;
    private readonly IStoreRepository _storeRepository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public CreateStoreUseCase(
        IStoreRegistry storeRegistry,
        IStoreRepository storeRepository,
        IDomainEventDispatcher domainEventDispatcher)
    {
        _storeRegistry = storeRegistry;
        _storeRepository = storeRepository;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task<StoreDto> ExecuteAsync(
        string tenantId,
        CreateStoreRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateName(request);
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            return await _storeRegistry.CreateForTenantAsync(tenantId, request.Name, cancellationToken);
        }

        var store = Store.Create(request.Name);
        await _storeRepository.AddAsync(store, cancellationToken);
        await _domainEventDispatcher.DispatchAndClearAsync(store, cancellationToken);
        return ToDto(store, null);
    }

    public async Task<StoreDto> ExecuteIdempotentAsync(
        string tenantId,
        CreateStoreRequestDto request,
        string actorId,
        string idempotencyKey,
        string requestFingerprint,
        CancellationToken cancellationToken = default)
    {
        ValidateIdempotencyContext(tenantId, actorId, idempotencyKey, requestFingerprint);
        ValidateName(request);
        var store = Store.Create(request.Name);
        var mutation = new IdempotentMutation(
            tenantId,
            actorId,
            "store.create",
            idempotencyKey,
            requestFingerprint,
            DateTimeOffset.UtcNow.AddHours(24));
        var result = await _storeRepository.AddIdempotentAsync(store, mutation, cancellationToken);
        if (result.Created)
        {
            await _domainEventDispatcher.DispatchAndClearAsync(store, cancellationToken);
        }

        return ToDto(result.Store, tenantId);
    }

    private static void ValidateName(CreateStoreRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Store name is required.");
        }
    }

    private static void ValidateIdempotencyContext(
        string tenantId,
        string actorId,
        string idempotencyKey,
        string requestFingerprint)
    {
        if (string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(actorId)
            || string.IsNullOrWhiteSpace(idempotencyKey)
            || string.IsNullOrWhiteSpace(requestFingerprint)
            || requestFingerprint.Length != 64
            || requestFingerprint.Any(character => !char.IsAsciiHexDigit(character)))
        {
            throw new ArgumentException("A valid tenant, actor, idempotency key, and SHA-256 request fingerprint are required.");
        }
    }

    private static StoreDto ToDto(Store store, string? tenantId)
    {
        return new StoreDto(store.Id, store.Name, store.CreatedAt, store.UpdatedAt, null, null, tenantId);
    }
}
