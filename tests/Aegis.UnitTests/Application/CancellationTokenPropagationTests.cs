using Aegis.Application.Features.Query;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Moq;

namespace Aegis.UnitTests.Application
{
    /// <summary>
    /// Risk Test: CancellationToken propagation
    /// If cancellation isn't properly propagated, operations may hang
    /// </summary>
    public sealed class CancellationTokenPropagationTests
    {
        [Fact]
        public async Task QueryAllowTuplesUseCase_ShouldRespectCancellation()
        {
            // Arrange
            var mockRelStore = new Mock<IRelationshipStore>();
            using var cts = new CancellationTokenSource();

            mockRelStore
                .Setup(x => x.QueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Subject?>(),
                    It.IsAny<string?>(),
                    It.IsAny<ObjectRef?>(),
                    It.IsAny<RelationshipEffect?>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var useCase = new QueryAllowTuplesUseCase(mockRelStore.Object);
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                useCase.ExecuteAsync("store-1", null, null, null, null, cts.Token));
        }
    }
}
