using Aegis.SharedKernel;

namespace Aegis.Application.DomainEvents
{
    public interface IDomainEventOutboxStore
    {
        Task AppendAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<OutboxMessageEnvelope>> GetPendingAsync(int take, CancellationToken cancellationToken = default);

        Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default);

        Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default);
    }
}
