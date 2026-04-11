using Aegis.Domain.Enums;
using Aegis.Domain.Events;
using Aegis.Domain.ValueObjects;
using Aegis.SharedKernel;

namespace Aegis.Domain.Entities
{
    /// <summary>
    /// Aggregate root representing a permission tuple relationship.
    /// </summary>
    public sealed class Relationship : AggregateRoot<Guid>
    {
        public string TenantId { get; private init; } = string.Empty;

        public SubjectId Subject { get; private init; } = null!;

        public RelationName Relation { get; private init; } = null!;

        public ObjectId Object { get; private init; } = null!;

        public RelationshipPermissionEffect Effect { get; private set; }

        public DateTimeOffset CreatedAt { get; private init; }

        public DateTimeOffset UpdatedAt { get; private set; }

        private Relationship()
        {
            // For serializers/ORM tools.
        }

        private Relationship(
            Guid id,
            string tenantId,
            SubjectId subject,
            RelationName relation,
            ObjectId obj,
            RelationshipPermissionEffect effect,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
            : base(id)
        {
            TenantId = NormalizeTenantId(tenantId);
            Subject = subject;
            Relation = relation;
            Object = obj;
            Effect = effect;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        /// <summary>
        /// Creates a relationship from raw tuple values and raises an upsert domain event.
        /// </summary>
        public static Relationship Create(
            string tenantId,
            string subject,
            string relation,
            string obj,
            RelationshipPermissionEffect effect,
            DateTimeOffset? createdAt = null)
        {
            if (!SubjectId.TryCreate(subject, out var subjectId)
                || !RelationName.TryCreate(relation, out var relationName)
                || !ObjectId.TryCreate(obj, out var objectId))
            {
                throw new ArgumentException("Invalid tuple format. Expected subject/object as <type>:<id> and non-empty relation.");
            }

            var now = createdAt ?? DateTimeOffset.UtcNow;
            var relationship = new Relationship(Guid.NewGuid(), tenantId, subjectId, relationName, objectId, effect, now, now);
            relationship.RaiseDomainEvent(new RelationshipUpsertedDomainEvent(
                relationship.TenantId,
                relationship.Subject.Value,
                relationship.Relation.Value,
                relationship.Object.Value,
                relationship.Effect.ToString().ToLowerInvariant(),
                relationship.CreatedAt));

            return relationship;
        }

        /// <summary>
        /// Rebuilds a relationship from persistence state without emitting domain events.
        /// </summary>
        public static Relationship Rehydrate(
            Guid id,
            string tenantId,
            string subject,
            string relation,
            string obj,
            RelationshipPermissionEffect effect,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            if (!SubjectId.TryCreate(subject, out var subjectId)
                || !RelationName.TryCreate(relation, out var relationName)
                || !ObjectId.TryCreate(obj, out var objectId))
            {
                throw new ArgumentException("Invalid tuple format. Expected subject/object as <type>:<id> and non-empty relation.");
            }

            return new Relationship(id, tenantId, subjectId, relationName, objectId, effect, createdAt, updatedAt);
        }

        /// <summary>
        /// Changes the permission effect and refreshes update timestamp.
        /// </summary>
        public void UpdateEffect(RelationshipPermissionEffect effect)
        {
            Effect = effect;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Marks the relationship as deleted by raising a domain event.
        /// </summary>
        public void MarkDeleted()
        {
            RaiseDomainEvent(new RelationshipDeletedDomainEvent(
                TenantId,
                Subject.Value,
                Relation.Value,
                Object.Value,
                DateTimeOffset.UtcNow));
        }

        private static string NormalizeTenantId(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            }

            return tenantId.Trim();
        }
    }
}
