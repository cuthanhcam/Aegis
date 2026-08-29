namespace Aegis.Infrastructure.DomainEvents;

public sealed record OutboxWorkerOptions(
    int BatchSize,
    TimeSpan PollInterval,
    TimeSpan InitialRetryDelay,
    TimeSpan MaximumRetryDelay);
