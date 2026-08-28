using Aegis.Application.Interfaces;
using Aegis.Contracts.Compatibility;
using Aegis.Infrastructure.Authorization;

namespace Aegis.UnitTests.Infrastructure.Authorization;

[Trait("Category", "InfrastructureTests")]
[Trait("Feature", "Assertions")]
public sealed class InMemoryAssertionRepositoryTests
{
    [Fact]
    public async Task Concurrent_appends_are_serialized_deduplicated_and_revisioned()
    {
        var repository = new InMemoryAssertionRepository();
        var first = Assertion("user:anne");
        var second = Assertion("user:bob");

        await Task.WhenAll(
            repository.AppendDistinctAsync("store-a", "model-a", [first], 100),
            repository.AppendDistinctAsync("store-a", "model-a", [first, second], 100));

        var snapshot = await repository.ReadAsync("store-a", "model-a");
        Assert.Equal(2, snapshot.Revision);
        Assert.Equal(2, snapshot.Assertions.Count);
    }

    [Fact]
    public async Task Capacity_failure_does_not_mutate_existing_snapshot()
    {
        var repository = new InMemoryAssertionRepository();
        await repository.ReplaceAsync("store-a", "model-a", [Assertion("user:anne")]);

        await Assert.ThrowsAsync<AssertionSetCapacityExceededException>(() =>
            repository.AppendDistinctAsync("store-a", "model-a", [Assertion("user:bob")], 1));

        var snapshot = await repository.ReadAsync("store-a", "model-a");
        Assert.Equal(1, snapshot.Revision);
        Assert.Single(snapshot.Assertions);
    }

    [Fact]
    public async Task Purge_removes_only_target_store_sets()
    {
        var repository = new InMemoryAssertionRepository();
        await repository.ReplaceAsync("store-a", "model-a", [Assertion("user:anne")]);
        await repository.ReplaceAsync("store-b", "model-b", [Assertion("user:bob")]);

        await repository.PurgeStoreAsync("store-a");

        Assert.Empty((await repository.ReadAsync("store-a", "model-a")).Assertions);
        Assert.Single((await repository.ReadAsync("store-b", "model-b")).Assertions);
    }

    private static AegisCompatAssertionDto Assertion(string user)
        => new(new AegisCompatTupleKeyDto(user, "viewer", "document:roadmap"), true);
}
