using Aegis.Contracts.Permissions;

namespace Aegis.Application.Features.Permissions
{
    public sealed class BatchCheckInStoreUseCase
    {
        private const int MaxBatchSize = 1000;
        private readonly CheckPermissionUseCase _checkPermissionUseCase;
        private readonly ResolveAuthorizationModelUseCase _resolveAuthorizationModelUseCase;

        public BatchCheckInStoreUseCase(
            CheckPermissionUseCase checkPermissionUseCase,
            ResolveAuthorizationModelUseCase resolveAuthorizationModelUseCase)
        {
            _checkPermissionUseCase = checkPermissionUseCase ?? throw new ArgumentNullException(nameof(checkPermissionUseCase));
            _resolveAuthorizationModelUseCase = resolveAuthorizationModelUseCase ?? throw new ArgumentNullException(nameof(resolveAuthorizationModelUseCase));
        }

        public async Task<BatchCheckResponseDto> ExecuteAsync(
            string storeId,
            BatchCheckRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Items is null || request.Items.Count == 0)
            {
                throw new ArgumentException("items are required.");
            }

            if (request.Items.Count > MaxBatchSize)
            {
                throw new ArgumentException($"items must not exceed {MaxBatchSize}.");
            }

            await _resolveAuthorizationModelUseCase.EnsureStoreAndValidateRequestedAsync(
                storeId,
                requestedAuthorizationModelId: null,
                cancellationToken);

            var results = new List<BatchCheckItemResultDto>(request.Items.Count);
            for (var i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                var correlationId = string.IsNullOrWhiteSpace(item.CorrelationId)
                    ? (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : item.CorrelationId;

                var validatedAuthorizationModelId = await _resolveAuthorizationModelUseCase.EnsureStoreAndValidateRequestedAsync(
                    storeId,
                    item.AuthorizationModelId,
                    cancellationToken);

                var result = await _checkPermissionUseCase.ExecuteAsync(
                    storeId,
                    new CheckRequestDto(
                        item.User,
                        item.Relation,
                        item.Object,
                        item.ContextualTuples,
                        item.Consistency,
                        validatedAuthorizationModelId,
                        item.Context),
                    includeTrace: false,
                    cancellationToken);

                results.Add(new BatchCheckItemResultDto(correlationId, result));
            }

            return new BatchCheckResponseDto(results);
        }
    }
}
