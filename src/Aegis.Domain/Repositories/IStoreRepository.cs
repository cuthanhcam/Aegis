using Aegis.Domain.Entities;

namespace Aegis.Domain.Repositories
{
    /// <summary>
    /// Repository contract for store aggregate persistence.
    /// </summary>
    public interface IStoreRepository
    {
        /// <summary>
        /// Adds a new store.
        /// </summary>
        Task AddAsync(Store store, CancellationToken cancellationToken = default);

        Task<IdempotentStoreAddResult> AddIdempotentAsync(
            Store store,
            IdempotentMutation mutation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a store by identifier.
        /// </summary>
        Task<Store?> GetByIdAsync(string storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all stores.
        /// </summary>
        Task<IReadOnlyList<Store>> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a store.
        /// </summary>
        Task<bool> DeleteAsync(Store store, CancellationToken cancellationToken = default);
    }
}
