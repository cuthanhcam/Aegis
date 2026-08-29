using Aegis.Application.DomainEvents;
using Aegis.SharedKernel;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Aegis.Infrastructure.DomainEvents
{
    public sealed class InMemoryDomainEventOutboxStore : IDomainEventOutboxStore
    {
        private readonly ConcurrentDictionary<Guid, OutboxMessageEnvelope> _messages = new();
        private readonly ConcurrentDictionary<Guid, DateTimeOffset> _nextAttempts = new();
        private readonly TimeSpan _retryDelay;

        public InMemoryDomainEventOutboxStore(TimeSpan? retryDelay = null)
        {
            _retryDelay = retryDelay ?? TimeSpan.FromSeconds(2);
        }

        public Task AppendAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();
            var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
            _messages[id] = new OutboxMessageEnvelope(id, domainEvent.EventType, payload, domainEvent.OccurredOn, DateTimeOffset.UtcNow, 0, null, null);
            _nextAttempts[id] = DateTimeOffset.UtcNow;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OutboxMessageEnvelope>> GetPendingAsync(int take, CancellationToken cancellationToken = default)
        {
            var limit = take <= 0 ? 100 : take;
            IReadOnlyList<OutboxMessageEnvelope> pending = _messages.Values
                .Where(x => x.ProcessedAt is null && _nextAttempts.GetValueOrDefault(x.Id) <= DateTimeOffset.UtcNow)
                .OrderBy(x => x.CreatedAt)
                .Take(limit)
                .ToList();

            return Task.FromResult(pending);
        }

        public Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (_messages.TryGetValue(id, out var current))
            {
                _messages[id] = current with
                {
                    ProcessedAt = DateTimeOffset.UtcNow,
                    LastError = null
                };
                _nextAttempts.TryRemove(id, out _);
            }

            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
        {
            if (_messages.TryGetValue(id, out var current))
            {
                _messages[id] = current with
                {
                    AttemptCount = current.AttemptCount + 1,
                    LastError = string.IsNullOrWhiteSpace(error) ? "unknown_error" : error.Trim()
                };
                _nextAttempts[id] = DateTimeOffset.UtcNow + _retryDelay;
            }

            return Task.CompletedTask;
        }
    }
}
