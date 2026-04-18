using Aegis.Contracts.Common;
using System.Text.Json;

namespace Aegis.Contracts.Query
{
    /// <summary>
    /// Request payload for listing users that can satisfy a relation/object pair.
    /// </summary>
    public sealed record ListUsersRequestDto(
        string Relation,
        string Object,
        string? Consistency = null,
        IReadOnlyList<ContextualTupleDto>? ContextualTuples = null,
        string? AuthorizationModelId = null,
        IReadOnlyDictionary<string, JsonElement>? Context = null);

    /// <summary>
    /// Response payload containing the list of matching users.
    /// </summary>
    public sealed record ListUsersResponseDto(IReadOnlyList<string> Users);

    /// <summary>
    /// Request payload for listing objects reachable from a user and relation.
    /// </summary>
    public sealed record ListObjectsRequestDto(
        string User,
        string Relation,
        string Type,
        string? Consistency = null,
        IReadOnlyList<ContextualTupleDto>? ContextualTuples = null,
        string? AuthorizationModelId = null,
        IReadOnlyDictionary<string, JsonElement>? Context = null);

    /// <summary>
    /// Response payload containing the list of matching objects.
    /// </summary>
    public sealed record ListObjectsResponseDto(IReadOnlyList<string> Objects);

    /// <summary>
    /// Request payload for expanding a relationship path.
    /// </summary>
    public sealed record ExpandRequestDto(
        string Relation,
        string Object,
        string? Consistency = null,
        IReadOnlyList<ContextualTupleDto>? ContextualTuples = null,
        string? AuthorizationModelId = null,
        IReadOnlyDictionary<string, JsonElement>? Context = null);
}
