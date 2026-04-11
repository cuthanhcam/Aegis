using Aegis.Domain.Events;
using Aegis.SharedKernel;

namespace Aegis.Domain.Entities
{
    /// <summary>
    /// Aggregate root representing a tenant store.
    /// </summary>
    public sealed class Store : AggregateRoot<string>
    {
        public string Name { get; private set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; private init; }

        public DateTimeOffset UpdatedAt { get; private set; }

        private Store()
        {
            // For serializers/ORM tools.
        }

        private Store(string id, string name, DateTimeOffset now)
            : base(id)
        {
            Name = NormalizeName(name);
            CreatedAt = now;
            UpdatedAt = now;
        }

        private Store(string id, string name, DateTimeOffset createdAt, DateTimeOffset updatedAt)
            : base(id)
        {
            Name = NormalizeName(name);
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        /// <summary>
        /// Creates a new store and raises a creation domain event.
        /// </summary>
        public static Store Create(string name)
        {
            var store = new Store(NewUlidLikeId(), name, DateTimeOffset.UtcNow);
            store.RaiseDomainEvent(new StoreCreatedDomainEvent(store.Id, store.Name, store.CreatedAt));
            return store;
        }

        /// <summary>
        /// Rebuilds a store from persistence state without emitting domain events.
        /// </summary>
        public static Store Rehydrate(string id, string name, DateTimeOffset createdAt, DateTimeOffset updatedAt)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Store id is required.", nameof(id));
            }

            return new Store(id.Trim(), name, createdAt, updatedAt);
        }

        /// <summary>
        /// Renames the store and updates the modification timestamp.
        /// </summary>
        public void Rename(string newName)
        {
            Name = NormalizeName(newName);
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Marks the store as deleted by raising a domain event.
        /// </summary>
        public void MarkDeleted()
        {
            RaiseDomainEvent(new StoreDeletedDomainEvent(Id, Name, DateTimeOffset.UtcNow));
        }

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Store name is required.", nameof(name));
            }

            var normalized = name.Trim();
            if (normalized.Length > 256)
            {
                throw new ArgumentException("Store name cannot exceed 256 characters.", nameof(name));
            }

            return normalized;
        }

        private static string NewUlidLikeId()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("+", "A").Replace("/", "B");
        }
    }
}
