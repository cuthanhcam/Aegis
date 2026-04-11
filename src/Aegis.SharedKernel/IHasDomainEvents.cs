namespace Aegis.SharedKernel
{
    /// <summary>
    /// Contract for aggregates/entities that collect domain events.
    /// </summary>
    public interface IHasDomainEvents
    {
        /// <summary>
        /// Pending domain events that should be dispatched.
        /// </summary>
        IReadOnlyList<DomainEvent> DomainEvents { get; }

        /// <summary>
        /// Removes all pending events after dispatch.
        /// </summary>
        void ClearDomainEvents();
    }
}
