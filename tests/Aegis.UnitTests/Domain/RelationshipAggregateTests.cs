using Aegis.Domain.Entities;
using Aegis.Domain.Enums;
using Aegis.Domain.Events;

namespace Aegis.UnitTests.Domain
{
    public sealed class RelationshipAggregateTests
    {
        // Core tuple creation path with event emission.
        [Fact]
        public void Create_ShouldRaiseUpsertedEvent()
        {
            var relationship = Relationship.Create(
                "store-1",
                "user:charlie",
                "viewer",
                "document:spec",
                RelationshipPermissionEffect.Allow);

            Assert.Equal("store-1", relationship.TenantId);
            Assert.Equal("user:charlie", relationship.Subject.Value);
            Assert.Equal("viewer", relationship.Relation.Value);
            Assert.Equal("document:spec", relationship.Object.Value);
            Assert.Single(relationship.DomainEvents);
            var upserted = Assert.IsType<RelationshipUpsertedDomainEvent>(relationship.DomainEvents[0]);
            Assert.Equal("store-1", upserted.TenantId);
            Assert.Equal("user:charlie", upserted.Subject);
            Assert.Equal("viewer", upserted.Relation);
            Assert.Equal("document:spec", upserted.Object);
            Assert.Equal("allow", upserted.Effect);
        }

        [Fact]
        public void Create_ShouldUseProvidedCreatedAt_AndEmitEventWithSameTimestamp()
        {
            var createdAt = DateTimeOffset.UtcNow.AddHours(-2);

            var relationship = Relationship.Create(
                " store-1 ",
                "user:charlie",
                "viewer",
                "document:spec",
                RelationshipPermissionEffect.Deny,
                createdAt);

            Assert.Equal("store-1", relationship.TenantId);
            Assert.Equal(createdAt, relationship.CreatedAt);
            Assert.Equal(createdAt, relationship.UpdatedAt);

            var upserted = Assert.IsType<RelationshipUpsertedDomainEvent>(relationship.DomainEvents[0]);
            Assert.Equal(createdAt, upserted.OccurredAt);
            Assert.Equal(createdAt.UtcDateTime, upserted.OccurredOn);
            Assert.Equal("deny", upserted.Effect);
        }

        [Fact]
        public void Rehydrate_ShouldRestoreState_AndNotRaiseEvents()
        {
            var id = Guid.NewGuid();
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);
            var updatedAt = DateTimeOffset.UtcNow.AddMinutes(-1);

            var relationship = Relationship.Rehydrate(
                id,
                "store-1",
                "user:charlie",
                "viewer",
                "document:spec",
                RelationshipPermissionEffect.Deny,
                createdAt,
                updatedAt);

            Assert.Equal(id, relationship.Id);
            Assert.Equal("store-1", relationship.TenantId);
            Assert.Equal("user:charlie", relationship.Subject.Value);
            Assert.Equal("viewer", relationship.Relation.Value);
            Assert.Equal("document:spec", relationship.Object.Value);
            Assert.Equal(RelationshipPermissionEffect.Deny, relationship.Effect);
            Assert.Equal(createdAt, relationship.CreatedAt);
            Assert.Equal(updatedAt, relationship.UpdatedAt);
            Assert.Empty(relationship.DomainEvents);
        }

        [Fact]
        public void Rehydrate_ShouldTrimTenantId_AndRestoreState()
        {
            var relationship = Relationship.Rehydrate(
                Guid.NewGuid(),
                " store-1 ",
                "user:charlie",
                "viewer",
                "document:spec",
                RelationshipPermissionEffect.Allow,
                DateTimeOffset.UtcNow.AddMinutes(-10),
                DateTimeOffset.UtcNow.AddMinutes(-5));

            Assert.Equal("store-1", relationship.TenantId);
        }

