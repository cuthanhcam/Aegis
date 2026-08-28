using Aegis.Application.DomainEvents;
using Aegis.Application.Features.Stores;
using Aegis.Contracts.Administration;
using Aegis.Infrastructure.Persistence;
using Aegis.SharedKernel;

namespace Aegis.UnitTests.Application.Features.Stores;

[Trait("Category", "ApplicationTests")]
[Trait("Feature", "Stores")]
public sealed class CreateStoreUseCaseTests
{
    [Fact]
    public async Task Execute_idempotent_replays_result_and_dispatches_creation_once()
    {
        var registry = new InMemoryStoreRegistry();
        var dispatcher = new RecordingDispatcher();
        var useCase = new CreateStoreUseCase(registry, registry, dispatcher);
        var request = new CreateStoreRequestDto("enterprise-docs");
        var fingerprint = new string('a', 64);

        var first = await useCase.ExecuteIdempotentAsync(
            "tenant-a", request, "user:alice", "store-create-0001", fingerprint);
        var replay = await useCase.ExecuteIdempotentAsync(
            "tenant-a", request, "user:alice", "store-create-0001", fingerprint);

        Assert.Equal(first, replay);
        Assert.Equal(1, dispatcher.DispatchCount);
        Assert.Single(await registry.ListForTenantAsync("tenant-a"));
    }

    [Fact]
    public async Task Execute_rejects_invalid_idempotency_context_before_persistence()
    {
        var registry = new InMemoryStoreRegistry();
        var useCase = new CreateStoreUseCase(registry, registry, new RecordingDispatcher());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteIdempotentAsync(
            "tenant-a",
            new CreateStoreRequestDto("enterprise-docs"),
            "user:alice",
            "store-create-0001",
            "not-a-sha256"));

        Assert.Empty(await registry.ListForTenantAsync("tenant-a"));
    }

    private sealed class RecordingDispatcher : IDomainEventDispatcher
    {
        public int DispatchCount { get; private set; }

        public Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            DispatchCount += domainEvents.Count();
            return Task.CompletedTask;
        }
    }
}
