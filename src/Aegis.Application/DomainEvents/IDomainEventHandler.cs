using Aegis.SharedKernel;

namespace Aegis.Application.DomainEvents
{
    public interface IDomainEventHandler<in TDomainEvent>
        where TDomainEvent : DomainEvent
    {
        Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
    }
}
