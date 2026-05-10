using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using System.Collections.Concurrent;

namespace Aegis.Application.Services
{
    public sealed class PresetAppService : IPresetAppService
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PresetItemDto>> _tenantPresets =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PresetMetaDto>> _tenantMeta =
            new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<PresetItemDto> List(string tenantId, string? storeId = null, string? source = null, string? scope = null)
        {
            var presetMap = _tenantPresets.GetOrAdd(tenantId, _ => new(StringComparer.OrdinalIgnoreCase));
            return presetMap.Values
                .Where(item => string.IsNullOrWhiteSpace(storeId) || string.Equals(item.StoreId, storeId, StringComparison.OrdinalIgnoreCase))
                .Where(item => string.IsNullOrWhiteSpace(source) || string.Equals(item.Source, source, StringComparison.OrdinalIgnoreCase))
                .Where(item => string.IsNullOrWhiteSpace(scope) || string.Equals(item.Scope, scope, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.UpdatedAt)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public PresetItemDto Upsert(string tenantId, UpsertPresetRequestDto request)
        {
            ValidatePresetRequest(request.Source, request.StoreId, request.Scope, request.Name, request.Payload);

            var presetMap = _tenantPresets.GetOrAdd(tenantId, _ => new(StringComparer.OrdinalIgnoreCase));
            var updated = new PresetItemDto(
                request.Source.Trim(),
                request.StoreId.Trim(),
                request.Scope.Trim(),
                request.Name.Trim(),
                request.Payload,
                DateTimeOffset.UtcNow);

            presetMap[ComposeKey(updated.Source, updated.StoreId, updated.Scope, updated.Name)] = updated;
            return updated;
        }

        public bool Delete(string tenantId, DeletePresetRequestDto request)
        {
            ValidatePresetRequest(request.Source, request.StoreId, request.Scope, request.Name, "{}", requirePayload: false);

            var presetMap = _tenantPresets.GetOrAdd(tenantId, _ => new(StringComparer.OrdinalIgnoreCase));
            return presetMap.TryRemove(ComposeKey(request.Source, request.StoreId, request.Scope, request.Name), out _);
        }

        public IReadOnlyDictionary<string, PresetMetaDto> GetMeta(string tenantId)
        {
            var map = _tenantMeta.GetOrAdd(tenantId, _ => new(StringComparer.OrdinalIgnoreCase));
            return map.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyDictionary<string, PresetMetaDto> SetMeta(string tenantId, IDictionary<string, PresetMetaDto> meta)
        {
            ArgumentNullException.ThrowIfNull(meta);

            var map = _tenantMeta.GetOrAdd(tenantId, _ => new(StringComparer.OrdinalIgnoreCase));
            map.Clear();

            foreach (var pair in meta)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                map[pair.Key] = NormalizeMeta(pair.Value);
            }

            return map.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        private static void ValidatePresetRequest(string source, string storeId, string scope, string name, string payload, bool requirePayload = true)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("source is required.");
            }

            if (string.IsNullOrWhiteSpace(storeId))
            {
                throw new ArgumentException("storeId is required.");
            }

            if (string.IsNullOrWhiteSpace(scope))
            {
                throw new ArgumentException("scope is required.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name is required.");
            }

            if (requirePayload && string.IsNullOrWhiteSpace(payload))
            {
                throw new ArgumentException("payload is required.");
            }
        }

        private static PresetMetaDto NormalizeMeta(PresetMetaDto meta)
        {
            var tags = (meta.Tags ?? Array.Empty<string>())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new PresetMetaDto(meta.Pinned, meta.Favorite, tags, string.IsNullOrWhiteSpace(meta.Group) ? null : meta.Group.Trim());
        }

        private static string ComposeKey(string source, string storeId, string scope, string name)
            => $"{source.Trim().ToLowerInvariant()}::{storeId.Trim().ToLowerInvariant()}::{scope.Trim().ToLowerInvariant()}::{name.Trim().ToLowerInvariant()}";
    }
}
