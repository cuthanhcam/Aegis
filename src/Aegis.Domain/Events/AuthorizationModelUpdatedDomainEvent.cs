using Aegis.SharedKernel;

namespace Aegis.Domain.Events
{
    /// <summary>
    /// Raised when an authorization model definition is updated.
    /// </summary>
    public sealed class AuthorizationModelUpdatedDomainEvent : DomainEvent
    {
        /// <summary>
        /// Updated authorization model identifier.
        /// </summary>
        public string AuthorizationModelId { get; }

        /// <summary>
        /// Store identifier that owns the model.
        /// </summary>
        public string StoreId { get; }

        /// <summary>
        /// New schema version after update.
        /// </summary>
        public string SchemaVersion { get; }

        /// <summary>
        /// Domain timestamp when the update occurred.
        /// </summary>
        public DateTimeOffset UpdatedAt { get; }

        public AuthorizationModelUpdatedDomainEvent(
            string authorizationModelId,
            string storeId,
            string schemaVersion,
            DateTimeOffset updatedAt)
        {
            AuthorizationModelId = authorizationModelId;
            StoreId = storeId;
            SchemaVersion = schemaVersion;
            UpdatedAt = updatedAt;
            OccurredOn = updatedAt.UtcDateTime;
        }
    }
}
