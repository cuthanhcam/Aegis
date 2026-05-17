using Aegis.Authorization.Core.Engine;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;

namespace Aegis.UnitTests.Authorization;

public class AuthorizationEngineDeterministicFlowTests
{
    [Fact]
    public async Task CheckAsync_DenyTuple_ShortCircuitsBeforeRbacFallback()
    {
        var relationshipStore = new InMemoryRelationshipStore(
        [
            Tuple("user:alice", "viewer", "document:spec", RelationshipEffect.Deny)
        ]);
        var rbac = new CountingRbacProvider(allowed: true);
        var engine = new AuthorizationEngine(relationshipStore, rbac);

        var result = await engine.CheckAsync(
            new CheckRequest("tenant-a", new Subject("user:alice"), "viewer", new ObjectRef("document:spec")),
            includeTrace: true,
            CancellationToken.None);

        Assert.False(result.Allowed);
        Assert.Equal("DENY_EXPLICIT", result.ReasonCode);
        Assert.Equal(0, rbac.CallCount);
    }

    [Fact]
    public async Task CheckAsync_RewriteAllow_ShortCircuitsBeforeRbacFallback()
    {
        var relationshipStore = new InMemoryRelationshipStore(
        [
            Tuple("folder:root", "parent", "document:spec", RelationshipEffect.Allow),
            Tuple("user:alice", "viewer", "folder:root", RelationshipEffect.Allow)
        ]);

        const string model = """
            type document
              define viewer: viewer from parent
            type folder
              define viewer: this
            """;

        var rbac = new CountingRbacProvider(allowed: true);
        var engine = new AuthorizationEngine(
            relationshipStore,
            rbac,
            authorizationModelProvider: new FixedModelProvider(model));

        var result = await engine.CheckAsync(
            new CheckRequest("tenant-a", new Subject("user:alice"), "viewer", new ObjectRef("document:spec")),
            includeTrace: true,
            CancellationToken.None);

        Assert.True(result.Allowed);
        Assert.Equal("ALLOW_REBAC_DIRECT", result.ReasonCode);
        Assert.Equal(0, rbac.CallCount);
    }

    [Fact]
    public async Task CheckAsync_RewriteMiss_FallsBackToRbacDeterministically()
    {
        var relationshipStore = new InMemoryRelationshipStore(Array.Empty<RelationshipTuple>());

        const string model = """
            type document
              define viewer: viewer from parent
            type folder
              define viewer: this
            """;

        var rbac = new CountingRbacProvider(allowed: true);
        var engine = new AuthorizationEngine(
            relationshipStore,
            rbac,
            authorizationModelProvider: new FixedModelProvider(model));

        var result = await engine.CheckAsync(
            new CheckRequest("tenant-a", new Subject("user:alice"), "viewer", new ObjectRef("document:spec")),
            includeTrace: true,
            CancellationToken.None);

        Assert.True(result.Allowed);
        Assert.Equal("ALLOW_RBAC", result.ReasonCode);
        Assert.Equal(1, rbac.CallCount);
    }

    private static RelationshipTuple Tuple(string subject, string relation, string obj, RelationshipEffect effect)
    {
        return new RelationshipTuple(new Subject(subject), relation, new ObjectRef(obj), effect, DateTimeOffset.UtcNow);
    }

    private sealed class CountingRbacProvider : IRbacProvider
    {
        private readonly bool _allowed;

        public CountingRbacProvider(bool allowed)
        {
            _allowed = allowed;
        }

        public int CallCount { get; private set; }

        public Task<bool> HasPermissionAsync(CheckRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_allowed);
        }
    }

    private sealed class FixedModelProvider : IAuthorizationModelProvider
    {
        private readonly string _model;

        public FixedModelProvider(string model)
        {
            _model = model;
        }

        public Task<string?> GetLatestModelAsync(string storeId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>(_model);
        }

        public Task<string?> GetModelAsync(string storeId, string authorizationModelId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>(_model);
        }
    }

    private sealed class InMemoryRelationshipStore : IRelationshipStore
    {
        private readonly IReadOnlyList<RelationshipTuple> _tuples;

        public InMemoryRelationshipStore(IReadOnlyList<RelationshipTuple> tuples)
        {
            _tuples = tuples;
        }

        public Task<IReadOnlyList<RelationshipTuple>> QueryAsync(
            string tenantId,
            Subject? subject,
            string? relation,
            ObjectRef? obj,
            RelationshipEffect? effect,
            CancellationToken cancellationToken = default)
        {
            var result = _tuples
                .Where(x => subject is null || x.Subject == subject)
                .Where(x => relation is null || x.Relation.Equals(relation, StringComparison.OrdinalIgnoreCase))
                .Where(x => obj is null || x.Object == obj)
                .Where(x => effect is null || x.Effect == effect)
                .ToList();

            return Task.FromResult<IReadOnlyList<RelationshipTuple>>(result);
        }

        public Task UpsertAsync(string tenantId, RelationshipTuple tuple, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteAsync(string tenantId, Subject subject, string relation, ObjectRef obj, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<RelationshipChange>> ReadChangesAsync(string tenantId, int offset, int limit, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task PurgeTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
