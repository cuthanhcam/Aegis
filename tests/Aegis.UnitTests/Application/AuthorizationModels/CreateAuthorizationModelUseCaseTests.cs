using Aegis.Application.DomainEvents;
using Aegis.Application.Features.AuthorizationModels;
using Aegis.Contracts.Administration;
using Aegis.Infrastructure.Persistence;
using Aegis.SharedKernel;

namespace Aegis.UnitTests.Application.AuthorizationModels;

[Trait("Category", "ApplicationTests")]
[Trait("Feature", "AuthorizationModels")]
public sealed class CreateAuthorizationModelUseCaseTests
{
    [Fact]
    public async Task ExecuteIdempotentAsync_ReplaysResultAndDispatchesCreationOnce()
    {
        var registry = new InMemoryStoreRegistry();
        var store = await registry.CreateAsync("enterprise-docs");
        var dispatcher = new RecordingDispatcher();
        var useCase = new CreateAuthorizationModelUseCase(
            registry,
            registry,
            registry,
            dispatcher,
            new AuthorizationModelValidator());
        var request = new CreateAuthorizationModelRequestDto(
            "1.1",
            "type user\n\ntype document\n  relations\n    define viewer: [user]");

        var first = await useCase.ExecuteIdempotentAsync(
            store.Id, request, "tenant-a", "user:alice", "model-create-0001", new string('a', 64));
        var replay = await useCase.ExecuteIdempotentAsync(
            store.Id, request, "tenant-a", "user:alice", "model-create-0001", new string('a', 64));

        Assert.Equal(first, replay);
        Assert.Equal(1, dispatcher.DispatchCount);
        Assert.Single(await registry.ListAsync(store.Id));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsInvalidDefinitionBeforePersistence()
    {
        var registry = new InMemoryStoreRegistry();
        var store = await registry.CreateAsync("enterprise-docs");
        var dispatcher = new RecordingDispatcher();
        var useCase = new CreateAuthorizationModelUseCase(
            registry,
            registry,
            registry,
            dispatcher,
            new AuthorizationModelValidator());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(
            store.Id,
            new CreateAuthorizationModelRequestDto("1.1", "define viewer: [user]")));

        Assert.Empty(await registry.ListAsync(store.Id));
        Assert.Equal(0, dispatcher.DispatchCount);
    }

    private sealed class RecordingDispatcher : IDomainEventDispatcher
    {
        public int DispatchCount { get; private set; }

        public Task DispatchAsync(
            IEnumerable<DomainEvent> domainEvents,
            CancellationToken cancellationToken = default)
        {
            DispatchCount += domainEvents.Count();
            return Task.CompletedTask;
        }
    }
}
