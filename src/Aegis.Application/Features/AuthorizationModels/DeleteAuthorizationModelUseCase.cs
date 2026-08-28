using Aegis.Application.DomainEvents;
using Aegis.Application.Contracts;
using Aegis.Application.Interfaces;
using Aegis.Domain.Repositories;

namespace Aegis.Application.Features.AuthorizationModels;

public sealed class DeleteAuthorizationModelUseCase
{
    private readonly IStoreRegistry _storeRegistry;
    private readonly IAuthorizationModelRegistry _authorizationModelRegistry;
    private readonly IAuthorizationModelRepository? _authorizationModelRepository;
    private readonly IDomainEventDispatcher? _domainEventDispatcher;

    public DeleteAuthorizationModelUseCase(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRegistry authorizationModelRegistry,
        IAuthorizationModelRepository authorizationModelRepository,
        IDomainEventDispatcher domainEventDispatcher)
        : this(storeRegistry, authorizationModelRegistry, authorizationModelRepository, domainEventDispatcher, false)
    {
    }

    private DeleteAuthorizationModelUseCase(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRegistry authorizationModelRegistry,
        IAuthorizationModelRepository? authorizationModelRepository,
        IDomainEventDispatcher? domainEventDispatcher,
        bool compatibilityPath)
    {
        _ = compatibilityPath;
        _storeRegistry = storeRegistry;
        _authorizationModelRegistry = authorizationModelRegistry;
        _authorizationModelRepository = authorizationModelRepository;
        _domainEventDispatcher = domainEventDispatcher;
    }

    internal static DeleteAuthorizationModelUseCase CreateCompatibility(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRegistry authorizationModelRegistry,
        IAuthorizationModelRepository? authorizationModelRepository,
        IDomainEventDispatcher? domainEventDispatcher)
    {
        return new DeleteAuthorizationModelUseCase(
            storeRegistry,
            authorizationModelRegistry,
            authorizationModelRepository,
            domainEventDispatcher,
            true);
    }

    public async Task<bool> ExecuteAsync(
        string storeId,
        string authorizationModelId,
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationModelId))
        {
            throw new ArgumentException("authorizationModelId is required.");
        }

        await EnsureStoreExistsAsync(storeId, cancellationToken);

        if (_authorizationModelRepository is null)
        {
            var current = await _authorizationModelRegistry.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            if (current is not null && current.Revision != expectedRevision)
            {
                throw CreateConflict();
            }

            return await _authorizationModelRegistry.DeleteAsync(storeId, authorizationModelId, cancellationToken);
        }

        var existing = await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        existing.MarkDeleted();
        var deleted = await _authorizationModelRepository.DeleteAsync(existing, expectedRevision, cancellationToken);
        if (!deleted
            && await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken) is not null)
        {
            throw CreateConflict();
        }

        if (deleted)
        {
            await _domainEventDispatcher.DispatchAndClearAsync(existing, cancellationToken);
        }

        return deleted;
    }

    private async Task EnsureStoreExistsAsync(string storeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storeId))
        {
            throw new ArgumentException("storeId is required.");
        }

        if (await _storeRegistry.GetAsync(storeId, cancellationToken) is null)
        {
            throw new ArgumentException("Store not found.");
        }
    }

    private static ConcurrencyConflictException CreateConflict()
    {
        return new ConcurrencyConflictException("The authorization model was modified by another request.");
    }
}
