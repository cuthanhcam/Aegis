using Aegis.Application.Interfaces;
using Aegis.Contracts.Compatibility;

namespace Aegis.Application.Features.Assertions;

public sealed class AssertionScopeGuard
{
    private readonly IStoreRegistry _storeRegistry;
    private readonly IAuthorizationModelRegistry _authorizationModelRegistry;

    public AssertionScopeGuard(
        IStoreRegistry storeRegistry,
        IAuthorizationModelRegistry authorizationModelRegistry)
    {
        _storeRegistry = storeRegistry;
        _authorizationModelRegistry = authorizationModelRegistry;
    }

    public async Task EnsureStoreAsync(string storeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storeId))
        {
            throw new CompatibilityApiException(400, "validation_error", "store_id is required.");
        }

        if (await _storeRegistry.GetAsync(storeId, cancellationToken) is null)
        {
            throw new CompatibilityApiException(404, "store_id_not_found", "Store ID not found.");
        }
    }

    public async Task EnsureModelAsync(
        string storeId,
        string authorizationModelId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authorizationModelId))
        {
            throw new CompatibilityApiException(400, "validation_error", "authorization_model_id is required.");
        }

        if (await _authorizationModelRegistry.GetByIdAsync(storeId, authorizationModelId, cancellationToken) is null)
        {
            throw new CompatibilityApiException(
                400,
                "authorization_model_not_found",
                $"Authorization Model '{authorizationModelId}' not found");
        }
    }
}
