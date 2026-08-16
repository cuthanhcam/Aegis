using System.Text.Json;
using Aegis.Authorization.Core.Engine;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;

namespace Aegis.UnitTests.Authorization;

public sealed class GoldenDecisionCorpusTests
{
    [Fact]
    public async Task Every_golden_scenario_produces_the_contractual_decision_and_reason()
    {
        var corpusPath = Path.Combine(AppContext.BaseDirectory, "Authorization", "GoldenDecisionCorpus.json");
        var corpus = JsonSerializer.Deserialize<GoldenCorpus>(
            await File.ReadAllTextAsync(corpusPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(corpus);
        Assert.Equal(1, corpus.SchemaVersion);
        Assert.NotEmpty(corpus.Scenarios);

        foreach (var scenario in corpus.Scenarios)
        {
            var engine = new AuthorizationEngine(
                new CorpusRelationshipStore(scenario.Relationships),
                new CorpusRbacProvider(scenario.RbacAllowed),
                authorizationModelProvider: new CorpusModelProvider(scenario.Model));

            var result = await engine.CheckAsync(
                new CheckRequest(
                    scenario.TenantId,
                    new Subject(scenario.Subject),
                    scenario.Relation,
                    new ObjectRef(scenario.Object)),
                includeTrace: true,
                CancellationToken.None);

            Assert.True(
                result.Allowed == scenario.ExpectedAllowed,
                $"Scenario '{scenario.Id}' expected allowed={scenario.ExpectedAllowed} but received {result.Allowed}.");
            Assert.True(
                string.Equals(result.ReasonCode, scenario.ExpectedReasonCode, StringComparison.Ordinal),
                $"Scenario '{scenario.Id}' expected reason '{scenario.ExpectedReasonCode}' but received '{result.ReasonCode}'.");
            Assert.NotEmpty(result.Trace);
        }
    }

    private sealed record GoldenCorpus(int SchemaVersion, IReadOnlyList<GoldenScenario> Scenarios);

    private sealed record GoldenScenario(
        string Id,
        string TenantId,
        string Subject,
        string Relation,
        string Object,
        IReadOnlyList<GoldenRelationship> Relationships,
        bool ExpectedAllowed,
        string ExpectedReasonCode,
        bool RbacAllowed = false,
        string? Model = null);

    private sealed record GoldenRelationship(
        string TenantId,
        string Subject,
        string Relation,
        string Object,
        string Effect);

    private sealed class CorpusRbacProvider(bool allowed) : IRbacProvider
    {
        public Task<bool> HasPermissionAsync(CheckRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(allowed);
    }

    private sealed class CorpusModelProvider(string? model) : IAuthorizationModelProvider
    {
        public Task<string?> GetLatestModelAsync(string storeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(model);

        public Task<string?> GetModelAsync(
            string storeId,
            string authorizationModelId,
            CancellationToken cancellationToken = default) => Task.FromResult(model);
    }

    private sealed class CorpusRelationshipStore : IRelationshipStore
    {
        private readonly IReadOnlyList<(string TenantId, RelationshipTuple Tuple)> _relationships;

        public CorpusRelationshipStore(IReadOnlyList<GoldenRelationship> relationships)
        {
            _relationships = relationships
                .Select(relationship => (
                    relationship.TenantId,
                    new RelationshipTuple(
                        new Subject(relationship.Subject),
                        relationship.Relation,
                        new ObjectRef(relationship.Object),
                        Enum.Parse<RelationshipEffect>(relationship.Effect, ignoreCase: true),
                        DateTimeOffset.UnixEpoch)))
                .ToList();
        }

        public Task<IReadOnlyList<RelationshipTuple>> QueryAsync(
            string tenantId,
            Subject? subject,
            string? relation,
            ObjectRef? obj,
            RelationshipEffect? effect,
            CancellationToken cancellationToken = default)
        {
            var matches = _relationships
                .Where(item => string.Equals(item.TenantId, tenantId, StringComparison.Ordinal))
                .Select(item => item.Tuple)
                .Where(tuple => subject is null || tuple.Subject == subject)
                .Where(tuple => relation is null || tuple.Relation.Equals(relation, StringComparison.OrdinalIgnoreCase))
                .Where(tuple => obj is null || tuple.Object == obj)
                .Where(tuple => effect is null || tuple.Effect == effect)
                .ToList();

            return Task.FromResult<IReadOnlyList<RelationshipTuple>>(matches);
        }

        public async Task<IReadOnlyList<IReadOnlyList<RelationshipTuple>>> QueryMultipleAsync(
            string tenantId,
            IReadOnlyList<(Subject? subject, string? relation, ObjectRef? obj, RelationshipEffect? effect)> queries,
            CancellationToken cancellationToken = default)
        {
            var results = new List<IReadOnlyList<RelationshipTuple>>(queries.Count);
            foreach (var (subject, relation, obj, effect) in queries)
            {
                results.Add(await QueryAsync(tenantId, subject, relation, obj, effect, cancellationToken));
            }

            return results;
        }

        public Task UpsertAsync(string tenantId, RelationshipTuple tuple, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(
            string tenantId,
            Subject subject,
            string relation,
            ObjectRef obj,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<RelationshipChange>> ReadChangesAsync(
            string tenantId,
            int offset,
            int limit,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task PurgeTenantAsync(string tenantId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
