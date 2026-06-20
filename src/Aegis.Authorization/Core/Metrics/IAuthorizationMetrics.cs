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
        void IncrementCheckRequest();
        void IncrementCheckAllowed();
        void IncrementCheckDenied();
        void IncrementCheckError();

        MetricsSnapshot Snapshot();
    }

    public sealed record MetricsSnapshot(
        long MemoHits,
        long MemoMisses,
        long ParseCacheHits,
        long ParseCacheMisses,
        long DbQueries,
        long DbResults,
        long CheckRequests,
        long CheckAllowed,
        long CheckDenied,
        long CheckErrors);
}
