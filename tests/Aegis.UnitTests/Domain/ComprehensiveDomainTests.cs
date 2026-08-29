using Aegis.Application.DomainEvents;
using Aegis.Domain.Entities;
using Aegis.Domain.Enums;
using Aegis.Domain.Events;
using Aegis.Infrastructure.DomainEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.UnitTests.Domain;

/// <summary>
/// Comprehensive tests for aggregate behavior and domain events.
/// </summary>
public sealed class ComprehensiveDomainAggregateTests
{
    [Fact]
    public void Store_Create_NormalizesNameAndRaisesCreatedEvent()
    {
        var store = Store.Create("  My Store  ");

        Assert.Equal("My Store", store.Name);
        var evt = Assert.IsType<StoreCreatedDomainEvent>(Assert.Single(store.DomainEvents));
        Assert.Equal(store.Id, evt.StoreId);
        Assert.Equal("My Store", evt.StoreName);
    }

    [Fact]
    public void Store_Rehydrate_DoesNotRaiseEvents()
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var updatedAt = DateTimeOffset.UtcNow;

        var store = Store.Rehydrate("store-1", "Store One", createdAt, updatedAt);

        Assert.Equal("store-1", store.Id);
        Assert.Equal(createdAt, store.CreatedAt);
        Assert.Equal(updatedAt, store.UpdatedAt);
        Assert.Empty(store.DomainEvents);
    }

    [Fact]
    public void Store_MarkDeleted_RaisesDeletedEvent()
    {
        var store = Store.Create("To Delete");
        store.ClearDomainEvents();

        store.MarkDeleted();

        var evt = Assert.IsType<StoreDeletedDomainEvent>(Assert.Single(store.DomainEvents));
        Assert.Equal(store.Id, evt.StoreId);
        Assert.Equal(store.Name, evt.StoreName);
    }

    [Fact]
    public void AuthorizationModel_Create_PersistsSchemaAndModelAndRaisesEvent()
    {
        const string modelDsl = "type document\n  define viewer: [user]";
        var model = AuthorizationModel.Create("store-1", "1.1", modelDsl);

        Assert.Equal("store-1", model.StoreId);
        Assert.Equal("1.1", model.SchemaVersion);
        Assert.Equal(modelDsl, model.Model);

        var evt = Assert.IsType<AuthorizationModelCreatedDomainEvent>(Assert.Single(model.DomainEvents));
        Assert.Equal(model.Id, evt.AuthorizationModelId);
        Assert.Equal("store-1", evt.StoreId);
    }

    [Fact]
    public void AuthorizationModel_Rehydrate_DoesNotRaiseEvents()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var model = AuthorizationModel.Rehydrate("model-1", "store-1", "1.1", "type doc", createdAt);

        Assert.Equal("model-1", model.Id);
        Assert.Equal("store-1", model.StoreId);
        Assert.Equal("type doc", model.Model);
        Assert.Empty(model.DomainEvents);
    }

    [Fact]
    public void Relationship_Create_UsesValueObjectsAndRaisesUpsertEvent()
    {
        var relationship = Relationship.Create(
            "tenant-1",
            "user:alice",
            "viewer",
            "document:doc1",
            RelationshipPermissionEffect.Allow,
            DateTimeOffset.UtcNow);

        Assert.Equal("tenant-1", relationship.TenantId);
        Assert.Equal("user:alice", relationship.Subject.Value);
        Assert.Equal("viewer", relationship.Relation.Value);
        Assert.Equal("document:doc1", relationship.Object.Value);
        Assert.Equal(RelationshipPermissionEffect.Allow, relationship.Effect);

        var evt = Assert.IsType<RelationshipUpsertedDomainEvent>(Assert.Single(relationship.DomainEvents));
        Assert.Equal("allow", evt.Effect);
    }

    [Fact]
    public void Relationship_MarkDeleted_RaisesDeletedEvent()
    {
        var relationship = Relationship.Create(
            "tenant-1",
            "user:alice",
            "viewer",
            "document:doc1",
            RelationshipPermissionEffect.Allow,
            DateTimeOffset.UtcNow);
        relationship.ClearDomainEvents();

        relationship.MarkDeleted();

        var evt = Assert.IsType<RelationshipDeletedDomainEvent>(Assert.Single(relationship.DomainEvents));
        Assert.Equal("tenant-1", evt.TenantId);
        Assert.Equal("user:alice", evt.Subject);
    }

    [Fact]
    public void Relationship_Create_InvalidTuple_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Relationship.Create("tenant-1", "invalid-subject", "viewer", "document:doc1", RelationshipPermissionEffect.Allow));
    }
}

