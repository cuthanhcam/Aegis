using Aegis.Contracts.Common;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aegis.Contracts.Compatibility
{
    /// <summary>
    /// OpenFGA-compatible object reference payload.
    /// </summary>
    public sealed record AegisCompatObjectRefDto(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("relation")] string? Relation = null);

    /// <summary>
    /// OpenFGA-compatible user filter payload.
    /// </summary>
    public sealed record AegisCompatUserFilterDto(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("relation")] string? Relation = null);

    /// <summary>
    /// OpenFGA-compatible list-objects request.
    /// </summary>
    public sealed record AegisCompatListObjectsRequestDto(
        [property: JsonPropertyName("user")] string User,
        [property: JsonPropertyName("relation")] string Relation,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("contextual_tuples")] AegisCompatContextualTuplesDto? ContextualTuples = null,
        [property: JsonPropertyName("consistency")] string? Consistency = null,
        [property: JsonPropertyName("authorization_model_id")] string? AuthorizationModelId = null,
        [property: JsonPropertyName("context")] IReadOnlyDictionary<string, JsonElement>? Context = null);

    /// <summary>
    /// OpenFGA-compatible list-objects response.
    /// </summary>
    public sealed record AegisCompatListObjectsResponseDto(
        [property: JsonPropertyName("objects")] IReadOnlyList<string> Objects);

    /// <summary>
    /// OpenFGA-compatible streamed list-objects item.
    /// </summary>
    public sealed record AegisCompatStreamedListObjectsResponseDto(
        [property: JsonPropertyName("object")] string Object);

    /// <summary>
    /// OpenFGA-compatible list-users request.
    /// </summary>
    public sealed record AegisCompatListUsersRequestDto(
        [property: JsonPropertyName("object")] AegisCompatObjectRefDto Object,
        [property: JsonPropertyName("relation")] string Relation,
        [property: JsonPropertyName("user_filters")] IReadOnlyList<AegisCompatUserFilterDto>? UserFilters = null,
        [property: JsonPropertyName("contextual_tuples")] AegisCompatContextualTuplesDto? ContextualTuples = null,
        [property: JsonPropertyName("consistency")] string? Consistency = null,
        [property: JsonPropertyName("authorization_model_id")] string? AuthorizationModelId = null,
        [property: JsonPropertyName("context")] IReadOnlyDictionary<string, JsonElement>? Context = null);

    /// <summary>
    /// OpenFGA-compatible list-users entry.
    /// </summary>
    public sealed record AegisCompatUserEntryDto(
        [property: JsonPropertyName("object")] AegisCompatObjectRefDto Object);

    /// <summary>
    /// OpenFGA-compatible list-users response.
    /// </summary>
    public sealed record AegisCompatListUsersResponseDto(
        [property: JsonPropertyName("users")] IReadOnlyList<AegisCompatUserEntryDto> Users);

    /// <summary>
    /// OpenFGA-compatible expand request.
    /// </summary>
    public sealed record AegisCompatExpandRequestDto(
        [property: JsonPropertyName("tuple_key")] AegisCompatTupleKeyDto TupleKey,
        [property: JsonPropertyName("consistency")] string? Consistency = null,
        [property: JsonPropertyName("authorization_model_id")] string? AuthorizationModelId = null,
        [property: JsonPropertyName("contextual_tuples")] AegisCompatContextualTuplesDto? ContextualTuples = null,
        [property: JsonPropertyName("context")] IReadOnlyDictionary<string, JsonElement>? Context = null);

    /// <summary>
    /// OpenFGA-compatible expand response.
    /// </summary>
    public sealed record AegisCompatExpandResponseDto(
        [property: JsonPropertyName("tree")] ExpandNodeDto Tree);
}
