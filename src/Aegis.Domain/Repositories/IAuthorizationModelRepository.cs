using Aegis.Domain.Entities;

namespace Aegis.Domain.Repositories
{
    /// <summary>
    /// Repository contract for authorization model aggregate persistence.
    /// </summary>
    public interface IAuthorizationModelRepository
    {
        /// <summary>
        /// Adds a new authorization model.
        /// </summary>
        Task AddAsync(AuthorizationModel authorizationModel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all models associated with a store.
        /// </summary>
        Task<IReadOnlyList<AuthorizationModel>> ListByStoreAsync(string storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the latest model for a store.
        /// </summary>
        Task<AuthorizationModel?> GetLatestByStoreAsync(string storeId, CancellationToken cancellationToken = default);

        Task<AuthorizationModel?> GetByIdAsync(string storeId, string authorizationModelId, CancellationToken cancellationToken = default);

        Task<AuthorizationModel?> UpdateAsync(AuthorizationModel authorizationModel, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(AuthorizationModel authorizationModel, CancellationToken cancellationToken = default);
    }
}
