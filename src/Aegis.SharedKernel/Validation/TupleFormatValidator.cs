using System.Text.RegularExpressions;

namespace Aegis.SharedKernel.Validation
{
    /// <summary>
    /// Validates tuple reference formats used by the permission model.
    /// </summary>
    public static partial class TupleFormatValidator
    {
        /// <summary>
        /// Validates a resource reference with format: <c>type:id</c>.
        /// </summary>
        public static bool IsValidResourceRef(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) && ResourceRegex().IsMatch(value);
        }

        /// <summary>
        /// Validates a userset reference with format: <c>type:id#relation</c>.
        /// </summary>
        public static bool IsValidUsersetRef(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) && UsersetRegex().IsMatch(value);
        }

        /// <summary>
        /// Validates subject reference accepted by tuple checks.
        /// A subject may be either a direct resource reference or a userset reference.
        /// </summary>
        public static bool IsValidSubjectRef(string? value)
        {
            return IsValidResourceRef(value) || IsValidUsersetRef(value);
        }

        [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_-]*:[a-zA-Z0-9._-]+$")]
        private static partial Regex ResourceRegex();

        [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_-]*:[a-zA-Z0-9._-]+#[a-zA-Z][a-zA-Z0-9_-]*$")]
        private static partial Regex UsersetRegex();
    }
}
