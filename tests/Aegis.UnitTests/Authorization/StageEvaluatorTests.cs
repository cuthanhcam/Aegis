using Aegis.Authorization.Core.Engine;
using Aegis.Authorization.Core.Engine.Evaluators;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;

namespace Aegis.UnitTests.Authorization;

public class StageEvaluatorTests
{
    [Fact]
    public async Task DenyPolicyStageEvaluator_ReturnsTerminalDeny_WhenDenyTupleExists()
    {
        var store = new InMemoryRelationshipStore([
            CreateTuple("user:charlie", "viewer", "document:spec", RelationshipEffect.Deny)]);

        var evaluator = new DenyPolicyStageEvaluator(store);
        var request = new CheckRequest("tenant-a", new Subject("user:charlie"), "viewer", new ObjectRef("document:spec"));
        var trace = new List<TraceStep>();

        var result = await evaluator.EvaluateAsync(request, includeTrace: true, trace, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.Allowed);
        Assert.Equal("DENY_EXPLICIT", result.ReasonCode);
        Assert.Contains(trace, step => step.Step == "DENY_POLICY" && step.Result == "MATCHED");
    }

    [Fact]
    public async Task DenyPolicyStageEvaluator_ReturnsNull_WhenNoDenyTupleExists()
    {
        var store = new InMemoryRelationshipStore(Array.Empty<RelationshipTuple>());
        var evaluator = new DenyPolicyStageEvaluator(store);
        var request = new CheckRequest("tenant-a", new Subject("user:charlie"), "viewer", new ObjectRef("document:spec"));
        var trace = new List<TraceStep>();

        var result = await evaluator.EvaluateAsync(request, includeTrace: true, trace, CancellationToken.None);

        Assert.Null(result);
        Assert.Contains(trace, step => step.Step == "DENY_POLICY" && step.Result == "NOT_MATCHED");
    }

    [Fact]
    public async Task RebacDirectStageEvaluator_ReturnsTerminalAllow_WhenDelegateAllows()
    {
        var request = new CheckRequest("tenant-a", new Subject("user:charlie"), "viewer", new ObjectRef("document:spec"));
        var trace = new List<TraceStep>();
        var evaluator = new RebacDirectStageEvaluator((_, _, _, _, _, _) => Task.FromResult(true));

        var result = await evaluator.EvaluateAsync(request, includeTrace: true, trace, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.Allowed);
        Assert.Equal("ALLOW_REBAC_DIRECT", result.ReasonCode);
        Assert.Contains(trace, step => step.Step == "REBAC_DIRECT" && step.Result == "MATCHED");
    }

    [Fact]
    public async Task RebacDirectStageEvaluator_ReturnsNull_WhenDelegateRejects()
    {
        var request = new CheckRequest("tenant-a", new Subject("user:charlie"), "viewer", new ObjectRef("document:spec"));
        var trace = new List<TraceStep>();
        var evaluator = new RebacDirectStageEvaluator((_, _, _, _, _, _) => Task.FromResult(false));

        var result = await evaluator.EvaluateAsync(request, includeTrace: true, trace, CancellationToken.None);

        Assert.Null(result);
        Assert.Contains(trace, step => step.Step == "REBAC_DIRECT" && step.Result == "NOT_MATCHED");
    }

    [Fact]
    public async Task RbacFallbackStageEvaluator_ReturnsAllow_WhenProviderAllows()
    {
        var evaluator = new RbacFallbackStageEvaluator(new FixedRbacProvider(true));
        var request = new CheckRequest("tenant-a", new Subject("user:charlie"), "viewer", new ObjectRef("document:spec"));
        var trace = new List<TraceStep>();

        var result = await evaluator.EvaluateAsync(request, includeTrace: true, trace, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.Allowed);
        Assert.Equal("ALLOW_RBAC", result.ReasonCode);
        Assert.Contains(trace, step => step.Step == "RBAC_FALLBACK" && step.Result == "MATCHED");
    }

    [Fact]
    public async Task RbacFallbackStageEvaluator_ReturnsDeny_WhenProviderRejects()
    {
        var evaluator = new RbacFallbackStageEvaluator(new FixedRbacProvider(false));
        var request = new CheckRequest("tenant-a", new Subject("user:charlie"), "viewer", new ObjectRef("document:spec"));
        var trace = new List<TraceStep>();

        var result = await evaluator.EvaluateAsync(request, includeTrace: true, trace, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.Allowed);
        Assert.Equal("DENY_NOT_FOUND", result.ReasonCode);
        Assert.Contains(trace, step => step.Step == "RBAC_FALLBACK" && step.Result == "NOT_MATCHED");
    }

    [Fact]
    public async Task QueryWithContextAsync_MergesAndDeduplicatesContextualTuplesByLatestTimestamp()
    {
        var older = CreateTuple("user:charlie", "viewer", "document:spec", RelationshipEffect.Allow, DateTimeOffset.UtcNow.AddMinutes(-10));
        var newer = CreateTuple("user:charlie", "viewer", "document:spec", RelationshipEffect.Allow, DateTimeOffset.UtcNow);
        var store = new InMemoryRelationshipStore(Array.Empty<RelationshipTuple>());

        var request = new CheckRequest(
            "tenant-a",
            new Subject("user:charlie"),
            "viewer",
            new ObjectRef("document:spec"),
            [older, newer]);

        var tuples = await AuthorizationStageSupport.QueryWithContextAsync(
            store,
            request,
            subject: new Subject("user:charlie"),
            relation: "viewer",
            obj: new ObjectRef("document:spec"),
            effect: RelationshipEffect.Allow,
            CancellationToken.None);

        Assert.Single(tuples);
        Assert.Equal(newer.CreatedAt, tuples.Single(x => x.Subject.Value == "user:charlie").CreatedAt);
    }

    [Fact]
    public void AddTrace_DoesNotAppend_WhenTraceDisabled()
    {
        var trace = new List<TraceStep>();

        AuthorizationStageSupport.AddTrace(trace, includeTrace: false, new TraceStep("STEP", "RESULT"));

        Assert.Empty(trace);
    }

    private static RelationshipTuple CreateTuple(string subject, string relation, string obj, RelationshipEffect effect, DateTimeOffset? createdAt = null)
    {
        return new RelationshipTuple(new Subject(subject), relation, new ObjectRef(obj), effect, createdAt ?? DateTimeOffset.UtcNow);
    }

    private sealed class FixedRbacProvider : IRbacProvider
    {
        private readonly bool _allowed;

        public FixedRbacProvider(bool allowed)
        {
            _allowed = allowed;
        }

        public Task<bool> HasPermissionAsync(CheckRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_allowed);
        }
    }

    private sealed class InMemoryRelationshipStore : IRelationshipStore
    {
        private readonly List<RelationshipTuple> _tuples;

        public InMemoryRelationshipStore(IEnumerable<RelationshipTuple> tuples)
        {
            _tuples = tuples.ToList();
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
            _tuples.Add(tuple);
            return Task.CompletedTask;
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
