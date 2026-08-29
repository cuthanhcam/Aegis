using Aegis.Application.Interfaces;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;

namespace Aegis.Application.Features.Assertions;

public sealed class WriteAssertionsUseCase
{
    public const int MaximumAssertionsPerModel = 100;
    private readonly IStoreRegistry _storeRegistry;
    private readonly IAuthorizationModelRegistry _authorizationModelRegistry;
    private readonly IAssertionRepository _assertionRepository;
    private readonly AssertionValidator _validator;

    public WriteAssertionsUseCase(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRegistry authorizationModelRegistry,
        IAssertionRepository assertionRepository,
        AssertionValidator validator)
    {
        _storeRegistry = storeRegistry;
        _authorizationModelRegistry = authorizationModelRegistry;
        _assertionRepository = assertionRepository;
        _validator = validator;
    }

    public async Task ExecuteAsync(
        string storeId,
        string authorizationModelId,
        AegisCompatWriteAssertionsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(storeId))
        {
            throw new CompatibilityApiException(400, "validation_error", "store_id is required.");
        }

        if (await _storeRegistry.GetAsync(storeId, cancellationToken) is null)
        {
            throw new CompatibilityApiException(404, "store_id_not_found", "Store ID not found.");
        }

        if (string.IsNullOrWhiteSpace(authorizationModelId))
        {
            throw new CompatibilityApiException(400, "validation_error", "authorization_model_id is required.");
        }

        var model = await _authorizationModelRegistry.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
        if (model is null)
        {
            throw new CompatibilityApiException(400, "authorization_model_not_found", $"Authorization Model '{authorizationModelId}' not found");
        }

        if (request.Assertions is null)
        {
            throw new CompatibilityApiException(400, "validation_error", "assertions are required.");
        }

        if (request.Assertions.Count > MaximumAssertionsPerModel)
        {
            throw new CompatibilityApiException(
                400,
                "assertions_too_many_items",
                $"assertions exceeds max allowed items of {MaximumAssertionsPerModel}.");
        }

        var relationIndex = _validator.BuildRelationIndex(model.Model);
        foreach (var assertion in request.Assertions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _validator.Validate(assertion, relationIndex);
        }

        await _assertionRepository.ReplaceAsync(storeId, authorizationModelId, request.Assertions, cancellationToken);
    }
}
