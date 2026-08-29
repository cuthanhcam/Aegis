using Aegis.Application.Features.Assertions;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Compatibility;
using Aegis.Infrastructure.Authorization;
using Aegis.Infrastructure.Persistence;

namespace Aegis.UnitTests.Application.Features.Assertions;

[Trait("Category", "ApplicationTests")]
[Trait("Feature", "Assertions")]
public sealed class GenerateAssertionsFromAuditUseCaseTests
{
    private const string ModelText = "type user\n\ntype document\n  relations\n    define viewer: [user]";

    [Fact]
    public async Task Execute_rejects_invalid_decision_without_mutating_snapshot()
    {
        var context = await CreateContextAsync();

        var exception = await Assert.ThrowsAsync<CompatibilityApiException>(() =>
            context.UseCase.ExecuteAsync(
                context.StoreId,
                context.ModelId,
                new AegisGenerateAssertionsFromAuditRequestDto(Decision: "unknown", Append: true)));

        Assert.Equal("validation_error", exception.ErrorCode);
        Assert.Equal(0, (await context.Repository.ReadAsync(context.StoreId, context.ModelId)).Revision);
    }

    [Fact]
    public async Task Execute_maps_capacity_failure_and_preserves_existing_revision()
    {
        var context = await CreateContextAsync();
        var existing = Enumerable.Range(0, WriteAssertionsUseCase.MaximumAssertionsPerModel)
            .Select(index => Assertion($"user:existing-{index}"))
            .ToList();
        var before = await context.Repository.ReplaceAsync(context.StoreId, context.ModelId, existing);
        await context.AuditStore.WriteAsync(new AuditEvent(
            "tenant-a",
            "check",
            "user:new",
            "viewer",
            "document:roadmap",
            "Allow",
            "RELATIONSHIP_MATCH",
            DateTimeOffset.UtcNow,
            context.StoreId));

        var exception = await Assert.ThrowsAsync<CompatibilityApiException>(() =>
            context.UseCase.ExecuteAsync(
                context.StoreId,
                context.ModelId,
                new AegisGenerateAssertionsFromAuditRequestDto(Append: true)));

        Assert.Equal("assertions_too_many_items", exception.ErrorCode);
        var after = await context.Repository.ReadAsync(context.StoreId, context.ModelId);
        Assert.Equal(before.Revision, after.Revision);
        Assert.Equal(existing.Count, after.Assertions.Count);
    }

    private static async Task<TestContext> CreateContextAsync()
    {
        var registry = new InMemoryStoreRegistry();
        var store = await registry.CreateForTenantAsync("tenant-a", "docs");
        var model = await registry.CreateAsync(store.Id, "1.1", ModelText);
        var repository = new InMemoryAssertionRepository();
        var auditStore = new InMemoryAuditStore();
        var useCase = new GenerateAssertionsFromAuditUseCase(
            registry,
            registry,
            repository,
            auditStore,
            new AssertionValidator());
        return new TestContext(store.Id, model.Id, repository, auditStore, useCase);
    }

    private static AegisCompatAssertionDto Assertion(string user)
        => new(new AegisCompatTupleKeyDto(user, "viewer", "document:roadmap"), true);

    private sealed record TestContext(
        string StoreId,
        string ModelId,
        InMemoryAssertionRepository Repository,
        InMemoryAuditStore AuditStore,
        GenerateAssertionsFromAuditUseCase UseCase);
}
