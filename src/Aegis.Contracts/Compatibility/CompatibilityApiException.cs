namespace Aegis.Contracts.Compatibility
{
    /// <summary>
    /// Exception used to return OpenFGA-compatible API error metadata.
    /// </summary>
    public sealed class CompatibilityApiException : Exception
    {
        /// <summary>
        /// HTTP status code returned to the client.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// OpenFGA-compatible error code.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Creates a compatibility API exception.
        /// </summary>
        public CompatibilityApiException(int statusCode, string errorCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }
}
