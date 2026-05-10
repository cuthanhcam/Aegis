using Aegis.Contracts.Administration;

namespace Aegis.Application.Interfaces;

public interface IPresetAppService
{
    IReadOnlyList<PresetItemDto> List(string tenantId, string? storeId = null, string? source = null, string? scope = null);

    PresetItemDto Upsert(string tenantId, UpsertPresetRequestDto request);

    bool Delete(string tenantId, DeletePresetRequestDto request);

    IReadOnlyDictionary<string, PresetMetaDto> GetMeta(string tenantId);

    IReadOnlyDictionary<string, PresetMetaDto> SetMeta(string tenantId, IDictionary<string, PresetMetaDto> meta);
}
