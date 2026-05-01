// Application Layer Unit Tests - Organized by namespace structure
// This file consolidates tests organized by namespace hierarchy
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
    /// Tests for CheckPermissionUseCase - Primary authorization workflow
    /// File: tests/Aegis.UnitTests/Application/Features/Permissions/CheckPermissionUseCaseTests.cs
    /// </summary>
    [Trait("Category", "Application")]
    [Trait("Feature", "Permissions")]
    public sealed class CheckPermissionUseCaseTests
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
                .ReturnsAsync(new DecisionResult(allowed: true, decision: "Allow", reasonCode: "AUTHORIZED", trace: new List<ExplainTraceStep>()));

            var request = new CheckRequestDto(Subject: "user:alice", Relation: "editor", Object: "doc:1", Context: null);

            // Act
            var result = await useCase.ExecuteAsync(request, CancellationToken.None);

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
                .ReturnsAsync(new DecisionResult(allowed: false, decision: "Deny", reasonCode: "UNAUTHORIZED", trace: new List<ExplainTraceStep>()));

            var request = new CheckRequestDto(Subject: "user:bob", Relation: "viewer", Object: "doc:1", Context: null);

            // Act
            var result = await useCase.ExecuteAsync(request, CancellationToken.None);

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
                .ReturnsAsync(new DecisionResult(allowed: true, decision: "Allow", reasonCode: "AUTHORIZED", trace: new List<ExplainTraceStep>()));

            var request = new CheckRequestDto(Subject: "user:alice", Relation: "editor", Object: "doc:1", Context: null);

            // Act
            await useCase.ExecuteAsync(request, CancellationToken.None);

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

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var request = new CheckRequestDto(Subject: "user:alice", Relation: "editor", Object: "doc:1", Context: null);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => useCase.ExecuteAsync(request, cts.Token));
        }

        [Fact]
        public async Task ExecuteAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => useCase.ExecuteAsync(null!, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_IncludesTraceWhenAvailable()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            var traceSteps = new List<ExplainTraceStep>
            {
                new ExplainTraceStep { Step = 1, Description = "Check direct relationship" }
            };

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(allowed: true, decision: "Allow", reasonCode: "AUTHORIZED", trace: traceSteps));

            var request = new CheckRequestDto(Subject: "user:alice", Relation: "editor", Object: "doc:1", Context: null);

            // Act
            var result = await useCase.ExecuteAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result.Trace);
            Assert.NotEmpty(result.Trace);
        }
    }
}
