using Aegis.Domain.Entities;
using Aegis.Domain.Events;

namespace Aegis.UnitTests.Domain
{
    public sealed class AuthorizationModelAggregateTests
    {
        // Happy path: creation normalizes inputs and emits the expected domain event.
        [Fact]
        public void Create_ShouldNormalizeInput_AndRaiseCreatedEvent()
        {
            var model = AuthorizationModel.Create(" store-1 ", " 1.1 ", " type document\n  relations\n    define viewer: [user] ");

            Assert.False(string.IsNullOrWhiteSpace(model.Id));
            Assert.Equal("store-1", model.StoreId);
            Assert.Equal("1.1", model.SchemaVersion);
            Assert.Equal("type document\n  relations\n    define viewer: [user]", model.Model);
            Assert.Single(model.DomainEvents);
            var created = Assert.IsType<AuthorizationModelCreatedDomainEvent>(model.DomainEvents[0]);
            Assert.Equal(model.Id, created.AuthorizationModelId);
            Assert.Equal(model.StoreId, created.StoreId);
            Assert.Equal(model.SchemaVersion, created.SchemaVersion);
            Assert.Equal(model.CreatedAt.UtcDateTime, created.OccurredOn);
        }

        // Rehydrate should reconstruct state without side effects.
        [Fact]
        public void Rehydrate_ShouldNotRaiseDomainEvents()
        {
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);

            var model = AuthorizationModel.Rehydrate("model-1", "store-1", "1.1", "type doc", createdAt);

            Assert.Equal("model-1", model.Id);
            Assert.Equal("store-1", model.StoreId);
            Assert.Equal("1.1", model.SchemaVersion);
            Assert.Equal("type doc", model.Model);
            Assert.Equal(createdAt, model.CreatedAt);
            Assert.Empty(model.DomainEvents);
        }

        // Update should mutate state and emit an update event with new schema version.
        [Fact]
        public void UpdateDefinition_ShouldChangeData_AndRaiseUpdatedEvent()
        {
            var model = AuthorizationModel.Create("store-1", "1.1", "type doc");
            model.ClearDomainEvents();

            model.UpdateDefinition("1.2", "type doc2");

            Assert.Equal("1.2", model.SchemaVersion);
            Assert.Equal("type doc2", model.Model);
            Assert.Single(model.DomainEvents);
            var updated = Assert.IsType<AuthorizationModelUpdatedDomainEvent>(model.DomainEvents[0]);
            Assert.Equal(model.Id, updated.AuthorizationModelId);
            Assert.Equal(model.StoreId, updated.StoreId);
            Assert.Equal(model.SchemaVersion, updated.SchemaVersion);
        }

        [Fact]
        public void MarkDeleted_ShouldRaiseDeletedEvent()
        {
            var model = AuthorizationModel.Create("store-1", "1.1", "type doc");
            model.ClearDomainEvents();

            model.MarkDeleted();

            Assert.Single(model.DomainEvents);
            var deleted = Assert.IsType<AuthorizationModelDeletedDomainEvent>(model.DomainEvents[0]);
            Assert.Equal(model.Id, deleted.AuthorizationModelId);
            Assert.Equal(model.StoreId, deleted.StoreId);
        }

        [Fact]
        public void Create_ShouldThrow_WhenSchemaVersionTooLong()
        {
            var schemaVersion = new string('a', 33);

            Assert.Throws<ArgumentException>(() => AuthorizationModel.Create("store-1", schemaVersion, "type doc"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Create_ShouldThrow_WhenStoreIdInvalid(string storeId)
        {
            Assert.Throws<ArgumentException>(() => AuthorizationModel.Create(storeId, "1.1", "type doc"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Create_ShouldThrow_WhenSchemaVersionInvalid(string schemaVersion)
        {
            Assert.Throws<ArgumentException>(() => AuthorizationModel.Create("store-1", schemaVersion, "type doc"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Create_ShouldThrow_WhenModelInvalid(string modelText)
        {
            Assert.Throws<ArgumentException>(() => AuthorizationModel.Create("store-1", "1.1", modelText));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Rehydrate_ShouldThrow_WhenIdInvalid(string id)
        {
            Assert.Throws<ArgumentException>(() => AuthorizationModel.Rehydrate(id, "store-1", "1.1", "type doc", DateTimeOffset.UtcNow));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void UpdateDefinition_ShouldThrow_WhenSchemaVersionInvalid(string schemaVersion)
        {
            var model = AuthorizationModel.Create("store-1", "1.1", "type doc");

            Assert.Throws<ArgumentException>(() => model.UpdateDefinition(schemaVersion, "type doc2"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void UpdateDefinition_ShouldThrow_WhenModelInvalid(string modelText)
        {
            var model = AuthorizationModel.Create("store-1", "1.1", "type doc");

            Assert.Throws<ArgumentException>(() => model.UpdateDefinition("1.2", modelText));
        }
    }
}
