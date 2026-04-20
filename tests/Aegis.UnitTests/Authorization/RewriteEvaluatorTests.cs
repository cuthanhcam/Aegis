using Aegis.Authorization.Core.Engine.Rewrite;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;

namespace Aegis.UnitTests.Authorization;

public class RewriteEvaluatorTests
{
    [Fact]
    public async Task EvaluateTermAsync_MatchesThis_WhenDirectTupleExists()
    {
        var store = new InMemoryRelationshipStore();
        await store.UpsertAsync("tenant-rw", CreateTuple("user:charlie", "viewer", "document:spec", RelationshipEffect.Allow));

        var evaluator = new RewriteEvaluator(store, (_, _, _, _, _, _) => Task.FromResult(false));
        var request = new CheckRequest("tenant-rw", new Subject("user:charlie"), "viewer", new ObjectRef("document:spec"));
        var trace = new List<TraceStep>();

        var allowed = await evaluator.EvaluateTermAsync(
            request,
            new RewriteTerm(["this"], []),
            includeTrace: true,
            trace,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            depth: 0,
            CancellationToken.None);

        Assert.True(allowed);
        Assert.Contains(trace, step => step.Step == "REBAC_REWRITE" && step.Result == "MATCHED_THIS");
    }

    [Fact]
    public async Task EvaluateTermAsync_Fails_WhenConditionIsNotMet()
    {
        var store = new InMemoryRelationshipStore();
        await store.UpsertAsync("tenant-rw", CreateTuple("user:charlie", "viewer", "document:spec", RelationshipEffect.Allow));

        var evaluator = new RewriteEvaluator(store, (_, _, _, _, _, _) => Task.FromResult(false));
        var request = new CheckRequest(
            "tenant-rw",
            new Subject("user:charlie"),
            "viewer",
            new ObjectRef("document:spec"),
            null,
            ConsistencyPreference.MinimizeLatency,
            null,
            new Dictionary<string, System.Text.Json.JsonElement> { ["approved"] = System.Text.Json.JsonDocument.Parse("false").RootElement });

        var trace = new List<TraceStep>();

        var allowed = await evaluator.EvaluateTermAsync(
            request,
            new RewriteTerm(["this with approved"], []),
            includeTrace: true,
            trace,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            depth: 0,
            CancellationToken.None);

        Assert.False(allowed);
        Assert.Contains(trace, step => step.Step == "REBAC_REWRITE" && step.Result == "CONDITION_NOT_MET");
    }

    [Fact]
    public async Task EvaluateTermAsync_MatchesTupleToUserset_WhenNestedCheckAllows()
    {
        var store = new InMemoryRelationshipStore();
        await store.UpsertAsync("tenant-rw", CreateTuple("folder:root", "parent", "document:spec", RelationshipEffect.Allow));

        var evaluator = new RewriteEvaluator(
            store,
            (req, _, _, _, _, _) =>
            {
                var nestedMatches = string.Equals(req.Relation, "viewer", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(req.Object.Value, "folder:root", StringComparison.OrdinalIgnoreCase);
                return Task.FromResult(nestedMatches);
            });

        var request = new CheckRequest("tenant-rw", new Subject("user:charlie"), "viewer", new ObjectRef("document:spec"));
        var trace = new List<TraceStep>();

        var allowed = await evaluator.EvaluateTermAsync(
            request,
            new RewriteTerm(["viewer from parent"], []),
            includeTrace: true,
            trace,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            depth: 0,
            CancellationToken.None);

        Assert.True(allowed);
        Assert.Contains(trace, step => step.Step == "REBAC_REWRITE" && step.Result == "MATCHED_TUPLESET");
    }

    [Fact]
    public async Task EvaluateTermAsync_MatchesUserset_WhenTypeAndRelationAlign()
    {
        var store = new InMemoryRelationshipStore();
        await store.UpsertAsync("tenant-rw", CreateTuple("group:eng#member", "viewer", "document:spec", RelationshipEffect.Allow));

        var evaluator = new RewriteEvaluator(
            store,
            (req, _, _, _, _, _) =>
            {
                return Task.FromResult(
                    string.Equals(req.Relation, "member", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(req.Object.Value, "group:eng", StringComparison.OrdinalIgnoreCase));
            });

        var request = new CheckRequest("tenant-rw", new Subject("user:charlie"), "viewer", new ObjectRef("document:spec"));
        var trace = new List<TraceStep>();

        var allowed = await evaluator.EvaluateTermAsync(
            request,
            new RewriteTerm(["group#member"], []),
            includeTrace: true,
            trace,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            depth: 0,
            CancellationToken.None);

        Assert.True(allowed);
        Assert.Contains(trace, step => step.Step == "REBAC_REWRITE" && step.Result == "MATCHED_USERSET");
    }

    [Fact]
    public async Task EvaluateTermAsync_MatchesPlainTypeToken_WhenSubjectTypeMatches()
    {
        var store = new InMemoryRelationshipStore();
        await store.UpsertAsync("tenant-rw", CreateTuple("user:charlie", "viewer", "document:spec", RelationshipEffect.Allow));

        var evaluator = new RewriteEvaluator(store, (_, _, _, _, _, _) => Task.FromResult(false));
        var request = new CheckRequest("tenant-rw", new Subject("user:charlie"), "viewer", new ObjectRef("document:spec"));

        var allowed = await evaluator.EvaluateTermAsync(
            request,
            new RewriteTerm(["user"], []),
            includeTrace: false,
            trace: new List<TraceStep>(),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            depth: 0,
            CancellationToken.None);

        Assert.True(allowed);
    }

    [Fact]
    public async Task EvaluateTermAsync_RejectsWhenExcludeClauseMatches()
    {
        var store = new InMemoryRelationshipStore();
        await store.UpsertAsync("tenant-rw", CreateTuple("user:charlie", "viewer", "document:spec", RelationshipEffect.Allow));

        var evaluator = new RewriteEvaluator(store, (_, _, _, _, _, _) => Task.FromResult(false));
        var request = new CheckRequest("tenant-rw", new Subject("user:charlie"), "viewer", new ObjectRef("document:spec"));
        var trace = new List<TraceStep>();

        var allowed = await evaluator.EvaluateTermAsync(
            request,
            new RewriteTerm(["this"], [["this"]]),
            includeTrace: true,
            trace,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            depth: 0,
            CancellationToken.None);

        Assert.False(allowed);
        Assert.Contains(trace, step => step.Step == "REBAC_REWRITE" && step.Result == "EXCLUDED");
    }

    private static RelationshipTuple CreateTuple(string subject, string relation, string obj, RelationshipEffect effect)
    {
        return new RelationshipTuple(new Subject(subject), relation, new ObjectRef(obj), effect, DateTimeOffset.UtcNow);
    }

    private sealed class InMemoryRelationshipStore : IRelationshipStore
    {
        private readonly List<RelationshipTuple> _tuples = [];

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
