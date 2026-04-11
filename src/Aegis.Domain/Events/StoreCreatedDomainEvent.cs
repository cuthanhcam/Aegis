using Aegis.SharedKernel;

namespace Aegis.Domain.Events
{
    /// <summary>
    /// Raised when a store is created.
    /// </summary>
    public sealed class StoreCreatedDomainEvent : DomainEvent
    {
        /// <summary>
        /// Created store identifier.
        /// </summary>
        public string StoreId { get; }

        /// <summary>
        /// Store name at creation time.
        /// </summary>
        public string StoreName { get; }

        /// <summary>
        /// Domain timestamp when the store was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; }

        public StoreCreatedDomainEvent(string storeId, string storeName, DateTimeOffset createdAt)
        {
            StoreId = storeId;
            StoreName = storeName;
            CreatedAt = createdAt;
            OccurredOn = createdAt.UtcDateTime;
        }
    }
}
