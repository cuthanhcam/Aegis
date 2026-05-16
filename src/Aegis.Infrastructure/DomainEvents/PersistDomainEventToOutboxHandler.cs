using Aegis.Application.DomainEvents;
using Aegis.SharedKernel;

namespace Aegis.Infrastructure.DomainEvents
{
    public sealed class PersistDomainEventToOutboxHandler<TDomainEvent> : IDomainEventHandler<TDomainEvent>
        where TDomainEvent : DomainEvent
    {
        private readonly IDomainEventOutboxStore _outboxStore;

        public PersistDomainEventToOutboxHandler(IDomainEventOutboxStore outboxStore)
        {
            _outboxStore = outboxStore;
        }

        public Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            return _outboxStore.AppendAsync(domainEvent, cancellationToken);
        }
    }
}
