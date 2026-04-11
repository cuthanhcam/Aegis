using Aegis.Domain.Entities;
using Aegis.Domain.Enums;

namespace Aegis.Domain.Repositories
{
    /// <summary>
    /// Repository contract for relationship tuples and their change feed.
    /// </summary>
    public interface IRelationshipRepository
    {
        /// <summary>
        /// Queries relationship tuples using optional tuple filters.
        /// </summary>
        Task<IReadOnlyList<Relationship>> QueryAsync(
            string tenantId,
            string? subject,
            string? relation,
            string? obj,
            RelationshipPermissionEffect? effect,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts a new tuple or updates an existing one.
        /// </summary>
        Task UpsertAsync(Relationship relationship, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a tuple by its unique tuple key.
        /// </summary>
        Task<bool> DeleteAsync(string tenantId, string subject, string relation, string obj, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads relationship change entries for the tenant.
        /// </summary>
        Task<IReadOnlyList<RelationshipChangeEntry>> ReadChangesAsync(string tenantId, int offset, int limit, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all relationship data for a tenant.
        /// </summary>
        Task PurgeTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    }
}
