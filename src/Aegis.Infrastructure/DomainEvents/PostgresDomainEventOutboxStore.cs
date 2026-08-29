using System.Text.Json;
using Aegis.Application.DomainEvents;
using Aegis.SharedKernel;
using Npgsql;

namespace Aegis.Infrastructure.DomainEvents;

public sealed class PostgresDomainEventOutboxStore : IDomainEventOutboxStore
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly OutboxWorkerOptions _options;

    public PostgresDomainEventOutboxStore(NpgsqlDataSource dataSource, OutboxWorkerOptions options)
    {
        _dataSource = dataSource;
        _options = options;
    }

    public async Task AppendAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
        await using var command = _dataSource.CreateCommand("""
            INSERT INTO outbox_messages
                (id, event_type, payload, occurred_on, created_at, next_attempt_at)
            VALUES
                (@id, @event_type, CAST(@payload AS jsonb), @occurred_on, @created_at, @next_attempt_at);
            """);
        var now = DateTimeOffset.UtcNow;
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("event_type", domainEvent.EventType);
        command.Parameters.AddWithValue("payload", payload);
        command.Parameters.AddWithValue("occurred_on", new DateTimeOffset(DateTime.SpecifyKind(domainEvent.OccurredOn, DateTimeKind.Utc)));
        command.Parameters.AddWithValue("created_at", now);
        command.Parameters.AddWithValue("next_attempt_at", now);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessageEnvelope>> GetPendingAsync(int take, CancellationToken cancellationToken = default)
    {
        var limit = take <= 0 ? _options.BatchSize : take;
        await using var command = _dataSource.CreateCommand("""
            SELECT id, event_type, payload::text, occurred_on, created_at,
                   attempt_count, last_error, processed_at
            FROM outbox_messages
            WHERE processed_at IS NULL
              AND next_attempt_at <= NOW()
            ORDER BY next_attempt_at, created_at, id
            LIMIT @take;
            """);
        command.Parameters.AddWithValue("take", limit);
        var messages = new List<OutboxMessageEnvelope>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            messages.Add(new OutboxMessageEnvelope(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetFieldValue<DateTime>(3),
                reader.GetFieldValue<DateTimeOffset>(4),
                reader.GetInt32(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7)));
        }

        return messages;
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var command = _dataSource.CreateCommand("""
            UPDATE outbox_messages
            SET processed_at = NOW(), last_error = NULL
            WHERE id = @id AND processed_at IS NULL;
            """);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        var safeError = string.IsNullOrWhiteSpace(error) ? "unknown_error" : error.Trim();
        if (safeError.Length > 4000)
        {
            safeError = safeError[..4000];
        }

        await using var command = _dataSource.CreateCommand("""
            UPDATE outbox_messages
            SET attempt_count = attempt_count + 1,
                last_error = @error,
                next_attempt_at = NOW() + make_interval(secs => LEAST(
                    @maximum_retry_seconds,
                    @initial_retry_seconds * POWER(2, LEAST(attempt_count, 16))))
            WHERE id = @id AND processed_at IS NULL;
            """);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("error", safeError);
        command.Parameters.AddWithValue("initial_retry_seconds", _options.InitialRetryDelay.TotalSeconds);
        command.Parameters.AddWithValue("maximum_retry_seconds", _options.MaximumRetryDelay.TotalSeconds);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
