using Aegis.Application.Contracts;
using Aegis.Application.DomainEvents;
using Aegis.Application.Features.AuthorizationModels;
using Aegis.Contracts.Administration;
using Aegis.Infrastructure.Persistence;
using Aegis.SharedKernel;

namespace Aegis.UnitTests.Application.AuthorizationModels;

[Trait("Category", "ApplicationTests")]
[Trait("Feature", "AuthorizationModels")]
public sealed class UpdateDeleteAuthorizationModelUseCaseTests
{
    [Fact]
    public async Task Update_UsesExpectedRevisionAndDispatchesOnlySuccessfulMutation()
    {
        var registry = new InMemoryStoreRegistry();
        var store = await registry.CreateAsync("enterprise-docs");
        var model = await registry.CreateAsync(
            store.Id,
            "1.1",
            "type user\n\ntype document\n  relations\n    define viewer: [user]");
        var dispatcher = new RecordingDispatcher();
        var useCase = new UpdateAuthorizationModelUseCase(
            registry,
            registry,
            registry,
            dispatcher,
            new AuthorizationModelValidator());
        var request = new CreateAuthorizationModelRequestDto(
            "1.1",
            "type user\n\ntype document\n  relations\n    define viewer: [user]\n    define editor: [user]");

        var updated = await useCase.ExecuteAsync(store.Id, model.Id, request, model.Revision);

        Assert.NotNull(updated);
        Assert.Equal(model.Revision + 1, updated.Revision);
        Assert.Equal(AuthorizationModelLifecycleStates.Validated, updated.State);
        Assert.Equal(1, dispatcher.DispatchCount);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            useCase.ExecuteAsync(store.Id, model.Id, request, model.Revision));
        Assert.Equal(1, dispatcher.DispatchCount);
    }

    [Fact]
    public async Task Delete_UsesExpectedRevisionAndDispatchesOnlyWhenRemoved()
    {
        var registry = new InMemoryStoreRegistry();
        var store = await registry.CreateAsync("enterprise-docs");
        var model = await registry.CreateAsync(store.Id, "1.1", "type document");
        var dispatcher = new RecordingDispatcher();
        var useCase = new DeleteAuthorizationModelUseCase(registry, registry, registry, dispatcher);

        var deleted = await useCase.ExecuteAsync(store.Id, model.Id, model.Revision);
        var missing = await useCase.ExecuteAsync(store.Id, "missing-model", 1);

        Assert.True(deleted);
        Assert.False(missing);
        Assert.Equal(1, dispatcher.DispatchCount);
    }

    [Fact]
    public async Task Delete_WithStaleRevision_ThrowsWithoutDispatching()
    {
        var registry = new InMemoryStoreRegistry();
        var store = await registry.CreateAsync("enterprise-docs");
        var model = await registry.CreateAsync(store.Id, "1.1", "type document");
        var dispatcher = new RecordingDispatcher();
        var useCase = new DeleteAuthorizationModelUseCase(registry, registry, registry, dispatcher);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            useCase.ExecuteAsync(store.Id, model.Id, model.Revision + 1));

        Assert.Equal(0, dispatcher.DispatchCount);
        Assert.NotNull(await registry.GetByIdAsync(store.Id, model.Id));
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
