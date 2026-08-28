using Aegis.Application.Interfaces;
using Aegis.Contracts.Compatibility;
using System.Collections.Concurrent;

namespace Aegis.Infrastructure.Authorization;

public sealed class InMemoryAssertionRepository : IAssertionRepository
{
    private readonly ConcurrentDictionary<string, AssertionSetSnapshot> _sets = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, object> _locks = new(StringComparer.OrdinalIgnoreCase);

    public Task<AssertionSetSnapshot> ReadAsync(
        string storeId,
        string authorizationModelId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildKey(storeId, authorizationModelId);
        return Task.FromResult(_sets.TryGetValue(key, out var snapshot)
            ? snapshot
            : Empty(storeId, authorizationModelId));
    }

    public Task<AssertionSetSnapshot> ReplaceAsync(
        string storeId,
        string authorizationModelId,
        IReadOnlyList<AegisCompatAssertionDto> assertions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildKey(storeId, authorizationModelId);
        lock (_locks.GetOrAdd(key, static _ => new object()))
        {
            var revision = _sets.TryGetValue(key, out var current) ? current.Revision + 1 : 1;
            var snapshot = new AssertionSetSnapshot(storeId, authorizationModelId, revision, assertions.ToList());
            _sets[key] = snapshot;
            return Task.FromResult(snapshot);
        }
    }

    public Task<AssertionSetSnapshot> AppendDistinctAsync(
        string storeId,
        string authorizationModelId,
        IReadOnlyList<AegisCompatAssertionDto> assertions,
        int maximum,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildKey(storeId, authorizationModelId);
        lock (_locks.GetOrAdd(key, static _ => new object()))
        {
            var current = _sets.TryGetValue(key, out var stored) ? stored : Empty(storeId, authorizationModelId);
            var combined = Distinct(current.Assertions.Concat(assertions));
            if (combined.Count > maximum)
            {
                throw new AssertionSetCapacityExceededException(maximum);
            }

            var snapshot = new AssertionSetSnapshot(storeId, authorizationModelId, current.Revision + 1, combined);
            _sets[key] = snapshot;
            return Task.FromResult(snapshot);
        }
    }

    public Task PurgeStoreAsync(string storeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var key in _sets.Keys.Where(x => x.StartsWith($"{storeId}:", StringComparison.OrdinalIgnoreCase)))
        {
            _sets.TryRemove(key, out _);
            _locks.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    internal static List<AegisCompatAssertionDto> Distinct(IEnumerable<AegisCompatAssertionDto> assertions)
    {
        return assertions
            .GroupBy(BuildIdentity, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static string BuildIdentity(AegisCompatAssertionDto assertion)
        => $"{assertion.TupleKey.User}\u001f{assertion.TupleKey.Relation}\u001f{assertion.TupleKey.Object}\u001f{assertion.Expectation}";

    private static AssertionSetSnapshot Empty(string storeId, string authorizationModelId)
        => new(storeId, authorizationModelId, 0, []);

    private static string BuildKey(string storeId, string authorizationModelId)
        => $"{storeId}:{authorizationModelId}";
}
