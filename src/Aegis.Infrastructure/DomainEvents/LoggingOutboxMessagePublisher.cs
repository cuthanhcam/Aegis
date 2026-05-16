using Aegis.Application.DomainEvents;
using Microsoft.Extensions.Logging;

namespace Aegis.Infrastructure.DomainEvents
{
    public sealed class LoggingOutboxMessagePublisher : IOutboxMessagePublisher
    {
        private readonly ILogger<LoggingOutboxMessagePublisher> _logger;

        public LoggingOutboxMessagePublisher(ILogger<LoggingOutboxMessagePublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync(OutboxMessageEnvelope message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Published outbox event {EventType} with id {Id}", message.EventType, message.Id);
            return Task.CompletedTask;
        }
    }
}
