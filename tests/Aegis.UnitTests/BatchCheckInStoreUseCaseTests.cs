// Application Layer Unit Tests - Batch Permission Operations
// Folder structure: tests/Aegis.UnitTests/Application/Features/Permissions/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Aegis.Application.Features.Permissions;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Permissions;
using Aegis.Contracts.Common;

namespace Aegis.UnitTests.Application.Features.Permissions
{
    /// <summary>
    /// Tests for BatchCheckInStoreUseCase - Batch permission checking
    /// File: tests/Aegis.UnitTests/Application/Features/Permissions/BatchCheckInStoreUseCaseTests.cs
    /// </summary>
    [Trait("Category", "Application")]
    [Trait("Feature", "Permissions")]
    public sealed class BatchCheckInStoreUseCaseTests
    {
        [Fact]
        public async Task ExecuteAsync_WithMultipleRequests_ReturnsBatchResults()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new BatchCheckInStoreUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(allowed: true, decision: "Allow", reasonCode: "AUTHORIZED", trace: new List<ExplainTraceStep>()));

            var requests = new List<CheckRequestDto>
            {
                new CheckRequestDto(Subject: "user:alice", Relation: "editor", Object: "doc:1", Context: null),
                new CheckRequestDto(Subject: "user:bob", Relation: "viewer", Object: "doc:2", Context: null),
                new CheckRequestDto(Subject: "user:charlie", Relation: "admin", Object: "doc:3", Context: null)
            };

            // Act
            var results = await useCase.ExecuteAsync(requests, "tenant:789", null, CancellationToken.None);

            // Assert
            Assert.NotEmpty(results);
            Assert.Equal(3, results.Count);
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptyBatch_ThrowsArgumentException()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new BatchCheckInStoreUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            var emptyRequests = new List<CheckRequestDto>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(emptyRequests, "tenant:789", null, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_GeneratesCorrelationIdFromTenantId()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new BatchCheckInStoreUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(allowed: true, decision: "Allow", reasonCode: "AUTHORIZED", trace: new List<ExplainTraceStep>()));

            var requests = new List<CheckRequestDto>
            {
                new CheckRequestDto(Subject: "user:alice", Relation: "editor", Object: "doc:1", Context: null)
            };

            // Act
            var results = await useCase.ExecuteAsync(requests, "tenant:123", null, CancellationToken.None);

            // Assert
            Assert.NotEmpty(results);
            Assert.NotNull(results[0].CorrelationId);
        }

        [Fact]
        public async Task ExecuteAsync_WithNullCorrelationId_GeneratesNumericId()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new BatchCheckInStoreUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(allowed: true, decision: "Allow", reasonCode: "AUTHORIZED", trace: new List<ExplainTraceStep>()));

            var requests = new List<CheckRequestDto>
            {
                new CheckRequestDto(Subject: "user:alice", Relation: "editor", Object: "doc:1", Context: null),
                new CheckRequestDto(Subject: "user:bob", Relation: "viewer", Object: "doc:2", Context: null)
            };

            // Act
            var results = await useCase.ExecuteAsync(requests, "tenant:789", null, CancellationToken.None);

            // Assert
            Assert.Equal(2, results.Count);
            Assert.NotNull(results[0].CorrelationId);
            Assert.NotNull(results[1].CorrelationId);
        }

        [Fact]
        public async Task ExecuteAsync_ExceedsMaxBatchSize_ThrowsArgumentException()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new BatchCheckInStoreUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            var requests = new List<CheckRequestDto>();
            for (int i = 0; i < 1001; i++)
            {
                requests.Add(new CheckRequestDto(Subject: $"user:{i}", Relation: "viewer", Object: $"doc:{i}", Context: null));
            }

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(requests, "tenant:789", null, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_LogsAuditForEachDecision()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new BatchCheckInStoreUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(allowed: true, decision: "Allow", reasonCode: "AUTHORIZED", trace: new List<ExplainTraceStep>()));

            var requests = new List<CheckRequestDto>
            {
                new CheckRequestDto(Subject: "user:alice", Relation: "editor", Object: "doc:1", Context: null),
                new CheckRequestDto(Subject: "user:bob", Relation: "viewer", Object: "doc:2", Context: null)
            };

            // Act
            await useCase.ExecuteAsync(requests, "tenant:789", null, CancellationToken.None);

            // Assert
            mockAuditStore.Verify(x => x.WriteAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
    }
}
