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
}
