using Aegis.Authorization.Caching;
using Aegis.Authorization.Core.Engine;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;

namespace Aegis.UnitTests.Authorization;

public class AuthorizationEngineCacheTests
{
    [Fact]
    public async Task CheckAsync_UsesCache_ForSameRequest()
    {
        var relationshipStore = new EmptyRelationshipStore();
        var rbacProvider = new CountingRbacProvider(allowed: true);
        var cache = new AuthorizationCache(TimeSpan.FromMinutes(1));
        var engine = new AuthorizationEngine(relationshipStore, rbacProvider, authorizationCache: cache);

        var request = new CheckRequest("tenant-a", new Subject("user:charlie"), "viewer", new ObjectRef("document:spec"));

        var first = await engine.CheckAsync(request, includeTrace: false);
        var second = await engine.CheckAsync(request, includeTrace: false);

        Assert.True(first.Allowed);
        Assert.True(second.Allowed);
        Assert.Equal(1, rbacProvider.CallCount);
    }

    [Fact]
    public async Task CheckAsync_DoesNotShareCache_BetweenTraceModes()
    {
        var relationshipStore = new EmptyRelationshipStore();
        var rbacProvider = new CountingRbacProvider(allowed: true);
        var cache = new AuthorizationCache(TimeSpan.FromMinutes(1));
        var engine = new AuthorizationEngine(relationshipStore, rbacProvider, authorizationCache: cache);

        var request = new CheckRequest("tenant-a", new Subject("user:charlie"), "viewer", new ObjectRef("document:spec"));

        _ = await engine.CheckAsync(request, includeTrace: false);
        _ = await engine.CheckAsync(request, includeTrace: true);

        Assert.Equal(2, rbacProvider.CallCount);
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

    private sealed class EmptyRelationshipStore : IRelationshipStore
    {
        public Task<IReadOnlyList<RelationshipTuple>> QueryAsync(
            string tenantId,
            Subject? subject,
            string? relation,
            ObjectRef? obj,
            RelationshipEffect? effect,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RelationshipTuple>>(Array.Empty<RelationshipTuple>());
        }

        public Task<IReadOnlyList<IReadOnlyList<RelationshipTuple>>> QueryMultipleAsync(
            string tenantId,
            IReadOnlyList<(Subject? subject, string? relation, ObjectRef? obj, RelationshipEffect? effect)> queries,
            CancellationToken cancellationToken = default)
        {
            var results = new List<IReadOnlyList<RelationshipTuple>>(queries.Count);
            for (int i = 0; i < queries.Count; i++)
            {
                results.Add(Array.Empty<RelationshipTuple>());
            }
            return Task.FromResult<IReadOnlyList<IReadOnlyList<RelationshipTuple>>>(results);
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
