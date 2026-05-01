using Aegis.Application.Interfaces;
using Aegis.Contracts.Compatibility;

namespace Aegis.Application.Features.Permissions
{
    public sealed class ResolveAuthorizationModelUseCase
    {
        private readonly IStoreRegistry _storeRegistry;
        private readonly IAuthorizationModelRegistry _authorizationModelRegistry;

        public ResolveAuthorizationModelUseCase(
            IStoreRegistry storeRegistry,
            IAuthorizationModelRegistry authorizationModelRegistry)
        {
            _storeRegistry = storeRegistry ?? throw new ArgumentNullException(nameof(storeRegistry));
            _authorizationModelRegistry = authorizationModelRegistry ?? throw new ArgumentNullException(nameof(authorizationModelRegistry));
        }

        public async Task<string> ExecuteAsync(
            string storeId,
            string? requestedAuthorizationModelId,
            CancellationToken cancellationToken = default)
        {
            var store = await _storeRegistry.GetAsync(storeId, cancellationToken);
            if (store is null)
            {
                throw new CompatibilityApiException(404, "store_id_not_found", "Store ID not found.");
            }

            if (!string.IsNullOrWhiteSpace(requestedAuthorizationModelId))
            {
                var model = await _authorizationModelRegistry.GetByIdAsync(storeId, requestedAuthorizationModelId, cancellationToken);
                if (model is null)
                {
                    throw new CompatibilityApiException(
                        400,
                        "authorization_model_not_found",
                        $"Authorization Model '{requestedAuthorizationModelId}' not found");
                }

                return model.Id;
            }

            var latest = await _authorizationModelRegistry.GetLatestAsync(storeId, cancellationToken);
            if (latest is null)
            {
                throw new CompatibilityApiException(
                    400,
                    "latest_authorization_model_not_found",
                    $"No authorization models found for store '{storeId}'");
            }

            return latest.Id;
        }
    }
}
