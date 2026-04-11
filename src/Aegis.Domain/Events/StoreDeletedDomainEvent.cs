using Aegis.SharedKernel;

namespace Aegis.Domain.Events
{
    /// <summary>
    /// Raised when a store is deleted.
    /// </summary>
    public sealed class StoreDeletedDomainEvent : DomainEvent
    {
        /// <summary>
        /// Deleted store identifier.
        /// </summary>
        public string StoreId { get; }

        /// <summary>
        /// Store name at deletion time.
        /// </summary>
        public string StoreName { get; }

        /// <summary>
        /// Domain timestamp when deletion occurred.
        /// </summary>
        public DateTimeOffset DeletedAt { get; }

        public StoreDeletedDomainEvent(string storeId, string storeName, DateTimeOffset deletedAt)
        {
            StoreId = storeId;
            StoreName = storeName;
            DeletedAt = deletedAt;
            OccurredOn = deletedAt.UtcDateTime;
        }
    }
}
