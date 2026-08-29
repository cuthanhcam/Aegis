using Aegis.Application.Features.Assertions;
using Aegis.Application.Features.Permissions;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Engine;
using Aegis.Contracts.Compatibility;
using Aegis.Infrastructure.Authorization;
using Aegis.Infrastructure.Persistence;

namespace Aegis.UnitTests.Application.Features.Assertions;

[Trait("Category", "ApplicationTests")]
[Trait("Feature", "Assertions")]
public sealed class RunAssertionsUseCaseTests
{
    [Fact]
    public async Task Execute_with_empty_snapshot_persists_a_zero_result_run()
    {
        var registry = new InMemoryStoreRegistry();
        var store = await registry.CreateForTenantAsync("tenant-a", "docs");
        var model = await registry.CreateAsync(
            store.Id,
            "1.1",
            "type user\n\ntype document\n  relations\n    define viewer: [user]");
        var runStore = new InMemoryAssertionRunStore();
        var useCase = CreateUseCase(registry, new InMemoryAssertionRepository(), runStore);

        var run = await useCase.ExecuteAsync(store.Id, model.Id);

        Assert.Equal(0, run.Summary.Total);
        Assert.Equal(0, run.DefinitionRevision);
        Assert.Equal(0, run.Summary.Passed);
        Assert.Equal(0, run.Summary.Failed);
        Assert.Equal(run, await runStore.GetAsync(store.Id, run.RunId));
    }

    [Fact]
    public async Task Execute_with_model_from_another_store_does_not_persist_history()
    {
        var registry = new InMemoryStoreRegistry();
        var firstStore = await registry.CreateForTenantAsync("tenant-a", "first");
        var secondStore = await registry.CreateForTenantAsync("tenant-a", "second");
        var model = await registry.CreateAsync(firstStore.Id, "1.1", "type user");
        var runStore = new InMemoryAssertionRunStore();
        var useCase = CreateUseCase(registry, new InMemoryAssertionRepository(), runStore);

        var exception = await Assert.ThrowsAsync<CompatibilityApiException>(() =>
            useCase.ExecuteAsync(secondStore.Id, model.Id));

        Assert.Equal("authorization_model_not_found", exception.ErrorCode);
        Assert.Empty(await runStore.ListByModelAsync(secondStore.Id, model.Id));
    }

    private static RunAssertionsUseCase CreateUseCase(
        InMemoryStoreRegistry registry,
        IAssertionRepository assertionRepository,
        IAssertionRunStore runStore)
    {
        var auditStore = new InMemoryAuditStore();
        var checker = new CheckPermissionUseCase(
            new AuthorizationEngine(
                new InMemoryRelationshipStore(),
                new InMemoryRbacStore(),
                authorizationModelProvider: new AuthorizationModelProvider(registry)),
            auditStore);
        return new RunAssertionsUseCase(registry, registry, assertionRepository, checker, runStore);
    }
}
