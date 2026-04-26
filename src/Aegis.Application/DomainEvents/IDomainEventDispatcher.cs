using Aegis.SharedKernel;

namespace Aegis.Application.DomainEvents
{
    public interface IDomainEventDispatcher
    {
        Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
    }
}
