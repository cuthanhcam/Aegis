using Aegis.Domain.Events;

namespace Aegis.UnitTests.Domain
{
    public sealed class DomainEventPayloadTests
    {
        // Event constructors should keep payload and synchronize DomainEvent.OccurredOn.
        [Fact]
        public void AuthorizationModelEvents_ShouldMapPayloadAndOccurredOn()
        {
            var at = DateTimeOffset.UtcNow;

            var created = new AuthorizationModelCreatedDomainEvent("m1", "s1", "1.1", at);
            var updated = new AuthorizationModelUpdatedDomainEvent("m1", "s1", "1.2", at);
            var deleted = new AuthorizationModelDeletedDomainEvent("m1", "s1", at);

            Assert.Equal("m1", created.AuthorizationModelId);
            Assert.Equal("s1", created.StoreId);
            Assert.Equal("1.1", created.SchemaVersion);
            Assert.Equal(at.UtcDateTime, created.OccurredOn);

            Assert.Equal("m1", updated.AuthorizationModelId);
            Assert.Equal("s1", updated.StoreId);
            Assert.Equal("1.2", updated.SchemaVersion);
            Assert.Equal(at.UtcDateTime, updated.OccurredOn);

            Assert.Equal("m1", deleted.AuthorizationModelId);
            Assert.Equal("s1", deleted.StoreId);
            Assert.Equal(at.UtcDateTime, deleted.OccurredOn);
        }

        [Fact]
        public void StoreEvents_ShouldMapPayloadAndOccurredOn()
        {
            var at = DateTimeOffset.UtcNow;

            var created = new StoreCreatedDomainEvent("store-1", "Main", at);
            var deleted = new StoreDeletedDomainEvent("store-1", "Main", at);

            Assert.Equal("store-1", created.StoreId);
            Assert.Equal("Main", created.StoreName);
            Assert.Equal(at.UtcDateTime, created.OccurredOn);

            Assert.Equal("store-1", deleted.StoreId);
            Assert.Equal("Main", deleted.StoreName);
            Assert.Equal(at.UtcDateTime, deleted.OccurredOn);
        }

        [Fact]
        public void RelationshipEvents_ShouldMapPayloadAndOccurredOn()
        {
            var at = DateTimeOffset.UtcNow;

            var upserted = new RelationshipUpsertedDomainEvent("t1", "user:charlie", "viewer", "document:spec", "allow", at);
            var deleted = new RelationshipDeletedDomainEvent("t1", "user:charlie", "viewer", "document:spec", at);

            Assert.Equal("t1", upserted.TenantId);
            Assert.Equal("user:charlie", upserted.Subject);
            Assert.Equal("viewer", upserted.Relation);
            Assert.Equal("document:spec", upserted.Object);
            Assert.Equal("allow", upserted.Effect);
            Assert.Equal(at, upserted.OccurredAt);
            Assert.Equal(at.UtcDateTime, upserted.OccurredOn);

            Assert.Equal("t1", deleted.TenantId);
            Assert.Equal("user:charlie", deleted.Subject);
            Assert.Equal("viewer", deleted.Relation);
            Assert.Equal("document:spec", deleted.Object);
            Assert.Equal(at, deleted.DeletedAt);
            Assert.Equal(at.UtcDateTime, deleted.OccurredOn);
        }
    }
}
