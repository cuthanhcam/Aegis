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
        Task<StoreDto> CreateAsync(
            string name,
            CancellationToken cancellationToken = default);

        Task<StoreDto> CreateForTenantAsync(
            string tenantId,
            string name,
            CancellationToken cancellationToken = default)
        {
            return CreateAsync(name, cancellationToken);
        }

        /// <summary>
        /// Lists all stores visible to the current scope.
        /// </summary>
        Task<IReadOnlyList<StoreDto>> ListAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StoreDto>> ListForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return ListAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves one store by identifier.
        /// </summary>
        Task<StoreDto?> GetAsync(
            string storeId,
            CancellationToken cancellationToken = default);

        Task<StoreDto?> GetForTenantAsync(
            string tenantId,
            string storeId,
            CancellationToken cancellationToken = default)
        {
            return GetAsync(storeId, cancellationToken);
        }

        /// <summary>
        /// Deletes a store by identifier.
        /// </summary>
        Task<bool> DeleteAsync(
            string storeId,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteForTenantAsync(
            string tenantId,
            string storeId,
            CancellationToken cancellationToken = default)
        {
            return DeleteAsync(storeId, cancellationToken);
        }
    }
}
