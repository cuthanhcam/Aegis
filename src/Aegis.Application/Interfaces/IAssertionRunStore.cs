using Aegis.Contracts.Compatibility;

namespace Aegis.Application.Interfaces
{
    public interface IAssertionRunStore
    {
        Task SaveAsync(AegisAssertionRunRecordDto record, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AegisAssertionRunRecordDto>> ListByModelAsync(
            string storeId,
            string authorizationModelId,
            int limit = 25,
            CancellationToken cancellationToken = default);

        Task<AegisAssertionRunRecordDto?> GetAsync(
            string storeId,
            string runId,
            CancellationToken cancellationToken = default);

        Task PurgeStoreAsync(string storeId, CancellationToken cancellationToken = default);
    }
}