/// <summary>
/// Tests for outbox persistence and processing behavior.
/// </summary>
public sealed class DomainEventOutboxTests
{
    private readonly IDomainEventOutboxStore _outboxStore = new InMemoryDomainEventOutboxStore(TimeSpan.Zero);

    [Fact]
    public async Task AppendAndGetPending_ReturnsAppendedMessages()
    {
        await _outboxStore.AppendAsync(new StoreCreatedDomainEvent("store-1", "Store", DateTimeOffset.UtcNow), CancellationToken.None);

        var pending = await _outboxStore.GetPendingAsync(10, CancellationToken.None);

        Assert.Single(pending);
        Assert.Null(pending[0].ProcessedAt);
    }

    [Fact]
    public async Task MarkProcessed_RemovesMessageFromPending()
    {
        await _outboxStore.AppendAsync(new StoreCreatedDomainEvent("store-1", "Store", DateTimeOffset.UtcNow), CancellationToken.None);
        var message = (await _outboxStore.GetPendingAsync(1, CancellationToken.None)).Single();

        await _outboxStore.MarkProcessedAsync(message.Id, CancellationToken.None);

        var pending = await _outboxStore.GetPendingAsync(10, CancellationToken.None);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task MarkFailed_IncrementsAttemptCountAndKeepsPending()
    {
        await _outboxStore.AppendAsync(new StoreCreatedDomainEvent("store-1", "Store", DateTimeOffset.UtcNow), CancellationToken.None);
        var message = (await _outboxStore.GetPendingAsync(1, CancellationToken.None)).Single();

        await _outboxStore.MarkFailedAsync(message.Id, "boom", CancellationToken.None);

        var pending = await _outboxStore.GetPendingAsync(10, CancellationToken.None);
        Assert.Single(pending);
        Assert.Equal(1, pending[0].AttemptCount);
        Assert.Equal("boom", pending[0].LastError);
    }

    [Fact]
    public async Task OutboxProcessor_ProcessesAndMarksMessages()
    {
        await _outboxStore.AppendAsync(new StoreCreatedDomainEvent("store-1", "Store", DateTimeOffset.UtcNow), CancellationToken.None);

        var processor = new OutboxProcessor(_outboxStore, new NoOpPublisher());
        var processed = await processor.ProcessPendingAsync(10, CancellationToken.None);

        var pending = await _outboxStore.GetPendingAsync(10, CancellationToken.None);
        Assert.Equal(1, processed);
        Assert.Empty(pending);
    }

    private sealed class NoOpPublisher : IOutboxMessagePublisher
    {
        public Task PublishAsync(OutboxMessageEnvelope envelope, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// Tests for in-process domain event dispatching.
/// </summary>
public sealed class DomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_WithNoRegisteredHandlers_CompletesSuccessfully()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var dispatcher = new InProcessDomainEventDispatcher(provider);

        var events = new Aegis.SharedKernel.DomainEvent[]
        {
            new StoreCreatedDomainEvent("store-1", "Store", DateTimeOffset.UtcNow),
            new AuthorizationModelCreatedDomainEvent("model-1", "store-1", "1.1", DateTimeOffset.UtcNow),
        };

        await dispatcher.DispatchAsync(events, CancellationToken.None);
    }
}
