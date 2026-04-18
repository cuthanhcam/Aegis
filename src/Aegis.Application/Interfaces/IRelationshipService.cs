using Aegis.Contracts.Relationships;

namespace Aegis.Application.Interfaces
{
    /// <summary>
    /// Application boundary for relationship tuple operations and change history.
    /// </summary>
    public interface IRelationshipService
    {
        /// <summary>
        /// Queries relationship tuples with optional filters.
        /// </summary>
        Task<IReadOnlyList<RelationshipTupleDto>> QueryAsync(
            string tenantId,
            string? subject,
            string? relation,
            string? objectRef,
            string? effect,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates or updates a relationship tuple.
        /// </summary>
        Task UpsertAsync(
            string tenantId,
            RelationshipWriteRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a relationship tuple.
        /// </summary>
        Task<bool> DeleteAsync(
            string tenantId,
            RelationshipDeleteRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads paged relationship change history.
        /// </summary>
        Task<ReadChangesResponseDto> ReadChangesAsync(
            string tenantId,
            ReadChangesRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
