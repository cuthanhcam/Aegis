namespace Aegis.SharedKernel.Primitives
{
    /// <summary>
    /// Lightweight success/failure primitive for application and domain operations.
    /// </summary>
    public sealed class Result
    {
        /// <summary>
        /// Indicates whether the operation completed successfully.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Machine-readable error code when <see cref="IsSuccess"/> is <see langword="false"/>.
        /// </summary>
        public string? ErrorCode { get; }

        private Result(bool isSuccess, string? errorCode)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static Result Success() => new(true, null);

        /// <summary>
        /// Creates a failed result with a required error code.
        /// </summary>
        public static Result Fail(string errorCode)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
            {
                throw new ArgumentException("Error code must not be null, empty, or whitespace.", nameof(errorCode));
            }

            return new(false, errorCode);
        }
    }
}
