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
    private readonly IAuthorizationModelRepository _authorizationModelRepository;
    private readonly AuthorizationModelValidator _validator;
    private readonly IAuditStore _auditStore;

    public RollbackAuthorizationModelUseCase(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRepository authorizationModelRepository,
        AuthorizationModelValidator validator,
        IAuditStore auditStore)
    {
        _storeRegistry = storeRegistry;
        _authorizationModelRepository = authorizationModelRepository;
        _validator = validator;
        _auditStore = auditStore;
    }

    public async Task<RollbackAuthorizationModelResponseDto?> ExecuteAsync(
        string storeId,
        string authorizationModelId,
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        ValidateModelId(authorizationModelId);
        await EnsureStoreExistsAsync(storeId, cancellationToken);
        var currentPublishedAggregate = await _authorizationModelRepository.GetPublishedByStoreAsync(storeId, cancellationToken);
        var currentPublished = currentPublishedAggregate is null ? null : ToDto(currentPublishedAggregate);
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

        var active = rolledBack is null ? null : ToDto(rolledBack);

        if (active is null)
        {
            return null;
        }

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
