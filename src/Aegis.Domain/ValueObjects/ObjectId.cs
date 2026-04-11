namespace Aegis.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing an object reference in tuple format: type:id.
    /// </summary>
    public sealed record ObjectId
    {
        public string Value { get; }

        private ObjectId(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates an <see cref="ObjectId"/> or throws when format is invalid.
        /// </summary>
        public static ObjectId Create(string value)
        {
            if (!TryCreate(value, out var objectId))
            {
                throw new ArgumentException("Invalid object id format. Expected 'type:id'.", nameof(value));
            }

            return objectId;
        }

        /// <summary>
        /// Validates and creates an object identifier in type:id format.
        /// </summary>
        public static bool TryCreate(string? value, out ObjectId objectId)
        {
            objectId = null!;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = value.Trim();
            if (normalized.Contains('#', StringComparison.Ordinal))
            {
                return false;
            }

            var separator = normalized.IndexOf(':');
            if (separator <= 0 || separator == normalized.Length - 1)
            {
                return false;
            }

            var type = normalized[..separator].Trim();
            var id = normalized[(separator + 1)..].Trim();
            if (!ResourceTypeName.TryCreate(type, out _) || string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            objectId = new ObjectId(normalized);
            return true;
        }

        public override string ToString() => Value;
    }
}
