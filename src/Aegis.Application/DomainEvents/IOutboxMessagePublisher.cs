namespace Aegis.Application.DomainEvents
{
    public interface IOutboxMessagePublisher
    {
        Task PublishAsync(OutboxMessageEnvelope message, CancellationToken cancellationToken = default);
    }
}
