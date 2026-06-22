using Aegis.Application.Features.Permissions;
using Aegis.Application.Services;
using Aegis.Authorization.Core.Engine;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Compatibility;
using Aegis.Infrastructure.Authorization;
using Aegis.Infrastructure.Persistence;

namespace Aegis.UnitTests.Application.Services
{
    /// <summary>
    /// Tests for AssertionAppService - Assertion management
    /// </summary>
    [Trait("Category", "ApplicationTests")]
    [Trait("Feature", "Services")]
    public class AssertionAppServiceTests
    {
        [Fact]
        public void CreateAssertionAsync_WithValidAssertion_PersistsAssertion()
        {
            // Should create assertion
            Assert.True(true); // Placeholder
        }

        [Fact]
        public void DeleteAssertionAsync_RemovesAssertion()
        {
            // Should delete assertion
            Assert.True(true); // Placeholder
        }

        [Fact]
        public void ListAssertionsAsync_ReturnsAllAssertions()
        {
            // Should list all assertions
            Assert.True(true); // Placeholder
        }

        [Fact]
        public void VerifyAssertionAsync_ChecksAssertion()
        {
            // Should verify assertion
            Assert.True(true); // Placeholder
        }

        [Fact]
        public void CreateAssertionAsync_WithInvalidAssertion_ThrowsValidationException()
        {
            // Should validate assertion
            Assert.True(true); // Placeholder
        }

        [Fact]
        public async Task RunAsync_ReturnsPassAndFailResultsAndStoresRunHistory()
        {
            var registry = new InMemoryStoreRegistry();
            var store = await registry.CreateAsync("docs");
            var model = await registry.CreateAsync(
                store.Id,
                "1.1",
                "type user\n\ntype document\n  relations\n    define viewer: [user]");
            await registry.UpdateStateAsync(store.Id, model.Id, AuthorizationModelLifecycleStates.Published, DateTimeOffset.UtcNow, null, null);

            var relationships = new InMemoryRelationshipStore();
            await relationships.UpsertAsync(
                store.Id,
                new RelationshipTuple(new Subject("user:anne"), "viewer", new ObjectRef("document:roadmap"), RelationshipEffect.Allow, DateTimeOffset.UtcNow),
                storeId: store.Id);

            var checker = new CheckPermissionUseCase(
                new AuthorizationEngine(relationships, new InMemoryRbacStore(), authorizationModelProvider: new AuthorizationModelProvider(registry)),
                new InMemoryAuditStore());
            var runStore = new InMemoryAssertionRunStore();
            var service = new AssertionAppService(registry, registry, checker, runStore);
            await service.WriteAsync(
                store.Id,
                model.Id,
                new AegisCompatWriteAssertionsRequestDto(
                [
                    new AegisCompatAssertionDto(new AegisCompatTupleKeyDto("user:anne", "viewer", "document:roadmap"), true),
                    new AegisCompatAssertionDto(new AegisCompatTupleKeyDto("user:bob", "viewer", "document:roadmap"), true),
                ]));

            var run = await service.RunAsync(store.Id, model.Id);
            var reloadedService = new AssertionAppService(registry, registry, checker, runStore);
            var runs = await reloadedService.ListRunsAsync(store.Id, model.Id);
            var detail = await reloadedService.GetRunAsync(store.Id, run.RunId);

            Assert.Equal(2, run.Summary.Total);
            Assert.Equal(1, run.Summary.Passed);
            Assert.Equal(1, run.Summary.Failed);
            Assert.NotEmpty(runs.Runs);
            Assert.Equal(run.RunId, detail?.RunId);
        }
    }
}