        [Fact]
        public void UpdateEffect_ShouldChangeEffect_AndRefreshUpdatedAt()
        {
            var relationship = Relationship.Create(
                "store-1",
                "user:charlie",
                "viewer",
                "document:spec",
                RelationshipPermissionEffect.Allow,
                DateTimeOffset.UtcNow.AddMinutes(-10));
            relationship.ClearDomainEvents();
            var before = relationship.UpdatedAt;

            relationship.UpdateEffect(RelationshipPermissionEffect.Deny);

            Assert.Equal(RelationshipPermissionEffect.Deny, relationship.Effect);
            Assert.True(relationship.UpdatedAt >= before);
            Assert.Empty(relationship.DomainEvents);
        }

        [Fact]
        public void UpdateEffect_ShouldAllowSameEffect_WithoutRecreatingEvent()
        {
            var relationship = Relationship.Create(
                "store-1",
                "user:charlie",
                "viewer",
                "document:spec",
                RelationshipPermissionEffect.Allow);
            relationship.ClearDomainEvents();
            var before = relationship.UpdatedAt;

            relationship.UpdateEffect(RelationshipPermissionEffect.Allow);

            Assert.Equal(RelationshipPermissionEffect.Allow, relationship.Effect);
            Assert.True(relationship.UpdatedAt >= before);
            Assert.Empty(relationship.DomainEvents);
        }

        [Fact]
        public void MarkDeleted_ShouldRaiseDeletedEvent()
        {
            var relationship = Relationship.Create(
                "store-1",
                "user:charlie",
                "viewer",
                "document:spec",
                RelationshipPermissionEffect.Allow);
            relationship.ClearDomainEvents();

            relationship.MarkDeleted();

            Assert.Single(relationship.DomainEvents);
            var deleted = Assert.IsType<RelationshipDeletedDomainEvent>(relationship.DomainEvents[0]);
            Assert.Equal("store-1", deleted.TenantId);
            Assert.Equal("user:charlie", deleted.Subject);
            Assert.Equal("viewer", deleted.Relation);
            Assert.Equal("document:spec", deleted.Object);
        }

        [Fact]
        public void Create_ShouldThrow_WhenTupleInvalid()
        {
            Assert.Throws<ArgumentException>(() => Relationship.Create(
                "store-1",
                "invalid-subject",
                "viewer",
                "document:spec",
                RelationshipPermissionEffect.Allow));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Create_ShouldThrow_WhenTenantInvalid(string tenantId)
        {
            Assert.Throws<ArgumentException>(() => Relationship.Create(
                tenantId,
                "user:charlie",
                "viewer",
                "document:spec",
                RelationshipPermissionEffect.Allow));
        }

        [Fact]
        public void Create_ShouldThrow_WhenRelationInvalid()
        {
            Assert.Throws<ArgumentException>(() => Relationship.Create(
                "store-1",
                "user:charlie",
                "1viewer",
                "document:spec",
                RelationshipPermissionEffect.Allow));
        }

        [Fact]
        public void Create_ShouldThrow_WhenObjectInvalid()
        {
            Assert.Throws<ArgumentException>(() => Relationship.Create(
                "store-1",
                "user:charlie",
                "viewer",
                "group:eng#member",
                RelationshipPermissionEffect.Allow));
        }

        [Fact]
        public void Rehydrate_ShouldThrow_WhenTupleInvalid()
        {
            Assert.Throws<ArgumentException>(() => Relationship.Rehydrate(
                Guid.NewGuid(),
                "store-1",
                "user:charlie",
                "viewer",
                "group:eng#member",
                RelationshipPermissionEffect.Allow,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Rehydrate_ShouldThrow_WhenTenantInvalid(string tenantId)
        {
            Assert.Throws<ArgumentException>(() => Relationship.Rehydrate(
                Guid.NewGuid(),
                tenantId,
                "user:charlie",
                "viewer",
                "document:spec",
                RelationshipPermissionEffect.Allow,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));
        }
    }
}
