using Aegis.Application.Contracts;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Domain.Entities;
using Aegis.Domain.Repositories;

namespace Aegis.Application.Features.AuthorizationModels;

public sealed class PublishAuthorizationModelUseCase
{
    private readonly IStoreRegistry _storeRegistry;
    private readonly IAuthorizationModelRepository _authorizationModelRepository;
    private readonly AuthorizationModelValidator _validator;

    public PublishAuthorizationModelUseCase(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRepository authorizationModelRepository,
        AuthorizationModelValidator validator)
    {
        _storeRegistry = storeRegistry;
        _authorizationModelRepository = authorizationModelRepository;
        _validator = validator;
    }

    public async Task<PublishAuthorizationModelResponseDto?> ExecuteAsync(
        string storeId,
        string authorizationModelId,
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        ValidateModelId(authorizationModelId);
        await EnsureStoreExistsAsync(storeId, cancellationToken);
        var model = await GetByIdAsync(storeId, authorizationModelId, cancellationToken);
        if (model is null)
        {
            return null;
        }

        if (model.Revision != expectedRevision)
        {
            throw new ConcurrencyConflictException("The authorization model lifecycle changed before publish started.");
        }

        ThrowIfInvalid(model, cancellationToken);

        var updated = await _authorizationModelRepository.PublishAsync(
            storeId,
            authorizationModelId,
            expectedRevision,
            cancellationToken);
        var published = updated.FirstOrDefault(item =>
            item.Id.Equals(authorizationModelId, StringComparison.OrdinalIgnoreCase));
        if (published is null
            && await _authorizationModelRepository.GetByIdAsync(storeId, authorizationModelId, cancellationToken) is not null)
        {
            throw new ConcurrencyConflictException("The authorization model lifecycle changed before publish completed.");
        }

        return published is null
            ? null
            : new PublishAuthorizationModelResponseDto(ToDto(published), published.Id, published.SchemaVersion);
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
