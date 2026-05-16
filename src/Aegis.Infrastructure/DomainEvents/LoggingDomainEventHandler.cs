using Aegis.Application.DomainEvents;
using Aegis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Aegis.Infrastructure.DomainEvents
{
    public sealed class LoggingDomainEventHandler<TDomainEvent> : IDomainEventHandler<TDomainEvent>
        where TDomainEvent : DomainEvent
    {
        private readonly ILogger<LoggingDomainEventHandler<TDomainEvent>> _logger;

        public LoggingDomainEventHandler(ILogger<LoggingDomainEventHandler<TDomainEvent>> logger)
        {
            _logger = logger;
        }

        public Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Handled domain event {EventType} at {OccurredOn}", domainEvent.EventType, domainEvent.OccurredOn);
            return Task.CompletedTask;
        }
    }
}
