namespace Aegis.Authorization.Core.Interfaces
{
    /// <summary>
    /// Reads authorization model definitions for rewrite-based evaluation.
    /// </summary>
    public interface IAuthorizationModelProvider
    {
        /// <summary>
        /// Gets the latest active model for a store.
        /// </summary>
        Task<string?> GetLatestModelAsync(
            string storeId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific model version by identifier.
        /// </summary>
        Task<string?> GetModelAsync(
            string storeId,
            string authorizationModelId,
            CancellationToken cancellationToken = default);
    }
}
