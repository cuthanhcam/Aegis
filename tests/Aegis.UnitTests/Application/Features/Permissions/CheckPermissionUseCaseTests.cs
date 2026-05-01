using Aegis.Application.Features.Permissions;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Permissions;
using Moq;

namespace Aegis.UnitTests.Application.Features.Permissions
{
    /// <summary>
    /// Tests for CheckPermissionUseCase - Primary authorization workflow
    /// </summary>
    [Trait("Category", "ApplicationTests")]
    [Trait("Feature", "Permissions")]
    public class CheckPermissionUseCaseTests
    {
        [Fact]
        public async Task ExecuteAsync_WithValidAllowRequest_ReturnsTrue()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<TraceStep>()));

            var request = new CheckRequestDto(Subject: "user:alice", Relation: "editor", Object: "doc:1", ContextualTuples: null, Consistency: null, AuthorizationModelId: null, Context: null);

            // Act
            var result = await useCase.ExecuteAsync("store-1", request, includeTrace: false, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Allowed);
            mockAuthEngine.Verify(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidDenyRequest_ReturnsFalse()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(false, "Deny", "UNAUTHORIZED", new List<TraceStep>()));

            var request = new CheckRequestDto(Subject: "user:bob", Relation: "viewer", Object: "doc:1", ContextualTuples: null, Consistency: null, AuthorizationModelId: null, Context: null);

            // Act
            var result = await useCase.ExecuteAsync("store-1", request, includeTrace: false, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Allowed);
        }

        [Fact]
        public async Task ExecuteAsync_LogsAuditEventWhenDecisionMade()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<TraceStep>()));

            var request = new CheckRequestDto(Subject: "user:alice", Relation: "editor", Object: "doc:1", ContextualTuples: null, Consistency: null, AuthorizationModelId: null, Context: null);

            // Act
            await useCase.ExecuteAsync("store-1", request, includeTrace: false, CancellationToken.None);

            // Assert
            mockAuditStore.Verify(x => x.WriteAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithCancelledToken_ThrowsOperationCanceledException()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var request = new CheckRequestDto(Subject: "user:alice", Relation: "editor", Object: "doc:1", ContextualTuples: null, Consistency: null, AuthorizationModelId: null, Context: null);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => useCase.ExecuteAsync("store-1", request, includeTrace: false, cts.Token));
        }

        [Fact]
        public async Task ExecuteAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => useCase.ExecuteAsync("store-1", null!, includeTrace: false, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_IncludesTraceWhenAvailable()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            var traceSteps = new List<TraceStep>
            {
                new("1", "Allow", "Check direct relationship")
            };

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", traceSteps));

            var request = new CheckRequestDto(Subject: "user:alice", Relation: "editor", Object: "doc:1", ContextualTuples: null, Consistency: null, AuthorizationModelId: null, Context: null);

            // Act
            var result = await useCase.ExecuteAsync("store-1", request, includeTrace: true, CancellationToken.None);

            // Assert
            Assert.NotNull(result.Trace);
            Assert.NotEmpty(result.Trace);
        }
    }
}
