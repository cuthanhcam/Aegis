using System.Text.Json.Serialization;

namespace Aegis.Contracts.Compatibility
{
    /// <summary>
    /// OpenFGA-compatible error payload.
    /// </summary>
    public sealed record AegisCompatErrorResponseDto(
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("message")] string Message);

    /// <summary>
    /// OpenFGA-compatible assertion item.
    /// </summary>
    public sealed record AegisCompatAssertionDto(
        [property: JsonPropertyName("tuple_key")] AegisCompatTupleKeyDto TupleKey,
        [property: JsonPropertyName("expectation")] bool Expectation,
        [property: JsonPropertyName("contextual_tuples")] AegisCompatContextualTuplesDto? ContextualTuples = null);

    /// <summary>
    /// OpenFGA-compatible read assertions response.
    /// </summary>
    public sealed record AegisCompatReadAssertionsResponseDto(
        [property: JsonPropertyName("authorization_model_id")] string AuthorizationModelId,
        [property: JsonPropertyName("assertions")] IReadOnlyList<AegisCompatAssertionDto> Assertions);

    /// <summary>
    /// OpenFGA-compatible write assertions request.
    /// </summary>
    public sealed record AegisCompatWriteAssertionsRequestDto(
        [property: JsonPropertyName("assertions")] IReadOnlyList<AegisCompatAssertionDto> Assertions);

    public sealed record AegisAssertionRunSummaryDto(
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("passed")] int Passed,
        [property: JsonPropertyName("failed")] int Failed);

    public sealed record AegisAssertionRunResultItemDto(
        [property: JsonPropertyName("tuple_key")] AegisCompatTupleKeyDto TupleKey,
        [property: JsonPropertyName("expected")] bool Expected,
        [property: JsonPropertyName("actual")] bool Actual,
        [property: JsonPropertyName("passed")] bool Passed,
        [property: JsonPropertyName("decision")] string Decision,
        [property: JsonPropertyName("reason")] string Reason,
        [property: JsonPropertyName("explain_trace_id")] string? ExplainTraceId);

    public sealed record AegisAssertionRunRecordDto(
        [property: JsonPropertyName("run_id")] string RunId,
        [property: JsonPropertyName("store_id")] string StoreId,
        [property: JsonPropertyName("authorization_model_id")] string AuthorizationModelId,
        [property: JsonPropertyName("started_at")] DateTimeOffset StartedAt,
        [property: JsonPropertyName("completed_at")] DateTimeOffset CompletedAt,
        [property: JsonPropertyName("summary")] AegisAssertionRunSummaryDto Summary,
        [property: JsonPropertyName("results")] IReadOnlyList<AegisAssertionRunResultItemDto> Results);

    public sealed record AegisAssertionRunListResponseDto(
        [property: JsonPropertyName("runs")] IReadOnlyList<AegisAssertionRunRecordDto> Runs);
}
