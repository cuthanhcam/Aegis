using System.Threading;
using System.Diagnostics;

namespace Aegis.Authorization.Core.Metrics
{
    public sealed class InMemoryAuthorizationMetrics : IAuthorizationMetrics
    {
        private long _memoHits;
        private long _memoMisses;
        private long _parseCacheHits;
        private long _parseCacheMisses;
        private long _dbQueries;
        private long _dbResults;
        private long _checkRequests;
        private long _checkAllowed;
        private long _checkDenied;
        private long _checkErrors;

        public void IncrementMemoHit() => Interlocked.Increment(ref _memoHits);
        public void IncrementMemoMiss() => Interlocked.Increment(ref _memoMisses);
        public void IncrementParseCacheHit() => Interlocked.Increment(ref _parseCacheHits);
        public void IncrementParseCacheMiss() => Interlocked.Increment(ref _parseCacheMisses);
        public void IncrementDbQuery() => Interlocked.Increment(ref _dbQueries);
        public void AddDbResultCount(int count) => Interlocked.Add(ref _dbResults, count);
        public void IncrementCheckRequest() => Interlocked.Increment(ref _checkRequests);
        public void IncrementCheckAllowed() => Interlocked.Increment(ref _checkAllowed);
        public void IncrementCheckDenied() => Interlocked.Increment(ref _checkDenied);
        public void IncrementCheckError() => Interlocked.Increment(ref _checkErrors);

        public MetricsSnapshot Snapshot()
        {
            var snapshot = new MetricsSnapshot(
                Interlocked.Read(ref _memoHits),
                Interlocked.Read(ref _memoMisses),
                Interlocked.Read(ref _parseCacheHits),
                Interlocked.Read(ref _parseCacheMisses),
                Interlocked.Read(ref _dbQueries),
                Interlocked.Read(ref _dbResults),
                Interlocked.Read(ref _checkRequests),
                Interlocked.Read(ref _checkAllowed),
                Interlocked.Read(ref _checkDenied),
                Interlocked.Read(ref _checkErrors));

            // Emit a Diagnostic Activity so Application Insights or other listeners can pick up metrics
            try
            {
                var source = new ActivitySource("Aegis.Authorization.Metrics");
                using var activity = source.StartActivity("AuthorizationMetricsSnapshot", ActivityKind.Internal);
                if (activity is not null)
                {
                    activity.SetTag("memo.hits", snapshot.MemoHits);
                    activity.SetTag("memo.misses", snapshot.MemoMisses);
                    activity.SetTag("parsecache.hits", snapshot.ParseCacheHits);
                    activity.SetTag("parsecache.misses", snapshot.ParseCacheMisses);
                    activity.SetTag("db.queries", snapshot.DbQueries);
                    activity.SetTag("db.results", snapshot.DbResults);
                    activity.SetTag("check.requests", snapshot.CheckRequests);
                    activity.SetTag("check.allowed", snapshot.CheckAllowed);
                    activity.SetTag("check.denied", snapshot.CheckDenied);
                    activity.SetTag("check.errors", snapshot.CheckErrors);
                }
            }
            catch
            {
                // swallow exceptions to avoid affecting authorization logic
            }

            return snapshot;
        }
    }
}
