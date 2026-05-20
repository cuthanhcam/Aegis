namespace Aegis.Authorization.Core.Metrics
{
    public interface IAuthorizationMetrics
    {
        void IncrementMemoHit();
        void IncrementMemoMiss();
        void IncrementParseCacheHit();
        void IncrementParseCacheMiss();
        void IncrementDbQuery();
        void AddDbResultCount(int count);

        MetricsSnapshot Snapshot();
    }

    public sealed record MetricsSnapshot(
        long MemoHits,
        long MemoMisses,
        long ParseCacheHits,
        long ParseCacheMisses,
        long DbQueries,
        long DbResults);
}
