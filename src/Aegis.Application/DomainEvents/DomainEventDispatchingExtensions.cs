using Aegis.SharedKernel;

namespace Aegis.Application.DomainEvents
{
    public static class DomainEventDispatchingExtensions
    {
        public static async Task DispatchAndClearAsync(
            this IDomainEventDispatcher? dispatcher,
            IHasDomainEvents aggregate,
            CancellationToken cancellationToken = default)
        {
            if (aggregate.DomainEvents.Count == 0)
            {
                aggregate.ClearDomainEvents();
                return;
            }

            if (dispatcher is not null)
            {
                await dispatcher.DispatchAsync(aggregate.DomainEvents, cancellationToken);
            }

            aggregate.ClearDomainEvents();
        }
    }
}
