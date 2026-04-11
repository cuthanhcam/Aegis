namespace Aegis.SharedKernel
{
    /// <summary>
    /// Base class for DDD aggregate roots.
    /// Aggregates are clusters of entities and value objects.
    /// They have identity and manage invariants within their boundary.
    /// </summary>
    public abstract class AggregateRoot<TId> : Entity<TId>
        , IHasDomainEvents
        where TId : notnull, IEquatable<TId>
    {
        private readonly List<DomainEvent> _domainEvents = new();
        private readonly IReadOnlyList<DomainEvent> _domainEventsView;

        /// <summary>
        /// Raised but not yet dispatched domain events.
        /// </summary>
        public IReadOnlyList<DomainEvent> DomainEvents => _domainEventsView;

        protected AggregateRoot(TId id) : base(id)
        {
            _domainEventsView = _domainEvents.AsReadOnly();
        }

        protected AggregateRoot()
        {
            // For EF Core
            _domainEventsView = _domainEvents.AsReadOnly();
        }

        /// <summary>
        /// Adds a domain event to the aggregate local queue.
        /// </summary>
        protected void RaiseDomainEvent(DomainEvent @event)
        {
            _domainEvents.Add(@event);
        }

        /// <summary>
        /// Clears queued domain events after they are dispatched.
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
