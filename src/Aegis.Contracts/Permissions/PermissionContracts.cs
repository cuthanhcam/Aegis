using Aegis.Contracts.Common;
using System.Text.Json;

namespace Aegis.Contracts.Permissions
{
    /// <summary>
    /// Request payload for evaluating a permission check.
    /// </summary>
    public sealed record CheckRequestDto(
        string Subject,
        string Relation,
        string Object,
        IReadOnlyList<ContextualTupleDto>? ContextualTuples = null,
        string? Consistency = null,
        string? AuthorizationModelId = null,
        IReadOnlyDictionary<string, JsonElement>? Context = null);

    /// <summary>
    /// Request payload for evaluating a permission check inside a store context.
    /// </summary>
    public sealed record StoreCheckRequestDto(
        string User,
        string Relation,
        string Object,
        IReadOnlyList<ContextualTupleDto>? ContextualTuples = null,
        string? Consistency = null,
        string? AuthorizationModelId = null,
        IReadOnlyDictionary<string, JsonElement>? Context = null);

    /// <summary>
    /// One item inside a batch permission check request.
    /// </summary>
    public sealed record BatchCheckItemDto(
        string User,
        string Relation,
        string Object,
        string? CorrelationId = null,
        IReadOnlyList<ContextualTupleDto>? ContextualTuples = null,
        string? Consistency = null,
        string? AuthorizationModelId = null,
        IReadOnlyDictionary<string, JsonElement>? Context = null);

    /// <summary>
    /// Batch permission check request payload.
    /// </summary>
    public sealed record BatchCheckRequestDto(IReadOnlyList<BatchCheckItemDto> Items);

    /// <summary>
    /// Result item for a batch permission check.
    /// </summary>
    public sealed record BatchCheckItemResultDto(
        string CorrelationId,
        CheckResponseDto Result);

    /// <summary>
    /// Batch permission check response payload.
    /// </summary>
    public sealed record BatchCheckResponseDto(IReadOnlyList<BatchCheckItemResultDto> Results);

    /// <summary>
    /// Standard permission decision payload.
    /// </summary>
    public sealed record CheckResponseDto(
        bool Allowed,
        string Decision,
        string ReasonCode,
        IReadOnlyList<ExplainTraceStepDto>? Trace = null);
}
