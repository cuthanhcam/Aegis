namespace Aegis.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing a resource type token used in tuple references.
    /// </summary>
    public sealed record ResourceTypeName
    {
        public string Value { get; }

        private ResourceTypeName(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a <see cref="ResourceTypeName"/> or throws when format is invalid.
        /// </summary>
        public static ResourceTypeName Create(string value)
        {
            if (!TryCreate(value, out var resourceTypeName))
            {
                throw new ArgumentException("Invalid resource type name format.", nameof(value));
            }

            return resourceTypeName;
        }

        /// <summary>
        /// Validates and creates a resource type token.
        /// </summary>
        public static bool TryCreate(string? value, out ResourceTypeName resourceTypeName)
        {
            resourceTypeName = null!;

            if (!TryValidateName(value, out var normalized))
            {
                return false;
            }

            resourceTypeName = new ResourceTypeName(normalized);
            return true;
        }

        public override string ToString() => Value;

        /// <summary>
        /// Shared token validation used by resource type and relation name value objects.
        /// </summary>
        internal static bool TryValidateName(string? value, out string normalized)
        {
            normalized = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var candidate = value.Trim();
            if (!char.IsLetter(candidate[0]))
            {
                return false;
            }

            foreach (var ch in candidate)
            {
                if (!(char.IsLetterOrDigit(ch) || ch is '_' or '-'))
                {
                    return false;
                }
            }

            normalized = candidate;
            return true;
        }
    }
}
