using Aegis.Application.Contracts;
using Aegis.Application.Features.AuthorizationModels;
using Aegis.Contracts.Administration;
using Aegis.Infrastructure.Authorization;
using Aegis.Infrastructure.Persistence;

namespace Aegis.UnitTests.Application.AuthorizationModels;

[Trait("Category", "ApplicationTests")]
[Trait("Feature", "AuthorizationModels")]
public sealed class AuthorizationModelLifecycleUseCaseTests
{
    [Fact]
    public async Task Publish_ArchivesPreviousAndKeepsSinglePublishedModel()
    {
        var registry = new InMemoryStoreRegistry();
        var store = await registry.CreateAsync("enterprise-docs");
        var first = await registry.CreateAsync(store.Id, "1.1", "type document");
        var second = await registry.CreateAsync(store.Id, "1.1", "type document\n  relations\n    define viewer: [user]");
        var useCase = new PublishAuthorizationModelUseCase(
            registry,
            registry,
            new AuthorizationModelValidator());

        await useCase.ExecuteAsync(store.Id, first.Id, first.Revision);
        var published = await useCase.ExecuteAsync(store.Id, second.Id, second.Revision);
        var models = await registry.ListAsync(store.Id);

        Assert.NotNull(published);
        Assert.Equal(second.Id, published.ActiveModelId);
        Assert.Single(models, model => model.State == AuthorizationModelLifecycleStates.Published);
        Assert.Equal(AuthorizationModelLifecycleStates.Archived, models.Single(model => model.Id == first.Id).State);
        Assert.Equal(second.Id, models.Single(model => model.Id == first.Id).SupersededBy);
    }

    [Fact]
    public async Task Publish_WithStaleRevision_DoesNotChangeLifecycle()
    {
        var registry = new InMemoryStoreRegistry();
        var store = await registry.CreateAsync("enterprise-docs");
        var model = await registry.CreateAsync(store.Id, "1.1", "type document");
        var useCase = new PublishAuthorizationModelUseCase(
            registry,
            registry,
            new AuthorizationModelValidator());

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            useCase.ExecuteAsync(store.Id, model.Id, model.Revision + 1));

        Assert.Null(await registry.GetPublishedAsync(store.Id));
    }

    [Fact]
    public async Task Rollback_RestoresArchivedTargetAndWritesAuditAfterSuccess()
    {
        var registry = new InMemoryStoreRegistry();
        var auditStore = new InMemoryAuditStore();
        var store = await registry.CreateAsync("enterprise-docs");
        var first = await registry.CreateAsync(store.Id, "1.1", "type document");
        var second = await registry.CreateAsync(store.Id, "1.1", "type document\n  relations\n    define viewer: [user]");
        var validator = new AuthorizationModelValidator();
        var publish = new PublishAuthorizationModelUseCase(registry, registry, validator);
        var rollback = new RollbackAuthorizationModelUseCase(registry, registry, validator, auditStore);
        await publish.ExecuteAsync(store.Id, first.Id, first.Revision);
        await publish.ExecuteAsync(store.Id, second.Id, second.Revision);
        var archivedFirst = await registry.GetByIdAsync(store.Id, first.Id);

        var result = await rollback.ExecuteAsync(store.Id, first.Id, archivedFirst!.Revision);
        var auditEvents = await auditStore.QueryAsync(store.Id, "model.rollback", "Allow", store.Id);

        Assert.NotNull(result);
        Assert.Equal(first.Id, result.ActiveModelId);
        Assert.Equal(second.Id, result.RolledBackFromModelId);
        Assert.Equal(first.Id, (await registry.GetPublishedAsync(store.Id))!.Id);
        var auditEvent = Assert.Single(auditEvents);
        Assert.Equal("MODEL_ROLLED_BACK", auditEvent.ReasonCode);
    }
}
