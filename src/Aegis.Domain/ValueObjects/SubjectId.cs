namespace Aegis.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing a subject reference: type:id or type:id#relation.
    /// </summary>
    public sealed record SubjectId
    {
        public string Value { get; }

        private SubjectId(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a <see cref="SubjectId"/> or throws when format is invalid.
        /// </summary>
        public static SubjectId Create(string value)
        {
            if (!TryCreate(value, out var subjectId))
            {
                throw new ArgumentException("Invalid subject id format. Expected 'type:id' or 'type:id#relation'.", nameof(value));
            }

            return subjectId;
        }

        /// <summary>
        /// Validates and creates a subject identifier in supported tuple formats.
        /// </summary>
        public static bool TryCreate(string? value, out SubjectId subjectId)
        {
            subjectId = null!;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = value.Trim();
            var markerIndex = normalized.IndexOf('#');
            var objectPart = markerIndex >= 0 ? normalized[..markerIndex] : normalized;

            if (!TrySplitTypeAndId(objectPart, out _))
            {
                return false;
            }

            if (markerIndex >= 0)
            {
                var relation = normalized[(markerIndex + 1)..].Trim();
                if (!RelationName.TryCreate(relation, out _))
                {
                    return false;
                }
            }

            subjectId = new SubjectId(normalized);
            return true;
        }

        public override string ToString() => Value;

        private static bool TrySplitTypeAndId(string value, out (string Type, string Id) parts)
        {
            parts = default;

            var separator = value.IndexOf(':');
            if (separator <= 0 || separator == value.Length - 1)
            {
                return false;
            }

            var type = value[..separator].Trim();
            var id = value[(separator + 1)..].Trim();
            if (!ResourceTypeName.TryCreate(type, out _) || string.IsNullOrWhiteSpace(id) || id.Contains('#', StringComparison.Ordinal))
            {
                return false;
            }

            parts = (type, id);
            return true;
        }
    }
}
