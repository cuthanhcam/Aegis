using Aegis.Application.DomainEvents;
using Aegis.Application.Contracts;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Domain.Entities;
using Aegis.Domain.Repositories;

namespace Aegis.Application.Features.AuthorizationModels;

public sealed class UpdateAuthorizationModelUseCase
{
    private readonly IStoreRegistry _storeRegistry;
    private readonly IAuthorizationModelRegistry _authorizationModelRegistry;
    private readonly IAuthorizationModelRepository? _authorizationModelRepository;
    private readonly IDomainEventDispatcher? _domainEventDispatcher;
    private readonly AuthorizationModelValidator _validator;

    public UpdateAuthorizationModelUseCase(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRegistry authorizationModelRegistry,
        IAuthorizationModelRepository authorizationModelRepository,
        IDomainEventDispatcher domainEventDispatcher,
        AuthorizationModelValidator validator)
        : this(storeRegistry, authorizationModelRegistry, authorizationModelRepository, domainEventDispatcher, validator, false)
    {
    }

    private UpdateAuthorizationModelUseCase(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRegistry authorizationModelRegistry,
        IAuthorizationModelRepository? authorizationModelRepository,
        IDomainEventDispatcher? domainEventDispatcher,
        AuthorizationModelValidator validator,
        bool compatibilityPath)
    {
        _ = compatibilityPath;
        _storeRegistry = storeRegistry;
        _authorizationModelRegistry = authorizationModelRegistry;
        _authorizationModelRepository = authorizationModelRepository;
        _domainEventDispatcher = domainEventDispatcher;
        _validator = validator;
    }

    internal static UpdateAuthorizationModelUseCase CreateCompatibility(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRegistry authorizationModelRegistry,
        IAuthorizationModelRepository? authorizationModelRepository,
        IDomainEventDispatcher? domainEventDispatcher,
        AuthorizationModelValidator validator)
    {
        return new UpdateAuthorizationModelUseCase(
            storeRegistry,
            authorizationModelRegistry,
            authorizationModelRepository,
            domainEventDispatcher,
            validator,
            true);
    }

    public async Task<AuthorizationModelDto?> ExecuteAsync(
        string storeId,
        string authorizationModelId,
        CreateAuthorizationModelRequestDto request,
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        ValidateCommand(authorizationModelId, request, cancellationToken);
        await EnsureStoreExistsAsync(storeId, cancellationToken);

        if (_authorizationModelRepository is null)
        {
            var current = await _authorizationModelRegistry.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            if (current is not null && current.Revision != expectedRevision)
            {
                throw CreateConflict();
            }

            return await _authorizationModelRegistry.UpdateAsync(
                storeId,
                authorizationModelId,
                request.SchemaVersion,
                request.Model,
                cancellationToken);
        }

        var existing = await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        existing.UpdateDefinition(request.SchemaVersion, request.Model);
        existing.MarkValidated();
        var updated = await _authorizationModelRepository.UpdateAsync(existing, expectedRevision, cancellationToken);
        if (updated is null)
        {
            if (await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken) is not null)
            {
                throw CreateConflict();
            }

            return null;
        }

        await _domainEventDispatcher.DispatchAndClearAsync(existing, cancellationToken);
        return ToDto(updated);
    }

    private void ValidateCommand(
        string authorizationModelId,
        CreateAuthorizationModelRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authorizationModelId))
        {
            throw new ArgumentException("authorizationModelId is required.");
        }

        ArgumentNullException.ThrowIfNull(request);
        var validation = _validator.Validate(
            new ValidateAuthorizationModelRequestDto(request.SchemaVersion, request.Model),
            cancellationToken);
        if (!validation.Valid)
        {
            throw new ArgumentException(string.Join(" ", validation.Errors.Select(error => error.Message)));
        }
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

    private static AuthorizationModelDto ToDto(AuthorizationModel authorizationModel)
    {
        return new AuthorizationModelDto(
            authorizationModel.Id,
            authorizationModel.StoreId,
            authorizationModel.SchemaVersion,
            authorizationModel.Model,
            authorizationModel.CreatedAt,
            authorizationModel.State,
            authorizationModel.PublishedAt,
            authorizationModel.ArchivedAt,
            authorizationModel.SupersededBy,
            authorizationModel.Revision);
    }
}
