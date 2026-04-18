namespace Aegis.Contracts.Common
{
    /// <summary>
    /// Request payload representing a contextual tuple used during authorization evaluation.
    /// </summary>
    public sealed record ContextualTupleDto(
        string Subject,
        string Relation,
        string Object,
        string Effect = "allow");

    /// <summary>
    /// Trace step returned by query evaluation and explain flows.
    /// </summary>
    public sealed record ExplainTraceStepDto(
        string Step,
        string Result,
        string? Tuple = null);

    /// <summary>
    /// Tree node used to represent expansion results.
    /// </summary>
    public sealed record ExpandNodeDto(
        string Node,
        string Kind,
        IReadOnlyList<string> Users,
        IReadOnlyList<ExpandNodeDto> Children);

    /// <summary>
    /// Generic API response envelope for native endpoints.
    /// </summary>
    public sealed class ApiResponse<T>
    {
        /// <summary>
        /// Indicates whether the operation succeeded.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// The success payload when <see cref="Success" /> is true.
        /// </summary>
        public T? Data { get; init; }

        /// <summary>
        /// The error payload when <see cref="Success" /> is false.
        /// </summary>
        public ApiError? Error { get; init; }

        /// <summary>
        /// Creates a successful response wrapper.
        /// </summary>
        public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };

        /// <summary>
        /// Creates a failed response wrapper.
        /// </summary>
        public static ApiResponse<T> Fail(string code, string message) => new()
        {
            Success = false,
            Error = new ApiError(code, message),
        };
    }

    /// <summary>
    /// Standard API error payload.
    /// </summary>
    public sealed record ApiError(
        string Code,
        string Message);
}
