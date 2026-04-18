using Aegis.Contracts.Administration;

namespace Aegis.Application.Interfaces
{
    /// <summary>
    /// Application boundary for store lifecycle operations.
    /// </summary>
    public interface IStoreRegistry
    {
        /// <summary>
        /// Creates a new store.
        /// </summary>
        Task<StoreDto> CreateAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all stores visible to the current scope.
        /// </summary>
        Task<IReadOnlyList<StoreDto>> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves one store by identifier.
        /// </summary>
        Task<StoreDto?> GetAsync(string storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a store by identifier.
        /// </summary>
        Task<bool> DeleteAsync(string storeId, CancellationToken cancellationToken = default);
    }
}
