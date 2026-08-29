using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;

namespace Aegis.Application.Services
{
    public sealed class AssertionAppService : IAssertionAppService
    {
        private readonly IStoreRegistry _storeRegistry;
        private readonly IAuthorizationModelRegistry _authorizationModelRegistry;
        private readonly IAssertionRepository _assertionRepository;
        private readonly IAssertionRunStore _assertionRunStore;

        public AssertionAppService(
            IStoreRegistry storeRegistry,
            IAuthorizationModelRegistry authorizationModelRegistry,
            IAssertionRepository assertionRepository,
            IAssertionRunStore assertionRunStore)
        {
            _storeRegistry = storeRegistry;
            _authorizationModelRegistry = authorizationModelRegistry;
            _assertionRepository = assertionRepository;
            _assertionRunStore = assertionRunStore;
        }

        public async Task<AegisCompatReadAssertionsResponseDto> ReadAsync(
            string storeId,
            string authorizationModelId,
            CancellationToken cancellationToken = default)
        {
            await EnsureStoreExists(storeId, cancellationToken);
            if (string.IsNullOrWhiteSpace(authorizationModelId))
            {
                throw new CompatibilityApiException(400, "validation_error", "authorization_model_id is required.");
            }

            await EnsureModelExists(storeId, authorizationModelId, cancellationToken);

            var snapshot = await _assertionRepository.ReadAsync(storeId, authorizationModelId, cancellationToken);
            return new AegisCompatReadAssertionsResponseDto(authorizationModelId, snapshot.Assertions);
        }

        public async Task<AegisAssertionRunListResponseDto> ListRunsAsync(
            string storeId,
            string authorizationModelId,
            CancellationToken cancellationToken = default)
        {
            await EnsureStoreExists(storeId, cancellationToken);
            await EnsureModelExists(storeId, authorizationModelId, cancellationToken);
            var runs = await _assertionRunStore.ListByModelAsync(storeId, authorizationModelId, 25, cancellationToken);

            return new AegisAssertionRunListResponseDto(runs);
        }

        public async Task<AegisAssertionRunRecordDto?> GetRunAsync(
            string storeId,
            string runId,
            CancellationToken cancellationToken = default)
        {
            await EnsureStoreExists(storeId, cancellationToken);
            return await _assertionRunStore.GetAsync(storeId, runId, cancellationToken);
        }

        public async Task PurgeStoreAsync(string storeId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                throw new ArgumentException("storeId is required.");
            }

            await _assertionRepository.PurgeStoreAsync(storeId, cancellationToken);
            await _assertionRunStore.PurgeStoreAsync(storeId, cancellationToken);
        }

        private async Task<StoreDto> EnsureStoreExists(string storeId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                throw new CompatibilityApiException(400, "validation_error", "store_id is required.");
            }

            var store = await _storeRegistry.GetAsync(storeId, cancellationToken);
            if (store is null)
            {
                throw new CompatibilityApiException(404, "store_id_not_found", "Store ID not found.");
            }

            return store;
        }

        private async Task<AuthorizationModelDto> EnsureModelExists(string storeId, string authorizationModelId, CancellationToken cancellationToken)
        {
            var model = await _authorizationModelRegistry.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            if (model is null)
            {
                throw new CompatibilityApiException(
                    400,
                    "authorization_model_not_found",
                    $"Authorization Model '{authorizationModelId}' not found");
            }

            return model;
        }

    }
}
