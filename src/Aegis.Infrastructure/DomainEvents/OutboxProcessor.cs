using Aegis.Application.DomainEvents;

namespace Aegis.Infrastructure.DomainEvents
{
    public sealed class OutboxProcessor : IOutboxProcessor
    {
        private readonly IDomainEventOutboxStore _outboxStore;
        private readonly IOutboxMessagePublisher _publisher;

        public OutboxProcessor(IDomainEventOutboxStore outboxStore, IOutboxMessagePublisher publisher)
        {
            _outboxStore = outboxStore;
            _publisher = publisher;
        }

        public async Task<int> ProcessPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default)
        {
            var pending = await _outboxStore.GetPendingAsync(batchSize, cancellationToken);
            var processed = 0;

            foreach (var message in pending)
            {
                try
                {
                    await _publisher.PublishAsync(message, cancellationToken);
                    await _outboxStore.MarkProcessedAsync(message.Id, cancellationToken);
                    processed += 1;
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    await _outboxStore.MarkFailedAsync(message.Id, ex.Message, cancellationToken);
                }
            }

            return processed;
        }
    }
}
