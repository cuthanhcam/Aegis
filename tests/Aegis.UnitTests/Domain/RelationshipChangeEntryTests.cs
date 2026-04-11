using Aegis.Domain.Entities;

namespace Aegis.UnitTests.Domain
{
    public sealed class RelationshipChangeEntryTests
    {
        // Rehydrate path should normalize textual fields and preserve identity/timestamp.
        [Fact]
        public void Rehydrate_ShouldNormalizeFields_AndPreserveIdentity()
        {
            var id = Guid.NewGuid();
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-2);

            var entry = RelationshipChangeEntry.Rehydrate(
                id,
                " store-1 ",
                " user:charlie ",
                " viewer ",
                " document:spec ",
                " write ",
                createdAt);

            Assert.Equal(id, entry.Id);
            Assert.Equal("store-1", entry.TenantId);
            Assert.Equal("user:charlie", entry.Subject);
            Assert.Equal("viewer", entry.Relation);
            Assert.Equal("document:spec", entry.Object);
            Assert.Equal("write", entry.Operation);
            Assert.Equal(createdAt, entry.CreatedAt);
        }

        [Theory]
        [InlineData("", "user:charlie", "viewer", "document:spec", "write")]
        [InlineData("store-1", "", "viewer", "document:spec", "write")]
        [InlineData("store-1", "user:charlie", "", "document:spec", "write")]
        [InlineData("store-1", "user:charlie", "viewer", "", "write")]
        [InlineData("store-1", "user:charlie", "viewer", "document:spec", "")]
        [InlineData(" ", "user:charlie", "viewer", "document:spec", "write")]
        [InlineData("store-1", " ", "viewer", "document:spec", "write")]
        [InlineData("store-1", "user:charlie", " ", "document:spec", "write")]
        [InlineData("store-1", "user:charlie", "viewer", " ", "write")]
        [InlineData("store-1", "user:charlie", "viewer", "document:spec", " ")]
        public void Rehydrate_ShouldThrow_WhenAnyRequiredFieldInvalid(
            string tenantId,
            string subject,
            string relation,
            string obj,
            string operation)
        {
            Assert.Throws<ArgumentException>(() => RelationshipChangeEntry.Rehydrate(
                Guid.NewGuid(),
                tenantId,
                subject,
                relation,
                obj,
                operation,
                DateTimeOffset.UtcNow));
        }
    }
}
