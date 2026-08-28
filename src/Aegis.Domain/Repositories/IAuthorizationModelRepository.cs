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

        Task<IdempotentAuthorizationModelAddResult> AddIdempotentAsync(
            AuthorizationModel authorizationModel,
            IdempotentMutation mutation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all models associated with a store.
        /// </summary>
        Task<IReadOnlyList<AuthorizationModel>> ListByStoreAsync(string storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the latest model for a store.
        /// </summary>
        Task<AuthorizationModel?> GetLatestByStoreAsync(string storeId, CancellationToken cancellationToken = default);

        Task<AuthorizationModel?> GetByIdAsync(string storeId, string authorizationModelId, CancellationToken cancellationToken = default);

        Task<AuthorizationModel?> UpdateAsync(AuthorizationModel authorizationModel, long expectedRevision, CancellationToken cancellationToken = default);

        Task<AuthorizationModel?> GetPublishedByStoreAsync(string storeId, CancellationToken cancellationToken = default)
        {
            return GetLatestByStoreAsync(storeId, cancellationToken);
        }

        Task<IReadOnlyList<AuthorizationModel>> PublishAsync(
            string storeId,
            string authorizationModelId,
            long expectedRevision,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Authorization model publishing is not supported by this repository.");
        }

        Task<AuthorizationModel?> RollbackAsync(
            string storeId,
            string authorizationModelId,
            long expectedRevision,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Authorization model rollback is not supported by this repository.");
        }

        Task<bool> DeleteAsync(AuthorizationModel authorizationModel, long expectedRevision, CancellationToken cancellationToken = default);
    }
}
