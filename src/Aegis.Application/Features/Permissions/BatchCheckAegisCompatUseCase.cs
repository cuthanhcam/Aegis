using Aegis.Application.Features.Query;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Compatibility;
using Aegis.Contracts.Permissions;

namespace Aegis.Application.Features.Permissions
{
    public sealed class BatchCheckAegisCompatUseCase
    {
        private readonly IStoreRegistry _storeRegistry;
        private readonly ResolveAuthorizationModelUseCase _resolveAuthorizationModelUseCase;
        private readonly CheckPermissionUseCase _checkPermissionUseCase;

        public BatchCheckAegisCompatUseCase(
            IStoreRegistry storeRegistry,
            ResolveAuthorizationModelUseCase resolveAuthorizationModelUseCase,
            CheckPermissionUseCase checkPermissionUseCase)
        {
            _storeRegistry = storeRegistry;
            _resolveAuthorizationModelUseCase = resolveAuthorizationModelUseCase;
            _checkPermissionUseCase = checkPermissionUseCase;
        }

        public async Task<AegisCompatBatchCheckResponseDto> ExecuteAsync(
            string storeId,
            AegisCompatBatchCheckRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.Checks is null || request.Checks.Count == 0)
            {
                throw new ArgumentException("checks are required.");
            }

            var store = await _storeRegistry.GetAsync(storeId, cancellationToken);
            if (store is null)
            {
                throw new CompatibilityApiException(404, "store_id_not_found", "Store ID not found.");
            }

            var results = new List<AegisCompatBatchCheckResultItemDto>(request.Checks.Count);
            foreach (var check in request.Checks)
            {
                try
                {
                    var effectiveAuthorizationModelId = !string.IsNullOrWhiteSpace(check.AuthorizationModelId)
                        ? check.AuthorizationModelId
                        : request.AuthorizationModelId;

                    var resolvedAuthorizationModelId = await _resolveAuthorizationModelUseCase.ExecuteAsync(
                        storeId,
                        effectiveAuthorizationModelId,
                        cancellationToken);

                    var decision = await _checkPermissionUseCase.ExecuteAsync(
                        storeId,
                        new CheckRequestDto(
                            check.TupleKey.User,
                            check.TupleKey.Relation,
                            check.TupleKey.Object,
                            AuthorizationQueryHelper.ToContextualTuples(check.ContextualTuples),
                            check.Consistency,
                            resolvedAuthorizationModelId,
                            check.Context),
                        includeTrace: false,
                        cancellationToken);

                    results.Add(new AegisCompatBatchCheckResultItemDto(check.CorrelationId, decision.Allowed));
                }
                catch (CompatibilityApiException ex)
                {
                    results.Add(new AegisCompatBatchCheckResultItemDto(
                        check.CorrelationId,
                        null,
                        new AegisCompatErrorResponseDto(ex.ErrorCode, ex.Message)));
                }
                catch (ArgumentException ex)
                {
                    results.Add(new AegisCompatBatchCheckResultItemDto(
                        check.CorrelationId,
                        null,
                        new AegisCompatErrorResponseDto("validation_error", ex.Message)));
                }
                catch (Exception)
                {
                    results.Add(new AegisCompatBatchCheckResultItemDto(
                        check.CorrelationId,
                        null,
                        new AegisCompatErrorResponseDto("internal_error", "Unexpected error occurred.")));
                }
            }

            return new AegisCompatBatchCheckResponseDto(results);
        }
    }
}
