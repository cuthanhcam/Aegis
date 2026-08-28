using Aegis.Application.DomainEvents;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Domain.Entities;
using Aegis.Domain.Repositories;

namespace Aegis.Application.Features.AuthorizationModels;

public sealed class CreateAuthorizationModelUseCase
{
    private readonly IStoreRegistry _storeRegistry;
    private readonly IAuthorizationModelRepository _authorizationModelRepository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly AuthorizationModelValidator _validator;

    public CreateAuthorizationModelUseCase(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRepository authorizationModelRepository,
        IDomainEventDispatcher domainEventDispatcher,
        AuthorizationModelValidator validator)
    {
        _storeRegistry = storeRegistry;
        _authorizationModelRepository = authorizationModelRepository;
        _domainEventDispatcher = domainEventDispatcher;
        _validator = validator;
    }

    public async Task<AuthorizationModelDto> ExecuteAsync(
        string storeId,
        CreateAuthorizationModelRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateDefinition(request, cancellationToken);
        await EnsureStoreExistsAsync(storeId, cancellationToken);

        var authorizationModel = CreateValidatedModel(storeId, request);
        await _authorizationModelRepository.AddAsync(authorizationModel, cancellationToken);
        await _domainEventDispatcher.DispatchAndClearAsync(authorizationModel, cancellationToken);
        return ToDto(authorizationModel);
    }

    public async Task<AuthorizationModelDto> ExecuteIdempotentAsync(
        string storeId,
        CreateAuthorizationModelRequestDto request,
        string tenantId,
        string actorId,
        string idempotencyKey,
        string requestFingerprint,
        CancellationToken cancellationToken = default)
    {
        ValidateIdempotencyContext(tenantId, actorId, idempotencyKey, requestFingerprint);
        ValidateDefinition(request, cancellationToken);
        await EnsureStoreExistsAsync(storeId, cancellationToken);

        var authorizationModel = CreateValidatedModel(storeId, request);
        var mutation = new IdempotentMutation(
            tenantId,
            actorId,
            "authorization-model.create",
            idempotencyKey,
            requestFingerprint,
            DateTimeOffset.UtcNow.AddHours(24));
        var result = await _authorizationModelRepository.AddIdempotentAsync(
            authorizationModel,
            mutation,
            cancellationToken);
        if (result.Created)
        {
            await _domainEventDispatcher.DispatchAndClearAsync(authorizationModel, cancellationToken);
        }

        return ToDto(result.AuthorizationModel);
    }

    private void ValidateDefinition(
        CreateAuthorizationModelRequestDto request,
        CancellationToken cancellationToken)
    {
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

    private static AuthorizationModel CreateValidatedModel(
        string storeId,
        CreateAuthorizationModelRequestDto request)
    {
        var authorizationModel = AuthorizationModel.Create(storeId, request.SchemaVersion, request.Model);
        authorizationModel.MarkValidated();
        return authorizationModel;
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
