using Aegis.Application.Contracts;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Administration;
using Aegis.Domain.Entities;
using Aegis.Domain.Repositories;

namespace Aegis.Application.Features.AuthorizationModels;

public sealed class RollbackAuthorizationModelUseCase
{
    private readonly IStoreRegistry _storeRegistry;
    private readonly IAuthorizationModelRegistry _authorizationModelRegistry;
    private readonly IAuthorizationModelRepository? _authorizationModelRepository;
    private readonly AuthorizationModelValidator _validator;
    private readonly IAuditStore? _auditStore;

    public RollbackAuthorizationModelUseCase(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRegistry authorizationModelRegistry,
        IAuthorizationModelRepository authorizationModelRepository,
        AuthorizationModelValidator validator,
        IAuditStore auditStore)
        : this(storeRegistry, authorizationModelRegistry, authorizationModelRepository, validator, auditStore, false)
    {
    }

    private RollbackAuthorizationModelUseCase(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRegistry authorizationModelRegistry,
        IAuthorizationModelRepository? authorizationModelRepository,
        AuthorizationModelValidator validator,
        IAuditStore? auditStore,
        bool compatibilityPath)
    {
        _ = compatibilityPath;
        _storeRegistry = storeRegistry;
        _authorizationModelRegistry = authorizationModelRegistry;
        _authorizationModelRepository = authorizationModelRepository;
        _validator = validator;
        _auditStore = auditStore;
    }

    internal static RollbackAuthorizationModelUseCase CreateCompatibility(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRegistry authorizationModelRegistry,
        IAuthorizationModelRepository? authorizationModelRepository,
        AuthorizationModelValidator validator,
        IAuditStore? auditStore)
    {
        return new RollbackAuthorizationModelUseCase(
            storeRegistry,
            authorizationModelRegistry,
            authorizationModelRepository,
            validator,
            auditStore,
            true);
    }

    public async Task<RollbackAuthorizationModelResponseDto?> ExecuteAsync(
        string storeId,
        string authorizationModelId,
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        ValidateModelId(authorizationModelId);
        await EnsureStoreExistsAsync(storeId, cancellationToken);
        var currentPublished = await _authorizationModelRegistry.GetPublishedAsync(storeId, cancellationToken);
        var target = await GetByIdAsync(storeId, authorizationModelId, cancellationToken);
        if (target is null)
        {
            return null;
        }

        if (target.Revision != expectedRevision)
        {
            throw new ConcurrencyConflictException("The authorization model lifecycle changed before rollback started.");
        }

        ThrowIfInvalid(target, cancellationToken);

        AuthorizationModelDto? active;
        if (_authorizationModelRepository is not null)
        {
            var rolledBack = await _authorizationModelRepository.RollbackAsync(
                storeId,
                authorizationModelId,
                expectedRevision,
                cancellationToken);
            if (rolledBack is null
                && await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken) is not null)
            {
                throw new ConcurrencyConflictException("The authorization model lifecycle changed before rollback completed.");
            }

            active = rolledBack is null ? null : ToDto(rolledBack);
        }
        else
        {
            if (currentPublished is not null
                && !currentPublished.Id.Equals(authorizationModelId, StringComparison.OrdinalIgnoreCase))
            {
                await _authorizationModelRegistry.UpdateStateAsync(
                    storeId,
                    currentPublished.Id,
                    AuthorizationModelLifecycleStates.Archived,
                    currentPublished.PublishedAt,
                    DateTimeOffset.UtcNow,
                    authorizationModelId,
                    cancellationToken);
            }

            active = await _authorizationModelRegistry.UpdateStateAsync(
                storeId,
                authorizationModelId,
                AuthorizationModelLifecycleStates.Published,
                DateTimeOffset.UtcNow,
                null,
                null,
                cancellationToken);
        }

        if (active is null)
        {
            return null;
        }

        if (_auditStore is not null)
        {
            await _auditStore.WriteAsync(
                new AuditEvent(
                    storeId,
                    "model.rollback",
                    "system",
                    "rollback",
                    authorizationModelId,
                    "Allow",
                    "MODEL_ROLLED_BACK",
                    DateTimeOffset.UtcNow,
                    storeId),
                cancellationToken);
        }

        return new RollbackAuthorizationModelResponseDto(
            active,
            active.Id,
            currentPublished?.Id ?? string.Empty);
    }

    private async Task<AuthorizationModelDto?> GetByIdAsync(
        string storeId,
        string authorizationModelId,
        CancellationToken cancellationToken)
    {
        if (_authorizationModelRepository is null)
        {
            return await _authorizationModelRegistry.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
        }

        var model = await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
        return model is null ? null : ToDto(model);
    }

    private void ThrowIfInvalid(AuthorizationModelDto model, CancellationToken cancellationToken)
    {
        var validation = _validator.Validate(
            new ValidateAuthorizationModelRequestDto(model.SchemaVersion, model.Model),
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

    private static void ValidateModelId(string authorizationModelId)
    {
        if (string.IsNullOrWhiteSpace(authorizationModelId))
        {
            throw new ArgumentException("authorizationModelId is required.");
        }
    }

    private static AuthorizationModelDto ToDto(AuthorizationModel model)
    {
        return new AuthorizationModelDto(
            model.Id,
            model.StoreId,
            model.SchemaVersion,
            model.Model,
            model.CreatedAt,
            model.State,
            model.PublishedAt,
            model.ArchivedAt,
            model.SupersededBy,
            model.Revision);
    }
}
