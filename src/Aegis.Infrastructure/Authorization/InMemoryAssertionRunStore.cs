using Aegis.Application.Interfaces;
using Aegis.Contracts.Compatibility;
using System.Collections.Concurrent;

namespace Aegis.Infrastructure.Authorization
{
    public sealed class InMemoryAssertionRunStore : IAssertionRunStore
    {
        private readonly ConcurrentDictionary<string, AegisAssertionRunRecordDto> _runsById = new(StringComparer.OrdinalIgnoreCase);

        public Task SaveAsync(AegisAssertionRunRecordDto record, CancellationToken cancellationToken = default)
        {
            _runsById[BuildKey(record.StoreId, record.RunId)] = record;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AegisAssertionRunRecordDto>> ListByModelAsync(
            string storeId,
            string authorizationModelId,
            int limit = 25,
            CancellationToken cancellationToken = default)
        {
            var runs = _runsById.Values
                .Where(x => x.StoreId.Equals(storeId, StringComparison.OrdinalIgnoreCase)
                    && x.AuthorizationModelId.Equals(authorizationModelId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.StartedAt)
                .Take(limit)
                .ToList();

            return Task.FromResult<IReadOnlyList<AegisAssertionRunRecordDto>>(runs);
        }

        public Task<AegisAssertionRunRecordDto?> GetAsync(
            string storeId,
            string runId,
            CancellationToken cancellationToken = default)
        {
            _runsById.TryGetValue(BuildKey(storeId, runId), out var run);
            return Task.FromResult(run);
        }

        public Task PurgeStoreAsync(string storeId, CancellationToken cancellationToken = default)
        {
            foreach (var key in _runsById.Keys
                         .Where(x => x.StartsWith($"{storeId}:", StringComparison.OrdinalIgnoreCase))
                         .ToArray())
            {
                _runsById.TryRemove(key, out _);
            }

            return Task.CompletedTask;
        }

        private static string BuildKey(string storeId, string runId)
            => $"{storeId}:{runId}";
    }
}
