using Aegis.Application.Features.Permissions;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Compatibility;
using Aegis.Contracts.Permissions;
using Moq;

namespace Aegis.UnitTests.Application.Features.Permissions
{
    /// <summary>
    /// Tests for BatchCheckInStoreUseCase - Batch permission checking
    /// </summary>
    [Trait("Category", "ApplicationTests")]
    [Trait("Feature", "Permissions")]
    public class BatchCheckInStoreUseCaseTests
    {
        [Fact]
        public async Task ExecuteAsync_WithMultipleRequests_ReturnsBatchResults()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = BuildUseCase(mockAuthEngine, mockAuditStore);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<TraceStep>()));

            var requests = new List<BatchCheckItemDto>
            {
                new("user:alice", "editor", "doc:1", null, null, null, null, null),
                new("user:bob", "viewer", "doc:2", null, null, null, null, null),
                new("user:charlie", "admin", "doc:3", null, null, null, null, null)
            };

            var result = await useCase.ExecuteAsync("store-1", new BatchCheckRequestDto(requests));

            // Assert
            Assert.NotEmpty(result.Results);
            Assert.Equal(3, result.Results.Count);
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptyBatch_ThrowsArgumentException()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = BuildUseCase(mockAuthEngine, mockAuditStore);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync("store-1", new BatchCheckRequestDto([])));
        }

        [Fact]
        public async Task ExecuteAsync_GeneratesCorrelationIdFromTenantId()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = BuildUseCase(mockAuthEngine, mockAuditStore);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<TraceStep>()));

            var request = new BatchCheckRequestDto(new List<BatchCheckItemDto>
            {
                new("user:alice", "editor", "doc:1", null, null, null, null, null)
            });

            // Act
            var result = await useCase.ExecuteAsync("store-1", request);

            // Assert
            Assert.NotEmpty(result.Results);
            Assert.NotNull(result.Results[0].CorrelationId);
        }

        [Fact]
        public async Task ExecuteAsync_WithNullCorrelationId_GeneratesNumericId()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = BuildUseCase(mockAuthEngine, mockAuditStore);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<TraceStep>()));

            var request = new BatchCheckRequestDto(new List<BatchCheckItemDto>
            {
                new("user:alice", "editor", "doc:1", null, null, null, null, null),
                new("user:bob", "viewer", "doc:2", "my-correlation-id", null, null, null, null)
            });

            // Act
            var result = await useCase.ExecuteAsync("store-1", request);

            // Assert
            Assert.Equal(2, result.Results.Count);
            Assert.NotNull(result.Results[0].CorrelationId);
            Assert.Equal("my-correlation-id", result.Results[1].CorrelationId);
        }

        [Fact]
        public async Task ExecuteAsync_ExceedsMaxBatchSize_ThrowsArgumentException()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = BuildUseCase(mockAuthEngine, mockAuditStore);

            var items = Enumerable.Range(0, 1001)
                .Select(i => new BatchCheckItemDto($"user:{i}", "viewer", $"doc:{i}", null, null, null, null, null))
                .ToList();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync("store-1", new BatchCheckRequestDto(items)));
        }

        [Fact]
        public async Task ExecuteAsync_LogsAuditForEachDecision()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = BuildUseCase(mockAuthEngine, mockAuditStore);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<TraceStep>()));

            var requests = new List<BatchCheckItemDto>
            {
                new("user:alice", "editor", "doc:1", null, null, null, null, null),
                new("user:bob", "viewer", "doc:2", null, null, null, null, null)
            };

            // Act
            await useCase.ExecuteAsync("store-1", new BatchCheckRequestDto(requests));

            // Assert
            mockAuditStore.Verify(x => x.WriteAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ExecuteAsync_WithUnknownAuthorizationModel_ThrowsCompatibilityApiException()
        {
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var mockStoreRegistry = new Mock<IStoreRegistry>();
            var mockModelRegistry = new Mock<IAuthorizationModelRegistry>();

            mockStoreRegistry
                .Setup(x => x.GetAsync("store-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StoreDto("store-1", "Store 1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

            mockModelRegistry
                .Setup(x => x.GetByIdAsync("store-1", "missing-model", It.IsAny<CancellationToken>()))
                .ReturnsAsync((AuthorizationModelDto?)null);

            var checkUseCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);
            var resolveUseCase = new ResolveAuthorizationModelUseCase(mockStoreRegistry.Object, mockModelRegistry.Object);
            var useCase = new BatchCheckInStoreUseCase(checkUseCase, resolveUseCase);

            var request = new BatchCheckRequestDto(
            [
                new BatchCheckItemDto("user:alice", "viewer", "doc:1", AuthorizationModelId: "missing-model")
            ]);

            await Assert.ThrowsAsync<CompatibilityApiException>(() =>
                useCase.ExecuteAsync("store-1", request, CancellationToken.None));
        }

        private static BatchCheckInStoreUseCase BuildUseCase(
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
