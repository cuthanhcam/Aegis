using Aegis.SharedKernel;

namespace Aegis.Domain.Events
{
    /// <summary>
    /// Raised when a relationship is deleted.
    /// </summary>
    public sealed class RelationshipDeletedDomainEvent : DomainEvent
    {
        /// <summary>
        /// Tenant identifier where the relationship was deleted.
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// Subject reference in tuple format.
        /// </summary>
        public string Subject { get; }

        /// <summary>
        /// Relation name in tuple format.
        /// </summary>
        public string Relation { get; }

        /// <summary>
        /// Object reference in tuple format.
        /// </summary>
        public string Object { get; }

        /// <summary>
        /// Domain timestamp when deletion occurred.
        /// </summary>
        public DateTimeOffset DeletedAt { get; }

        public RelationshipDeletedDomainEvent(
            string tenantId,
            string subject,
            string relation,
            string obj,
            DateTimeOffset deletedAt)
        {
            TenantId = tenantId;
            Subject = subject;
            Relation = relation;
            Object = obj;
            DeletedAt = deletedAt;
            OccurredOn = deletedAt.UtcDateTime;
        }
    }
}
