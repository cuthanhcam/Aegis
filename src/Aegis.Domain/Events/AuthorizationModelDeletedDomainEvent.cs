using Aegis.SharedKernel;

namespace Aegis.Domain.Events
{
    /// <summary>
    /// Raised when an authorization model is deleted.
    /// </summary>
    public sealed class AuthorizationModelDeletedDomainEvent : DomainEvent
    {
        /// <summary>
        /// Deleted authorization model identifier.
        /// </summary>
        public string AuthorizationModelId { get; }

        /// <summary>
        /// Store identifier that owned the model.
        /// </summary>
        public string StoreId { get; }

        /// <summary>
        /// Domain timestamp when deletion occurred.
        /// </summary>
        public DateTimeOffset DeletedAt { get; }

        public AuthorizationModelDeletedDomainEvent(string authorizationModelId, string storeId, DateTimeOffset deletedAt)
        {
            AuthorizationModelId = authorizationModelId;
            StoreId = storeId;
            DeletedAt = deletedAt;
            OccurredOn = deletedAt.UtcDateTime;
        }
    }
}
