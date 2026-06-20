using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;

namespace Aegis.Infrastructure.Authorization
{
    public sealed class AuthorizationModelProvider : IAuthorizationModelProvider
    {
        private readonly IAuthorizationModelRegistry _registry;

        public AuthorizationModelProvider(IAuthorizationModelRegistry registry)
        {
            _registry = registry;
        }

        public async Task<string?> GetLatestModelAsync(string storeId, CancellationToken cancellationToken = default)
        {
            var model = await _registry.GetPublishedAsync(storeId, cancellationToken)
                ?? await _registry.GetLatestAsync(storeId, cancellationToken);
            return model?.Model;
        }

        public async Task<string?> GetModelAsync(string storeId, string authorizationModelId, CancellationToken cancellationToken = default)
        {
            var model = await _registry.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            return model?.Model;
        }
    }
}
