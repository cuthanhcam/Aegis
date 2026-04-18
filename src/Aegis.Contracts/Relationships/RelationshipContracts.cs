using System.Text.Json.Serialization;

namespace Aegis.Contracts.Relationships
{
    /// <summary>
    /// Request payload for creating or updating a relationship tuple.
    /// </summary>
    public sealed record RelationshipWriteRequestDto(
        string Subject,
        string Relation,
        string Object,
        string Effect = "allow");

    /// <summary>
    /// Request payload for deleting a relationship tuple.
    /// </summary>
    public sealed record RelationshipDeleteRequestDto(
        string Subject,
        string Relation,
        string Object);

    /// <summary>
    /// Read model representing a stored relationship tuple.
    /// </summary>
    public sealed record RelationshipTupleDto(
        string Subject,
        string Relation,
        string Object,
        string Effect,
        DateTimeOffset CreatedAt);

    /// <summary>
    /// Read model representing one relationship change entry.
    /// </summary>
    public sealed record RelationshipChangeDto(
        string Subject,
        string Relation,
        string Object,
        string Operation,
        DateTimeOffset CreatedAt);

    /// <summary>
    /// Request payload for reading paged relationship changes.
    /// </summary>
    public sealed record ReadChangesRequestDto(
        int? PageSize = null,
        string? PageToken = null,
        string? Type = null);

    /// <summary>
    /// Response payload containing relationship changes and continuation token.
    /// </summary>
    public sealed record ReadChangesResponseDto(
        IReadOnlyList<RelationshipChangeDto> Changes,
        [property: JsonPropertyName("continuation_token")] string? ContinuationToken = null);
}
