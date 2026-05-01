using Aegis.Contracts.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aegis.Application.Features.Permissions
{
    public sealed class BatchCheckInStoreUseCase
    {
        private const int MaxBatchSize = 1000;
        private readonly CheckPermissionUseCase _checkPermissionUseCase;

        public BatchCheckInStoreUseCase(CheckPermissionUseCase checkPermissionUseCase)
        {
            _checkPermissionUseCase = checkPermissionUseCase ?? throw new ArgumentNullException(nameof(checkPermissionUseCase));
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

            var results = new List<BatchCheckItemResultDto>(request.Items.Count);
            for (var i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                var correlationId = string.IsNullOrWhiteSpace(item.CorrelationId)
                    ? (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : item.CorrelationId;

                var result = await _checkPermissionUseCase.ExecuteAsync(
                    storeId,
                    new CheckRequestDto(
                        item.User,
                        item.Relation,
                        item.Object,
                        item.ContextualTuples,
                        item.Consistency,
                        item.AuthorizationModelId,
                        item.Context),
                    includeTrace: false,
                    cancellationToken);

                results.Add(new BatchCheckItemResultDto(correlationId, result));
            }

            return new BatchCheckResponseDto(results);
        }
    }
}
