using Aegis.Application.Interfaces;
using Aegis.Application.Services;
using Aegis.Application.DomainEvents;
using Aegis.Contracts.Administration;
using Aegis.Infrastructure.Authorization;
using Aegis.Infrastructure.Persistence;
using Moq;

namespace Aegis.UnitTests.Application.Services
{
    [Trait("Category", "ApplicationTests")]
    [Trait("Feature", "Services")]
    public class AuthorizationModelAppServiceTests
    {
        [Fact]
        public async Task ValidateAsync_WithValidModel_ReturnsSummary()
        {
            var service = CreateService();
            var request = new ValidateAuthorizationModelRequestDto(
                "1.1",
                """
                type user

                type document
                  relations
                    define viewer: [user]
                    define editor: viewer or owner
                    define owner: [user]
                """);

            var result = await service.ValidateAsync(request);

            Assert.True(result.Valid);
            Assert.Empty(result.Errors);
            Assert.Equal(2, result.Summary.TypeCount);
            Assert.Equal(3, result.Summary.RelationCount);
            Assert.Equal(2, result.Summary.DirectRelationCount);
            Assert.True(result.Summary.HasUnion);
        }

        [Fact]
        public async Task ValidateAsync_WithDuplicateRelation_ReturnsError()
        {
            var service = CreateService();
            var request = new ValidateAuthorizationModelRequestDto(
                "1.1",
                """
                type user

                type document
                  relations
                    define viewer: [user]
                    define viewer: owner
                """);

            var result = await service.ValidateAsync(request);

            Assert.False(result.Valid);
            Assert.Contains(result.Errors, error => error.Code == "DUPLICATE_RELATION");
        }

        [Fact]
        public async Task ValidateAsync_WithRelationOutsideType_ReturnsError()
        {
            var service = CreateService();
            var request = new ValidateAuthorizationModelRequestDto(
                "1.1",
                """
                define viewer: [user]
                type user
                """);

            var result = await service.ValidateAsync(request);

            Assert.False(result.Valid);
            Assert.Contains(result.Errors, error => error.Code == "RELATION_OUTSIDE_TYPE");
        }

        [Fact]
        public async Task ValidateAsync_WithMinimalModel_ReturnsWarningsOnly()
        {
            var service = CreateService();
            var request = new ValidateAuthorizationModelRequestDto("1.1", "type document");

            var result = await service.ValidateAsync(request);

            Assert.True(result.Valid);
            Assert.Empty(result.Errors);
            Assert.Contains(result.Warnings, warning => warning.Code == "RELATION_RECOMMENDED");
            Assert.Contains(result.Warnings, warning => warning.Code == "DIRECT_RELATION_RECOMMENDED");
        }

        [Fact]
        public async Task PublishAsync_MarksTargetPublishedAndArchivesPreviousPublishedModel()
        {
            var registry = new InMemoryStoreRegistry();
            var store = await registry.CreateAsync("docs");
            var service = new AuthorizationModelAppService(registry, registry, registry, new NoopDomainEventDispatcher());
            var first = await service.CreateAsync(store.Id, new CreateAuthorizationModelRequestDto("1.1", "type user\n\ntype document\n  relations\n    define viewer: [user]"));
            var second = await service.CreateAsync(store.Id, new CreateAuthorizationModelRequestDto("1.1", "type user\n\ntype document\n  relations\n    define viewer: [user]\n    define editor: [user]"));

            await service.PublishAsync(store.Id, first.Id);
            var published = await service.PublishAsync(store.Id, second.Id);
            var models = await service.ListAsync(store.Id);

            Assert.NotNull(published);
            Assert.Equal(second.Id, published.ActiveModelId);
            Assert.Equal(AuthorizationModelLifecycleStates.Published, models.Single(x => x.Id == second.Id).State);
            Assert.Equal(AuthorizationModelLifecycleStates.Archived, models.Single(x => x.Id == first.Id).State);
            Assert.Equal(second.Id, models.Single(x => x.Id == first.Id).SupersededBy);
        }

        [Fact]
        public async Task RollbackAsync_RestoresArchivedModelAsPublished()
        {
            var registry = new InMemoryStoreRegistry();
            var store = await registry.CreateAsync("docs");
            var service = new AuthorizationModelAppService(registry, registry, registry, new NoopDomainEventDispatcher(), new InMemoryAuditStore());
            var first = await service.CreateAsync(store.Id, new CreateAuthorizationModelRequestDto("1.1", "type user\n\ntype document\n  relations\n    define viewer: [user]"));
            var second = await service.CreateAsync(store.Id, new CreateAuthorizationModelRequestDto("1.1", "type user\n\ntype document\n  relations\n    define viewer: [user]\n    define editor: [user]"));
            await service.PublishAsync(store.Id, first.Id);
            await service.PublishAsync(store.Id, second.Id);

            var rollback = await service.RollbackAsync(store.Id, first.Id);

            Assert.NotNull(rollback);
            Assert.Equal(first.Id, rollback.ActiveModelId);
            Assert.Equal(AuthorizationModelLifecycleStates.Published, (await service.GetByIdAsync(store.Id, first.Id))!.State);
            Assert.Equal(AuthorizationModelLifecycleStates.Archived, (await service.GetByIdAsync(store.Id, second.Id))!.State);
        }

        [Fact]
        public async Task DiffAsync_ReturnsChangedTypesRelationsAndBreakingHints()
        {
            var registry = new InMemoryStoreRegistry();
            var store = await registry.CreateAsync("docs");
            var service = new AuthorizationModelAppService(registry, registry, registry, new NoopDomainEventDispatcher());
            var left = await service.CreateAsync(store.Id, new CreateAuthorizationModelRequestDto("1.1", "type user\n\ntype document\n  relations\n    define viewer: [user]\n    define editor: [user]"));
            var right = await service.CreateAsync(store.Id, new CreateAuthorizationModelRequestDto("1.1", "type user\n\ntype document\n  relations\n    define viewer: [user]\n    define owner: [user]"));

            var diff = await service.DiffAsync(store.Id, left.Id, right.Id);

            Assert.NotNull(diff);
            Assert.Contains(diff.RemovedRelations, relation => relation.Type == "document" && relation.Relation == "editor");
            Assert.Contains(diff.AddedRelations, relation => relation.Type == "document" && relation.Relation == "owner");
            Assert.Contains(diff.BreakingChangeHints, hint => hint.Contains("document#editor", StringComparison.Ordinal));
        }

        private static AuthorizationModelAppService CreateService()
        {
            return new AuthorizationModelAppService(
                new Mock<IStoreRegistry>().Object,
                new Mock<IAuthorizationModelRegistry>().Object);
        }

        private sealed class NoopDomainEventDispatcher : IDomainEventDispatcher
        {
            public Task DispatchAsync(IEnumerable<Aegis.SharedKernel.DomainEvent> domainEvents, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
