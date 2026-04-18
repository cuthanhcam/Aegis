using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aegis.Contracts.Compatibility
{
    /// <summary>
    /// OpenFGA-compatible tuple key payload.
    /// </summary>
    public sealed record AegisCompatTupleKeyDto(
        string User,
        string Relation,
        string Object);

    /// <summary>
    /// OpenFGA-compatible read request.
    /// </summary>
    public sealed record AegisCompatReadRequestDto(
        [property: JsonPropertyName("tuple_key")] AegisCompatTupleKeyDto? TupleKey = null,
        [property: JsonPropertyName("page_size")] int? PageSize = null,
        [property: JsonPropertyName("continuation_token")] string? ContinuationToken = null);

    /// <summary>
    /// OpenFGA-compatible tuple item.
    /// </summary>
    public sealed record AegisCompatTupleDto(
        [property: JsonPropertyName("key")] AegisCompatTupleKeyDto Key,
        [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp);

    /// <summary>
    /// OpenFGA-compatible read response.
    /// </summary>
    public sealed record AegisCompatReadResponseDto(
        [property: JsonPropertyName("tuples")] IReadOnlyList<AegisCompatTupleDto> Tuples,
        [property: JsonPropertyName("continuation_token")] string? ContinuationToken);

    /// <summary>
    /// OpenFGA-compatible write operation descriptor.
    /// </summary>
    public sealed record AegisCompatWriteOperationDto(
        [property: JsonPropertyName("tuple_keys")] IReadOnlyList<AegisCompatTupleKeyDto> TupleKeys,
        [property: JsonPropertyName("on_duplicate")] string? OnDuplicate = null,
        [property: JsonPropertyName("on_missing")] string? OnMissing = null);

    /// <summary>
    /// OpenFGA-compatible write request.
    /// </summary>
    public sealed record AegisCompatWriteRequestDto(
        [property: JsonPropertyName("writes")] AegisCompatWriteOperationDto? Writes = null,
        [property: JsonPropertyName("deletes")] AegisCompatWriteOperationDto? Deletes = null);

    /// <summary>
    /// OpenFGA-compatible contextual tuple container.
    /// </summary>
    public sealed record AegisCompatContextualTuplesDto(
        [property: JsonPropertyName("tuple_keys")] IReadOnlyList<AegisCompatTupleKeyDto> TupleKeys);

    /// <summary>
    /// OpenFGA-compatible check request.
    /// </summary>
    public sealed record AegisCompatCheckRequestDto(
        [property: JsonPropertyName("tuple_key")] AegisCompatTupleKeyDto TupleKey,
        [property: JsonPropertyName("contextual_tuples")] AegisCompatContextualTuplesDto? ContextualTuples = null,
        [property: JsonPropertyName("consistency")] string? Consistency = null,
        [property: JsonPropertyName("authorization_model_id")] string? AuthorizationModelId = null,
        [property: JsonPropertyName("context")] IReadOnlyDictionary<string, JsonElement>? Context = null);

    /// <summary>
    /// OpenFGA-compatible check response.
    /// </summary>
    public sealed record AegisCompatCheckResponseDto(
        [property: JsonPropertyName("allowed")] bool Allowed);

    /// <summary>
    /// OpenFGA-compatible batch check item request.
    /// </summary>
    public sealed record AegisCompatBatchCheckItemRequestDto(
        [property: JsonPropertyName("tuple_key")] AegisCompatTupleKeyDto TupleKey,
        [property: JsonPropertyName("correlation_id")] string CorrelationId,
        [property: JsonPropertyName("contextual_tuples")] AegisCompatContextualTuplesDto? ContextualTuples = null,
        [property: JsonPropertyName("consistency")] string? Consistency = null,
        [property: JsonPropertyName("authorization_model_id")] string? AuthorizationModelId = null,
        [property: JsonPropertyName("context")] IReadOnlyDictionary<string, JsonElement>? Context = null);

    /// <summary>
    /// OpenFGA-compatible batch check request.
    /// </summary>
    public sealed record AegisCompatBatchCheckRequestDto(
        [property: JsonPropertyName("checks")] IReadOnlyList<AegisCompatBatchCheckItemRequestDto> Checks,
        [property: JsonPropertyName("authorization_model_id")] string? AuthorizationModelId = null);

    /// <summary>
    /// OpenFGA-compatible batch check result item.
    /// </summary>
    public sealed record AegisCompatBatchCheckResultItemDto(
        [property: JsonPropertyName("correlation_id")] string CorrelationId,
        [property: JsonPropertyName("allowed")] bool? Allowed = null,
        [property: JsonPropertyName("error")] AegisCompatErrorResponseDto? Error = null);

    /// <summary>
    /// OpenFGA-compatible batch check response.
    /// </summary>
    public sealed record AegisCompatBatchCheckResponseDto(
        [property: JsonPropertyName("result")] IReadOnlyList<AegisCompatBatchCheckResultItemDto> Result);
}
