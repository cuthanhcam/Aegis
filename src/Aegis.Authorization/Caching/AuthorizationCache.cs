using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text;
using Aegis.Authorization.Core.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace Aegis.Authorization.Caching
{
    /// <summary>
    /// Lightweight in-memory cache for authorization decisions.
    /// </summary>
    public sealed class AuthorizationCache
    {
        private const string EntryPrefix = "aegis:authorization-cache:entry:";
        private const string TenantIndexPrefix = "aegis:authorization-cache:tenant:";
        private const string AllKeysIndex = "aegis:authorization-cache:keys";
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
        };

        private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);
        private readonly TimeSpan _ttl;
        private readonly IDistributedCache? _distributedCache;

        /// <summary>
        /// Creates a decision cache with optional custom TTL.
        /// </summary>
        public AuthorizationCache(TimeSpan? ttl = null, IDistributedCache? distributedCache = null)
        {
            _ttl = ttl ?? TimeSpan.FromSeconds(15);
            _distributedCache = distributedCache;
        }

        /// <summary>
        /// Returns a cached decision when present and not expired.
        /// </summary>
        public bool TryGet(CheckRequest request, bool includeTrace, out DecisionResult result)
        {
            var key = BuildCacheKey(request, includeTrace);
            if (!_entries.TryGetValue(key, out var entry))
            {
                if (TryGetDistributed(key, out entry))
                {
                    _entries[key] = entry;
                }
                else
                {
                    result = default!;
                    return false;
                }
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
            var entry = new CacheEntry(result, DateTimeOffset.UtcNow.Add(_ttl), request.TenantId);
            _entries[key] = entry;
            SetDistributed(key, entry);
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
            var distributedKeys = GetDistributedIndex(TenantIndexPrefix + tenantId);
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

            foreach (var key in distributedKeys)
            {
                if (_distributedCache is not null)
                {
                    _distributedCache.Remove(EntryPrefix + key);
                    removed++;
                }
            }

            if (_distributedCache is not null)
            {
                _distributedCache.Remove(TenantIndexPrefix + tenantId);
            }

            return removed;
        }

        /// <summary>
        /// Clears all cache entries.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            if (_distributedCache is null)
            {
                return;
            }

            foreach (var key in GetDistributedIndex(AllKeysIndex))
            {
                _distributedCache.Remove(EntryPrefix + key);
            }

            _distributedCache.Remove(AllKeysIndex);
        }

        private bool TryGetDistributed(string key, out CacheEntry entry)
        {
            entry = default!;
            if (_distributedCache is null)
            {
                return false;
            }

            var bytes = _distributedCache.Get(EntryPrefix + key);
            if (bytes is null || bytes.Length == 0)
            {
                return false;
            }

            entry = JsonSerializer.Deserialize<CacheEntry>(bytes, SerializerOptions) ?? default!;
            if (entry.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                _distributedCache.Remove(EntryPrefix + key);
                return false;
            }

            return true;
        }

        private void SetDistributed(string key, CacheEntry entry)
        {
            if (_distributedCache is null)
            {
                return;
            }

            var payload = JsonSerializer.SerializeToUtf8Bytes(entry, SerializerOptions);
            _distributedCache.Set(
                EntryPrefix + key,
                payload,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _ttl,
                });

            UpdateDistributedIndex(AllKeysIndex, key);
            UpdateDistributedIndex(TenantIndexPrefix + entry.TenantId, key);
        }

        private IReadOnlyList<string> GetDistributedIndex(string indexKey)
        {
            if (_distributedCache is null)
            {
                return Array.Empty<string>();
            }

            var bytes = _distributedCache.Get(indexKey);
            if (bytes is null || bytes.Length == 0)
            {
                return Array.Empty<string>();
            }

            return JsonSerializer.Deserialize<string[]>(bytes, SerializerOptions) ?? Array.Empty<string>();
        }

        private void UpdateDistributedIndex(string indexKey, string key)
        {
            if (_distributedCache is null)
            {
                return;
            }

            var keys = GetDistributedIndex(indexKey).ToList();
            if (!keys.Contains(key, StringComparer.Ordinal))
            {
                keys.Add(key);
            }

            _distributedCache.Set(
                indexKey,
                JsonSerializer.SerializeToUtf8Bytes(keys, SerializerOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _ttl,
                });
        }

        private static string BuildCacheKey(CheckRequest request, bool includeTrace)
        {
            var builder = new StringBuilder(512);
            builder.Append(request.TenantId).Append('|')
                .Append(request.EffectiveStoreId).Append('|')
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
