using Aegis.Contracts.Compatibility;

namespace Aegis.Application.Interfaces
{
    public interface IAssertionAppService
    {
        Task<AegisCompatReadAssertionsResponseDto> ReadAsync(
            string storeId,
            string authorizationModelId,
            CancellationToken cancellationToken = default);

        Task<AegisAssertionRunListResponseDto> ListRunsAsync(
            string storeId,
            string authorizationModelId,
            CancellationToken cancellationToken = default);

        Task<AegisAssertionRunRecordDto?> GetRunAsync(
            string storeId,
            string runId,
            CancellationToken cancellationToken = default);

        Task PurgeStoreAsync(string storeId, CancellationToken cancellationToken = default);
    }
}
