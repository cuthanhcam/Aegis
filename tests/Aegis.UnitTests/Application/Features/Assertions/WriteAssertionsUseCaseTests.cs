using Aegis.Application.Features.Assertions;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Aegis.Infrastructure.Authorization;
using Aegis.Infrastructure.Persistence;

namespace Aegis.UnitTests.Application.Features.Assertions;

[Trait("Category", "ApplicationTests")]
[Trait("Feature", "Assertions")]
public sealed class WriteAssertionsUseCaseTests
{
    private const string ModelText = "type user\n\ntype document\n  relations\n    define viewer: [user]";

    [Fact]
    public async Task Execute_replaces_one_store_model_snapshot_and_advances_revision()
    {
        var registry = new InMemoryStoreRegistry();
        var store = await registry.CreateForTenantAsync("tenant-a", "docs");
        var model = await registry.CreateAsync(store.Id, "1.1", ModelText);
        var repository = new InMemoryAssertionRepository();
        var useCase = new WriteAssertionsUseCase(registry, registry, repository, new AssertionValidator());

        await useCase.ExecuteAsync(store.Id, model.Id, Request("user:anne"));
        await useCase.ExecuteAsync(store.Id, model.Id, Request("user:bob"));

        var snapshot = await repository.ReadAsync(store.Id, model.Id);
        Assert.Equal(2, snapshot.Revision);
        Assert.Single(snapshot.Assertions);
        Assert.Equal("user:bob", snapshot.Assertions[0].TupleKey.User);
    }

    [Fact]
    public async Task Execute_rejects_unknown_relation_without_mutating_snapshot()
    {
        var registry = new InMemoryStoreRegistry();
        var store = await registry.CreateForTenantAsync("tenant-a", "docs");
        var model = await registry.CreateAsync(store.Id, "1.1", ModelText);
        var repository = new InMemoryAssertionRepository();
        var useCase = new WriteAssertionsUseCase(registry, registry, repository, new AssertionValidator());
        var invalid = new AegisCompatWriteAssertionsRequestDto(
        [
            new AegisCompatAssertionDto(
                new AegisCompatTupleKeyDto("user:anne", "editor", "document:roadmap"),
                true),
        ]);

        var exception = await Assert.ThrowsAsync<CompatibilityApiException>(() =>
            useCase.ExecuteAsync(store.Id, model.Id, invalid));

        Assert.Equal("relation_not_found", exception.ErrorCode);
        Assert.Empty((await repository.ReadAsync(store.Id, model.Id)).Assertions);
    }

    [Fact]
    public async Task Execute_rejects_model_from_another_store()
    {
        var registry = new InMemoryStoreRegistry();
        var firstStore = await registry.CreateForTenantAsync("tenant-a", "first");
        var secondStore = await registry.CreateForTenantAsync("tenant-a", "second");
        var model = await registry.CreateAsync(firstStore.Id, "1.1", ModelText);
        var useCase = new WriteAssertionsUseCase(
            registry,
            registry,
            new InMemoryAssertionRepository(),
            new AssertionValidator());

        var exception = await Assert.ThrowsAsync<CompatibilityApiException>(() =>
            useCase.ExecuteAsync(secondStore.Id, model.Id, Request("user:anne")));

        Assert.Equal("authorization_model_not_found", exception.ErrorCode);
    }

    private static AegisCompatWriteAssertionsRequestDto Request(string user)
        => new(
        [
            new AegisCompatAssertionDto(
                new AegisCompatTupleKeyDto(user, "viewer", "document:roadmap"),
                true),
        ]);
}
