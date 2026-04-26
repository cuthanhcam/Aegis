namespace Aegis.Application.DomainEvents
{
    public interface IOutboxProcessor
    {
        Task<int> ProcessPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default);
    }
}
