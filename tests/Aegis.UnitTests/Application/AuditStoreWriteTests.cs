using Aegis.Application.Features.Permissions;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Permissions;
using Moq;

namespace Aegis.UnitTests.Application
{
    /// <summary>
    /// Risk Test: Audit store write side effects
    /// If audit write fails, should it fail the whole request or just log?
    /// Current behavior: propagates exception to caller
    /// Risk: Could mask real authorization decision
    /// </summary>
    public sealed class AuditStoreWriteTests
    {
        [Fact]
        public async Task CheckPermissionUseCase_ShouldPropagateAuditStoreFailures()
        {
            // Arrange - this is the current behavior which could be risky
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<TraceStep>()));

            mockAuditStore
                .Setup(x => x.WriteAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Audit store is unavailable"));

            var validRequest = new CheckRequestDto(
                Subject: "user:alice",
                Relation: "editor",
                Object: "doc:1",
                ContextualTuples: null,
                Consistency: null,
                AuthorizationModelId: null,
                Context: null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                useCase.ExecuteAsync("store-1", validRequest, false));

            // Current behavior: audit failure propagates to caller
            // RISK: Client doesn't know if authorization failed or audit failed
            // RECOMMENDATION: Make audit non-blocking or use circuit breaker pattern
            Assert.Equal("Audit store is unavailable", ex.Message);
        }
    }
}
