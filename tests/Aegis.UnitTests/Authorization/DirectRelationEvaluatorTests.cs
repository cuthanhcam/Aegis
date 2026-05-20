using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Authorization.ReBAC;

namespace Aegis.UnitTests.Authorization;

public class DirectRelationEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_ReturnsDenied_WhenExplicitDenyExists()
    {
        var now = DateTimeOffset.UtcNow;
        var tuples = new[]
        {
            new RelationshipTuple(new Subject("user:alice"), "read", new ObjectRef("doc:1"), RelationshipEffect.Deny, now)
        };

        var store = new InMemoryRelationshipStore(tuples);
        var evaluator = new DirectRelationEvaluator(store);
        var request = BuildRequest("tenant-a", "user:alice", "read", "doc:1");

        var result = await evaluator.EvaluateAsync(request);

        Assert.True(result.IsTerminal);
        Assert.False(result.Allowed);
        Assert.Equal("DENY_EXPLICIT", result.ReasonCode);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsAllowed_WhenDirectAllowExists()
    {
        var now = DateTimeOffset.UtcNow;
        var tuples = new[]
        {
            new RelationshipTuple(new Subject("user:alice"), "read", new ObjectRef("doc:1"), RelationshipEffect.Allow, now)
        };

        var store = new InMemoryRelationshipStore(tuples);
        var evaluator = new DirectRelationEvaluator(store);
        var request = BuildRequest("tenant-a", "user:alice", "read", "doc:1");

        var result = await evaluator.EvaluateAsync(request);

        Assert.True(result.IsTerminal);
        Assert.True(result.Allowed);
        Assert.Equal("ALLOW_REBAC_DIRECT", result.ReasonCode);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsNoMatch_WhenNoTupleFound()
    {
        var store = new InMemoryRelationshipStore(Array.Empty<RelationshipTuple>());
        var evaluator = new DirectRelationEvaluator(store);
        var request = BuildRequest("tenant-a", "user:alice", "write", "doc:1");

        var result = await evaluator.EvaluateAsync(request);

        Assert.False(result.IsTerminal);
        Assert.False(result.Allowed);
        Assert.Equal("NOT_MATCHED", result.ReasonCode);
    }

    private static CheckRequest BuildRequest(string tenantId, string subject, string relation, string obj)
    {
        return new CheckRequest(
            TenantId: tenantId,
            Subject: new Subject(subject),
            Relation: relation,
            Object: new ObjectRef(obj));
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

        public Task<IReadOnlyList<IReadOnlyList<RelationshipTuple>>> QueryMultipleAsync(
            string tenantId,
            IReadOnlyList<(Subject? subject, string? relation, ObjectRef? obj, RelationshipEffect? effect)> queries,
            CancellationToken cancellationToken = default)
        {
            var results = new List<IReadOnlyList<RelationshipTuple>>(queries.Count);
            foreach (var (subject, relation, obj, effect) in queries)
            {
                var tuples = _tuples
                    .Where(x => subject is null || x.Subject == subject)
                    .Where(x => relation is null || x.Relation.Equals(relation, StringComparison.OrdinalIgnoreCase))
                    .Where(x => obj is null || x.Object == obj)
                    .Where(x => effect is null || x.Effect == effect)
                    .ToList();
                results.Add(tuples);
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
