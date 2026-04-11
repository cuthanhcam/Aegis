namespace Aegis.SharedKernel
{
    /// <summary>
    /// Base class for DDD value objects.
    /// Value objects are immutable and identified by their values, not identity.
    /// </summary>
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        /// <summary>
        /// Returns all components that participate in value-based equality.
        /// </summary>
        protected abstract IEnumerable<object?> GetEqualityComponents();

        /// <summary>
        /// Compares value objects by all declared equality components.
        /// </summary>
        public virtual bool Equals(ValueObject? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null || other.GetType() != GetType())
            {
                return false;
            }

            using var thisComponents = GetEqualityComponents().GetEnumerator();
            using var otherComponents = other.GetEqualityComponents().GetEnumerator();

            while (true)
            {
                var hasThis = thisComponents.MoveNext();
                var hasOther = otherComponents.MoveNext();

                if (!hasThis && !hasOther)
                {
                    return true;
                }

                if (hasThis != hasOther)
                {
                    return false;
                }

                if (!Equals(thisComponents.Current, otherComponents.Current))
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Object-level equality that delegates to value-object comparison.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is ValueObject other && Equals(other);
        }

        /// <summary>
        /// Computes hash code using the same components used for equality.
        /// </summary>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            foreach (var component in GetEqualityComponents())
            {
                hashCode.Add(component);
            }

            return hashCode.ToHashCode();
        }
    }
}
