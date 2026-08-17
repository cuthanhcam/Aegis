using Aegis.Contracts.Permissions;
using Aegis.Contracts.Common;

namespace Aegis.Application.Features.Permissions
{
    public sealed class BatchCheckInStoreUseCase
    {
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
            return await ExecuteCoreAsync(
                storeId,
                storeId,
                request,
                (itemAuthorizationModelId, ct) => _resolveAuthorizationModelUseCase.EnsureStoreAndValidateRequestedAsync(storeId, itemAuthorizationModelId, ct),
                cancellationToken);
        }

        public async Task<BatchCheckResponseDto> ExecuteAsync(
            string tenantId,
            string storeId,
            BatchCheckRequestDto request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteCoreAsync(
                tenantId,
                storeId,
                request,
                (itemAuthorizationModelId, ct) => _resolveAuthorizationModelUseCase.EnsureStoreAndValidateRequestedAsync(tenantId, storeId, itemAuthorizationModelId, ct),
                cancellationToken);
        }

        private async Task<BatchCheckResponseDto> ExecuteCoreAsync(
            string tenantId,
            string storeId,
            BatchCheckRequestDto request,
            Func<string?, CancellationToken, Task<string?>> resolveAuthorizationModelIdAsync,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Items is null || request.Items.Count == 0)
            {
                throw new ArgumentException("items are required.");
            }

            if (request.Items.Count > ApiRequestLimits.MaxBatchChecks)
            {
                throw new ArgumentException($"items must not exceed {ApiRequestLimits.MaxBatchChecks}.");
            }

            await resolveAuthorizationModelIdAsync(null, cancellationToken);

            var results = new List<BatchCheckItemResultDto>(request.Items.Count);
            for (var i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                var correlationId = string.IsNullOrWhiteSpace(item.CorrelationId)
                    ? (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : item.CorrelationId;

                var validatedAuthorizationModelId = await resolveAuthorizationModelIdAsync(item.AuthorizationModelId, cancellationToken);

                var result = await _checkPermissionUseCase.ExecuteAsync(
                    tenantId,
                    new CheckRequestDto(
                        item.User,
                        item.Relation,
                        item.Object,
                        item.ContextualTuples,
                        item.Consistency,
                        validatedAuthorizationModelId,
                        item.Context),
                    includeTrace: false,
                    cancellationToken,
                    storeId);

                results.Add(new BatchCheckItemResultDto(correlationId, result));
            }

            return new BatchCheckResponseDto(results);
        }
    }
}
