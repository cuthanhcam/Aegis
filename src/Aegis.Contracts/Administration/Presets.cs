using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aegis.Contracts.Administration
{
    /// <summary>
    /// Read model for a persisted preset item.
    /// </summary>
    public sealed record PresetItemDto(
        string Source,
        string StoreId,
        string Scope,
        string Name,
        string Payload,
        DateTimeOffset UpdatedAt);

    public sealed record UpsertPresetRequestDto(
        string Source,
        string StoreId,
        string Scope,
        string Name,
        string Payload);

    public sealed record DeletePresetRequestDto(
        string Source,
        string StoreId,
        string Scope,
        string Name);

    /// <summary>
    /// Metadata attached to a preset item.
    /// </summary>
    public sealed record PresetMetaDto(
        bool Pinned,
        bool Favorite,
        IReadOnlyList<string> Tags,
        string? Group);

    /// <summary>
    /// Request payload for bulk updating preset metadata.
    /// </summary>
    public sealed record SetPresetMetaRequestDto(IDictionary<string, PresetMetaDto> Meta);
}
