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
