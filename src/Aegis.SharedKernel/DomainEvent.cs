namespace Aegis.SharedKernel
{
    /// <summary>
    /// Base class for domain events.
    /// Domain events represent significant things that happen in the domain.
    /// </summary>
    public abstract class DomainEvent
    {
        /// <summary>
        /// UTC timestamp indicating when the event was created.
        /// </summary>
        public DateTime OccurredOn { get; protected init; } = DateTime.UtcNow;

        /// <summary>
        /// Event type name, typically used by logging and dispatch pipelines.
        /// </summary>
        public string EventType => GetType().Name;
    }
}
