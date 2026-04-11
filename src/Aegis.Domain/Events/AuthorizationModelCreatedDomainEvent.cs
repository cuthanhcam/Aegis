using Aegis.SharedKernel;

namespace Aegis.Domain.Events
{
    /// <summary>
    /// Raised when a new authorization model is created for a store.
    /// </summary>
    public sealed class AuthorizationModelCreatedDomainEvent : DomainEvent
    {
        /// <summary>
        /// Created authorization model identifier.
        /// </summary>
        public string AuthorizationModelId { get; }

        /// <summary>
        /// Store identifier that owns the authorization model.
        /// </summary>
        public string StoreId { get; }

        /// <summary>
        /// Schema version of the created model.
        /// </summary>
        public string SchemaVersion { get; }

        /// <summary>
        /// Domain timestamp when the model was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; }

        public AuthorizationModelCreatedDomainEvent(
            string authorizationModelId,
            string storeId,
            string schemaVersion,
            DateTimeOffset createdAt)
        {
            AuthorizationModelId = authorizationModelId;
            StoreId = storeId;
            SchemaVersion = schemaVersion;
            CreatedAt = createdAt;
            OccurredOn = createdAt.UtcDateTime;
        }
    }
}
