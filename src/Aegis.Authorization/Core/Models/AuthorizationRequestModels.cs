using System.Text.Json;

namespace Aegis.Authorization.Core.Models
{
    /// <summary>
    /// Read consistency preference used by authorization checks.
    /// </summary>
    public enum ConsistencyPreference
    {
        MinimizeLatency,
        HigherConsistency,
    }

    /// <summary>
    /// Canonical authorization check request consumed by the engine.
    /// </summary>
    public sealed record CheckRequest(
        string TenantId,
        Subject Subject,
        string Relation,
        ObjectRef Object,
        IReadOnlyList<RelationshipTuple>? ContextualTuples = null,
        ConsistencyPreference Consistency = ConsistencyPreference.MinimizeLatency,
        string? AuthorizationModelId = null,
        IReadOnlyDictionary<string, JsonElement>? Context = null);

    /// <summary>
    /// One trace step produced during a check evaluation.
    /// </summary>
    public sealed record TraceStep(string Step, string Result, string? Tuple = null);

    /// <summary>
    /// Authorization decision result with optional trace information.
    /// </summary>
    public sealed record DecisionResult(
        bool Allowed,
        string Decision,
        string ReasonCode,
        IReadOnlyList<TraceStep> Trace);
}
