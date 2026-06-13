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
            CancellationToken cancellationToken = default)
        {
            return QueryAsync(tenantId, subject, relation, obj, effect, cancellationToken, storeId: null);
        }

        Task<IReadOnlyList<RelationshipTuple>> QueryAsync(
            string tenantId,
            Subject? subject,
            string? relation,
            ObjectRef? obj,
            RelationshipEffect? effect,
            CancellationToken cancellationToken,
            string? storeId)
        {
            return QueryAsync(tenantId, subject, relation, obj, effect, cancellationToken);
        }

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
            CancellationToken cancellationToken = default)
        {
            return QueryMultipleAsync(tenantId, queries, cancellationToken, storeId: null);
        }

        Task<IReadOnlyList<IReadOnlyList<RelationshipTuple>>> QueryMultipleAsync(
            string tenantId,
            IReadOnlyList<(Subject? subject, string? relation, ObjectRef? obj, RelationshipEffect? effect)> queries,
            CancellationToken cancellationToken,
            string? storeId)
        {
            return QueryMultipleAsync(tenantId, queries, cancellationToken);
        }

        /// <summary>
        /// Inserts or updates a relationship tuple.
        /// </summary>
        Task UpsertAsync(
            string tenantId,
            RelationshipTuple tuple,
            CancellationToken cancellationToken = default)
        {
            return UpsertAsync(tenantId, tuple, cancellationToken, storeId: null);
        }

        Task UpsertAsync(
            string tenantId,
            RelationshipTuple tuple,
            CancellationToken cancellationToken,
            string? storeId)
        {
            return UpsertAsync(tenantId, tuple, cancellationToken);
        }

        /// <summary>
        /// Deletes one relationship tuple if it exists.
        /// </summary>
        Task<bool> DeleteAsync(
            string tenantId,
            Subject subject,
            string relation,
            ObjectRef obj,
            CancellationToken cancellationToken = default)
        {
            return DeleteAsync(tenantId, subject, relation, obj, cancellationToken, storeId: null);
        }

        Task<bool> DeleteAsync(
            string tenantId,
            Subject subject,
            string relation,
            ObjectRef obj,
            CancellationToken cancellationToken,
            string? storeId)
        {
            return DeleteAsync(tenantId, subject, relation, obj, cancellationToken);
        }

        /// <summary>
        /// Reads relationship mutation history using offset/limit pagination.
        /// </summary>
        Task<IReadOnlyList<RelationshipChange>> ReadChangesAsync(
            string tenantId,
            int offset,
            int limit,
            CancellationToken cancellationToken = default)
        {
            return ReadChangesAsync(tenantId, offset, limit, cancellationToken, storeId: null);
        }

        Task<IReadOnlyList<RelationshipChange>> ReadChangesAsync(
            string tenantId,
            int offset,
            int limit,
            CancellationToken cancellationToken,
            string? storeId)
        {
            return ReadChangesAsync(tenantId, offset, limit, cancellationToken);
        }

        /// <summary>
        /// Removes all relationship data for a tenant.
        /// </summary>
        Task PurgeTenantAsync(
            string tenantId,
            CancellationToken cancellationToken = default);

        Task PurgeStoreAsync(
            string tenantId,
            string storeId,
            CancellationToken cancellationToken = default)
        {
            return PurgeTenantAsync(tenantId, cancellationToken);
        }
    }
}
