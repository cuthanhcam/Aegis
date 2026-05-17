using Aegis.Application.Features.Permissions;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Permissions;
using Moq;

namespace Aegis.UnitTests.Application
{
    /// <summary>
    /// Risk Test: Batch operation failure modes
    /// Batch should handle individual item failures gracefully
    /// </summary>
    public sealed class BatchProcessingEdgeCasesTests
    {
        [Fact]
        public async Task BatchCheckInStoreUseCase_WithMixedValidInvalid_ShouldStopOnFirstInvalid()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var batchUseCase = BuildBatchUseCase(mockAuthEngine, mockAuditStore);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<TraceStep>()));

            var request = new BatchCheckRequestDto(new List<BatchCheckItemDto>
            {
                new("user:alice", "editor", "doc:1", "1", null, null, null, null),
                new("", "editor", "doc:2", "2", null, null, null, null), // Invalid
                new("user:charlie", "viewer", "doc:3", "3", null, null, null, null),
            });

            // Act & Assert - should throw on second item
            await Assert.ThrowsAsync<ArgumentException>(() =>
                batchUseCase.ExecuteAsync("store-1", request));

            // Only first check should execute
            mockAuthEngine.Verify(
                x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task BatchCheckInStoreUseCase_ShouldGenerateNumericCorrelationIdsWhenMissing()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var batchUseCase = BuildBatchUseCase(mockAuthEngine, mockAuditStore);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<TraceStep>()));

            var request = new BatchCheckRequestDto(new List<BatchCheckItemDto>
            {
                new("user:alice", "editor", "doc:1", null, null, null, null, null), // No correlation ID
                new("user:bob", "viewer", "doc:2", "custom-id", null, null, null, null),
            });

            // Act
            var result = await batchUseCase.ExecuteAsync("store-1", request);

            // Assert
            Assert.Equal("1", result.Results[0].CorrelationId);
            Assert.Equal("custom-id", result.Results[1].CorrelationId);
        }

        private static BatchCheckInStoreUseCase BuildBatchUseCase(
            Mock<IAuthorizationEngine> mockAuthEngine,
            Mock<IAuditStore> mockAuditStore)
        {
            var mockStoreRegistry = new Mock<IStoreRegistry>();
            var mockModelRegistry = new Mock<IAuthorizationModelRegistry>();

            mockStoreRegistry
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string storeId, CancellationToken _) =>
                    new StoreDto(storeId, "Test Store", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

            var checkUseCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);
            var resolveUseCase = new ResolveAuthorizationModelUseCase(mockStoreRegistry.Object, mockModelRegistry.Object);
            return new BatchCheckInStoreUseCase(checkUseCase, resolveUseCase);
        }
    }
}
