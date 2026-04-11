namespace Aegis.SharedKernel
{
    /// <summary>
    /// Base class for DDD entities. Provides identity and equality semantics.
    /// </summary>
    public abstract class Entity<TId> : IEquatable<Entity<TId>>
        where TId : notnull, IEquatable<TId> // Ensure TId is a non-nullable type that implements IEquatable<TId>
    {
        /// <summary>
        /// Unique identifier of the entity.
        /// Equality is based on Id and concrete type.
        /// </summary>
        public TId Id { get; protected init; }

        protected Entity(TId id)
        {
            Id = id;
        }

        /// <summary>
        /// Required by EF Core for materialization.
        /// Id will be populated by the ORM.
        /// </summary>
        protected Entity()
        {
            // For EF Core
            Id = default!;
        }

        /// <summary>
        /// Entities are equal if they have the same Id and same concrete type.
        /// </summary>
        public bool Equals(Entity<TId>? other)
        {
            if (other is null)
                return false;

            if (GetType() != other.GetType())
                return false;

            return Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            return obj is Entity<TId> entity && Equals(entity);
        }

        /// <summary>
        /// Combines type and Id to avoid collisions across different entity types.
        /// </summary>
        public override int GetHashCode()
        {
            //return Id.GetHashCode();
            return HashCode.Combine(GetType(), Id);
        }

        public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        {
            return !(left == right);
        }
    }
}
