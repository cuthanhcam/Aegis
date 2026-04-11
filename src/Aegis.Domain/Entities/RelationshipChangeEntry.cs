using Aegis.SharedKernel;

namespace Aegis.Domain.Entities
{
    /// <summary>
    /// Immutable audit record for relationship changes.
    /// </summary>
    public sealed class RelationshipChangeEntry : Entity<Guid>
    {
        public string TenantId { get; private init; } = string.Empty;

        public string Subject { get; private init; } = string.Empty;

        public string Relation { get; private init; } = string.Empty;

        public string Object { get; private init; } = string.Empty;

        public string Operation { get; private init; } = string.Empty;

        public DateTimeOffset CreatedAt { get; private init; }

        private RelationshipChangeEntry()
        {
            // For serializers/ORM tools.
        }

        private RelationshipChangeEntry(
            Guid id,
            string tenantId,
            string subject,
            string relation,
            string obj,
            string operation,
            DateTimeOffset createdAt)
            : base(id)
        {
            TenantId = NormalizeRequired(tenantId, nameof(tenantId));
            Subject = NormalizeRequired(subject, nameof(subject));
            Relation = NormalizeRequired(relation, nameof(relation));
            Object = NormalizeRequired(obj, nameof(obj));
            Operation = NormalizeRequired(operation, nameof(operation));
            CreatedAt = createdAt;
        }

        /// <summary>
        /// Rebuilds an audit entry from persistence state.
        /// </summary>
        public static RelationshipChangeEntry Rehydrate(
            Guid id,
            string tenantId,
            string subject,
            string relation,
            string obj,
            string operation,
            DateTimeOffset createdAt)
        {
            return new RelationshipChangeEntry(id, tenantId, subject, relation, obj, operation, createdAt);
        }

        private static string NormalizeRequired(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{paramName} is required.", paramName);
            }

            return value.Trim();
        }
    }
}
