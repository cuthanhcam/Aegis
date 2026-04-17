using Aegis.Contracts.Administration;

namespace Aegis.Application.Interfaces
{
    /// <summary>
    /// Registry boundary for creating, reading, updating, and deleting authorization models.
    /// </summary>
    public interface IAuthorizationModelRegistry
    {
        /// <summary>
        /// Creates a new authorization model inside the given store.
        /// </summary>
        Task<AuthorizationModelDto> CreateAsync(
            string storeId,
            string schemaVersion,
            string model,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all authorization models available for a store.
        /// </summary>
        Task<IReadOnlyList<AuthorizationModelDto>> ListAsync(
            string storeId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the latest authorization model for a store, if one exists.
        /// </summary>
        Task<AuthorizationModelDto?> GetLatestAsync(
            string storeId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific authorization model by identifier.
        /// </summary>
        Task<AuthorizationModelDto?> GetByIdAsync(
            string storeId, string authorizationModelId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing authorization model.
        /// </summary>
        Task<AuthorizationModelDto?> UpdateAsync(
            string storeId,
            string authorizationModelId,
            string schemaVersion,
            string model,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an authorization model from a store.
        /// </summary>
        Task<bool> DeleteAsync(
            string storeId,
            string authorizationModelId,
            CancellationToken cancellationToken = default);
    }
}
