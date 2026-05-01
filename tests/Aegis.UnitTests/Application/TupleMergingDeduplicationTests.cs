using Aegis.Application.Features.Query;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Moq;

namespace Aegis.UnitTests.Application
{
    /// <summary>
    /// Risk Test: Query tuple merging with deduplication
    /// Complex logic prone to bugs, especially with case sensitivity
    /// </summary>
    public sealed class TupleMergingDeduplicationTests
    {
        private readonly Mock<IRelationshipStore> _mockRelStore;
        private readonly QueryAllowTuplesUseCase _useCase;

        public TupleMergingDeduplicationTests()
        {
            _mockRelStore = new Mock<IRelationshipStore>();
            _useCase = new QueryAllowTuplesUseCase(_mockRelStore.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldDeduplicateTuplesAcrossPersistedAndContextual()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var persistedTuples = new List<RelationshipTuple>
            {
                new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Allow, now),
                new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Allow, now), // Duplicate
            };

            var contextualTuples = new List<RelationshipTuple>
            {
                new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Allow, now),
            };

            _mockRelStore
                .Setup(x => x.QueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Subject>(),
                    It.IsAny<string>(),
                    It.IsAny<ObjectRef>(),
                    RelationshipEffect.Allow,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(persistedTuples);

            // Act
            var result = await _useCase.ExecuteAsync(
                "store-1",
                subject: null,
                relation: null,
                @object: null,
                contextualTuples: contextualTuples,
                CancellationToken.None);

            // Assert - only one tuple should remain
            Assert.Single(result);
        }

        [Fact]
        public async Task ExecuteAsync_DenyTuplesShouldTakePrecedence()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var persistedTuples = new List<RelationshipTuple>
            {
                new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Allow, now),
            };

            var contextualTuples = new List<RelationshipTuple>
            {
                new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Deny, now),
            };

            _mockRelStore
                .Setup(x => x.QueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Subject>(),
                    It.IsAny<string>(),
                    It.IsAny<ObjectRef>(),
                    RelationshipEffect.Allow,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(persistedTuples);

            // Act
            var result = await _useCase.ExecuteAsync(
                "store-1",
                subject: null,
                relation: null,
                @object: null,
                contextualTuples: contextualTuples,
                CancellationToken.None);

            // Assert - deny tuple should prevent allow
            Assert.Empty(result);
        }

        [Fact]
        public async Task ExecuteAsync_WithMultipleTuples_ShouldFilterCorrectly()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var persistedTuples = new List<RelationshipTuple>
            {
                new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Allow, now),
                new(new Subject("user:bob"), "viewer", new ObjectRef("doc:1"), RelationshipEffect.Allow, now),
                new(new Subject("user:charlie"), "admin", new ObjectRef("doc:1"), RelationshipEffect.Allow, now),
            };

            var contextualTuples = new List<RelationshipTuple>
            {
                new(new Subject("user:bob"), "viewer", new ObjectRef("doc:1"), RelationshipEffect.Deny, now),
            };

            _mockRelStore
                .Setup(x => x.QueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Subject>(),
                    It.IsAny<string>(),
                    It.IsAny<ObjectRef>(),
                    RelationshipEffect.Allow,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(persistedTuples);

            // Act
            var result = await _useCase.ExecuteAsync(
                "store-1",
                subject: null,
                relation: null,
                @object: null,
                contextualTuples: contextualTuples,
                CancellationToken.None);

            // Assert - bob's tuple should be removed, others remain
            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, t => t.Subject.Value == "user:bob");
        }
    }
}
