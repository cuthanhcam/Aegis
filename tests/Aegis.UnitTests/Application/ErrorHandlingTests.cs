using Aegis.Application.Features.Permissions;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Permissions;
using Moq;

namespace Aegis.UnitTests.Application
{
    /// <summary>
    /// Risk Test: Error propagation and exception handling
    /// Different use cases handle errors differently - should be consistent
    /// </summary>
    public sealed class ErrorHandlingTests
    {
        [Fact]
        public async Task CheckPermissionUseCase_ShouldPropagateValidationErrors()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            var invalidRequest = new CheckRequestDto(
                Subject: "", // Invalid
                Relation: "editor",
                Object: "doc:1",
                ContextualTuples: null,
                Consistency: null,
                AuthorizationModelId: null,
                Context: null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                useCase.ExecuteAsync("store-1", invalidRequest, false));

            // Verify auth engine was never called for invalid input
            mockAuthEngine.Verify(
                x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CheckPermissionUseCase_ShouldPropagateAuthEngineFaults()
        {
            // Arrange
            var mockAuthEngine = new Mock<IAuthorizationEngine>();
            var mockAuditStore = new Mock<IAuditStore>();
            var useCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);

            mockAuthEngine
                .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Auth engine error"));

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

            Assert.Equal("Auth engine error", ex.Message);
        }
    }
}
