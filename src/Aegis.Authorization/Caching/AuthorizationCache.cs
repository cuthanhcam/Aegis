using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Aegis.Authorization.Core.Models;

namespace Aegis.Authorization.Caching
{
    /// <summary>
    /// Lightweight in-memory cache for authorization decisions.
    /// </summary>
    public sealed class AuthorizationCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);
        private readonly TimeSpan _ttl;

        /// <summary>
        /// Creates a decision cache with optional custom TTL.
        /// </summary>
        public AuthorizationCache(TimeSpan? ttl = null)
        {
            _ttl = ttl ?? TimeSpan.FromSeconds(15);
        }

        /// <summary>
        /// Returns a cached decision when present and not expired.
        /// </summary>
        public bool TryGet(CheckRequest request, bool includeTrace, out DecisionResult result)
        {
            var key = BuildCacheKey(request, includeTrace);
            if (!_entries.TryGetValue(key, out var entry))
            {
                result = default!;
                return false;
            }

            if (entry.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                _entries.TryRemove(key, out _);
                result = default!;
                return false;
            }

            result = entry.Result;
            return true;
        }

        /// <summary>
        /// Stores a decision in cache using configured TTL.
        /// </summary>
        public void Set(CheckRequest request, bool includeTrace, DecisionResult result)
        {
            var key = BuildCacheKey(request, includeTrace);
            _entries[key] = new CacheEntry(result, DateTimeOffset.UtcNow.Add(_ttl), request.TenantId);
        }

        /// <summary>
        /// Removes cached decisions associated with one tenant.
        /// </summary>
        public int InvalidateTenant(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return 0;
            }

            var removed = 0;
            foreach (var pair in _entries)
            {
                if (!pair.Value.TenantId.Equals(tenantId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (_entries.TryRemove(pair.Key, out _))
                {
                    removed++;
                }
            }

            return removed;
        }

        /// <summary>
        /// Clears all cache entries.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
        }

        private static string BuildCacheKey(CheckRequest request, bool includeTrace)
        {
            var builder = new StringBuilder(512);
            builder.Append(request.TenantId).Append('|')
                .Append(request.Subject.Value).Append('|')
                .Append(request.Relation).Append('|')
                .Append(request.Object.Value).Append('|')
                .Append(request.Consistency).Append('|')
                .Append(request.AuthorizationModelId ?? string.Empty).Append('|')
                .Append(includeTrace ? '1' : '0');

            if (request.ContextualTuples is not null && request.ContextualTuples.Count > 0)
            {
                foreach (var tuple in request.ContextualTuples
                    .OrderBy(x => x.Subject.Value, StringComparer.Ordinal)
                    .ThenBy(x => x.Relation, StringComparer.Ordinal)
                    .ThenBy(x => x.Object.Value, StringComparer.Ordinal)
                    .ThenBy(x => x.Effect)
                    .ThenBy(x => x.CreatedAt))
                {
                    builder.Append("|ct:")
                        .Append(tuple.Subject.Value).Append(',')
                        .Append(tuple.Relation).Append(',')
                        .Append(tuple.Object.Value).Append(',')
                        .Append(tuple.Effect).Append(',')
                        .Append(tuple.CreatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
                }
            }

            if (request.Context is not null && request.Context.Count > 0)
            {
                foreach (var pair in request.Context.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    builder.Append("|ctx:")
                        .Append(pair.Key)
                        .Append('=')
                        .Append(pair.Value.GetRawText());
                }
            }

            return builder.ToString();
        }

        private sealed record CacheEntry(
            DecisionResult Result,
            DateTimeOffset ExpiresAtUtc,
            string TenantId);
    }
}
