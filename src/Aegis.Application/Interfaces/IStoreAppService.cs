using Aegis.Contracts.Administration;

namespace Aegis.Application.Interfaces;

public interface IStoreAppService
{
    Task<IReadOnlyList<StoreDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoreDto>> ListAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<StoreDto?> GetByIdAsync(
        string storeId,
        CancellationToken cancellationToken = default);

    Task<StoreDto?> GetByIdAsync(
        string tenantId,
        string storeId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string storeId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string tenantId,
        string storeId,
        CancellationToken cancellationToken = default);
}
