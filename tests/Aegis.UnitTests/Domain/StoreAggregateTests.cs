using Aegis.Domain.Entities;
using Aegis.Domain.Events;

namespace Aegis.UnitTests.Domain
{
    public sealed class StoreAggregateTests
    {
        // Creation should normalize store name and emit store-created event.
        [Fact]
        public void Create_ShouldNormalizeName_AndRaiseCreatedEvent()
        {
            var store = Store.Create("  Product Store  ");

            Assert.False(string.IsNullOrWhiteSpace(store.Id));
            Assert.Equal("Product Store", store.Name);
            Assert.Single(store.DomainEvents);
            var created = Assert.IsType<StoreCreatedDomainEvent>(store.DomainEvents[0]);
            Assert.Equal(store.Id, created.StoreId);
            Assert.Equal(store.Name, created.StoreName);
            Assert.Equal(store.CreatedAt.UtcDateTime, created.OccurredOn);
        }

        [Fact]
        public void Rehydrate_ShouldNotRaiseDomainEvents()
        {
            var createdAt = DateTimeOffset.UtcNow.AddHours(-1);
            var updatedAt = DateTimeOffset.UtcNow;

            var store = Store.Rehydrate("store-1", "Main Store", createdAt, updatedAt);

            Assert.Equal("store-1", store.Id);
            Assert.Equal("Main Store", store.Name);
            Assert.Equal(createdAt, store.CreatedAt);
            Assert.Equal(updatedAt, store.UpdatedAt);
            Assert.Empty(store.DomainEvents);
        }

        [Fact]
        public void Rename_ShouldUpdateNameAndTimestamp()
        {
            var store = Store.Create("Initial");
            var before = store.UpdatedAt;

            store.Rename("Renamed");

            Assert.Equal("Renamed", store.Name);
            Assert.True(store.UpdatedAt >= before);
        }

        // Deletion is represented as a domain event instead of hard delete in aggregate.
        [Fact]
        public void MarkDeleted_ShouldRaiseDeletedEvent()
        {
            var store = Store.Create("Aegis");
            store.ClearDomainEvents();

            store.MarkDeleted();

            Assert.Single(store.DomainEvents);
            var deleted = Assert.IsType<StoreDeletedDomainEvent>(store.DomainEvents[0]);
            Assert.Equal(store.Id, deleted.StoreId);
            Assert.Equal(store.Name, deleted.StoreName);
        }

        [Fact]
        public void Create_ShouldThrow_WhenNameIsInvalid()
        {
            Assert.Throws<ArgumentException>(() => Store.Create("   "));
        }

        [Fact]
        public void Create_ShouldThrow_WhenNameTooLong()
        {
            var tooLong = new string('a', 257);

            Assert.Throws<ArgumentException>(() => Store.Create(tooLong));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Rehydrate_ShouldThrow_WhenIdInvalid(string id)
        {
            Assert.Throws<ArgumentException>(() => Store.Rehydrate(id, "Main", DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Rename_ShouldThrow_WhenNameInvalid(string name)
        {
            var store = Store.Create("Main");

            Assert.Throws<ArgumentException>(() => store.Rename(name));
        }
    }
}
