namespace Aegis.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing a relation token in tuple expressions.
    /// </summary>
    public sealed record RelationName
    {
        public string Value { get; }

        private RelationName(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a <see cref="RelationName"/> or throws when format is invalid.
        /// </summary>
        public static RelationName Create(string value)
        {
            if (!TryCreate(value, out var relationName))
            {
                throw new ArgumentException("Invalid relation name format.", nameof(value));
            }

            return relationName;
        }

        /// <summary>
        /// Validates and creates a relation name using shared naming rules.
        /// </summary>
        public static bool TryCreate(string? value, out RelationName relationName)
        {
            relationName = null!;

            if (!ResourceTypeName.TryValidateName(value, out var normalized))
            {
                return false;
            }

            relationName = new RelationName(normalized);
            return true;
        }

        public override string ToString() => Value;
    }
}
