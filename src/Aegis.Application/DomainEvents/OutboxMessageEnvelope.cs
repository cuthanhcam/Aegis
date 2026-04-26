namespace Aegis.Application.DomainEvents
{
    public sealed record OutboxMessageEnvelope(
        Guid Id,
        string EventType,
        string Payload,
        DateTime OccurredOn,
        DateTimeOffset CreatedAt,
        int AttemptCount,
        string? LastError,
        DateTimeOffset? ProcessedAt);
}
