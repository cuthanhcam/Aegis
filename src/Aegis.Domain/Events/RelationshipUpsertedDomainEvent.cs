using Aegis.SharedKernel;

namespace Aegis.Domain.Events
{
    /// <summary>
    /// Raised when a relationship is created or updated.
    /// </summary>
    public sealed class RelationshipUpsertedDomainEvent : DomainEvent
    {
        /// <summary>
        /// Tenant identifier where the relationship belongs.
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
        /// Permission effect serialized for downstream consumers.
        /// </summary>
        public string Effect { get; }

        /// <summary>
        /// Domain timestamp when upsert occurred.
        /// </summary>
        public DateTimeOffset OccurredAt { get; }

        public RelationshipUpsertedDomainEvent(
            string tenantId,
            string subject,
            string relation,
            string obj,
            string effect,
            DateTimeOffset occurredAt)
        {
            TenantId = tenantId;
            Subject = subject;
            Relation = relation;
            Object = obj;
            Effect = effect;
            OccurredAt = occurredAt;
            OccurredOn = occurredAt.UtcDateTime;
        }
    }
}
