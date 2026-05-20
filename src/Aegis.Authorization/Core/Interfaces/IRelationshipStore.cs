using Aegis.Authorization.Core.Models;

namespace Aegis.Authorization.Core.Interfaces
{
    /// <summary>
    /// Persistence contract for relationship tuples and change history.
    /// </summary>
    public interface IRelationshipStore
    {
        /// <summary>
        /// Queries relationship tuples using optional subject/relation/object/effect filters.
        /// </summary>
        Task<IReadOnlyList<RelationshipTuple>> QueryAsync(
            string tenantId,
            Subject? subject,
            string? relation,
            ObjectRef? obj,
            RelationshipEffect? effect,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Batch query for multiple relationship tuple sets in a single call to reduce round-trips.
        /// </summary>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="queries">List of query specifications (subject, relation, object, effect tuples).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of result sets in the same order as queries; each inner list is the result for the corresponding query.</returns>
        Task<IReadOnlyList<IReadOnlyList<RelationshipTuple>>> QueryMultipleAsync(
            string tenantId,
            IReadOnlyList<(Subject? subject, string? relation, ObjectRef? obj, RelationshipEffect? effect)> queries,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts or updates a relationship tuple.
        /// </summary>
        Task UpsertAsync(
            string tenantId,
            RelationshipTuple tuple,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes one relationship tuple if it exists.
        /// </summary>
        Task<bool> DeleteAsync(
            string tenantId,
            Subject subject,
            string relation,
            ObjectRef obj,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads relationship mutation history using offset/limit pagination.
        /// </summary>
        Task<IReadOnlyList<RelationshipChange>> ReadChangesAsync(
            string tenantId,
            int offset,
            int limit,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all relationship data for a tenant.
        /// </summary>
        Task PurgeTenantAsync(
            string tenantId,
            CancellationToken cancellationToken = default);
    }
}
