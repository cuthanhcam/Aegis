using System.Globalization;
using System.Text;
using Aegis.Authorization.Core.Metrics;

namespace Aegis.Api.Metrics;

public static class PrometheusMetricsFormatter
{
    public const string ContentType = "text/plain; version=0.0.4; charset=utf-8";

    public static string Format(IAuthorizationMetrics authorizationMetrics)
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var uptimeSeconds = (DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds;
        var auth = authorizationMetrics.Snapshot();

        var builder = new StringBuilder();
        AppendGauge(builder, "aegis_process_uptime_seconds", "Aegis process uptime in seconds.", uptimeSeconds);
        AppendGauge(builder, "aegis_process_working_set_bytes", "Aegis process working set in bytes.", process.WorkingSet64);
        AppendGauge(builder, "aegis_process_thread_count", "Aegis process thread count.", process.Threads.Count);
        AppendGauge(builder, "aegis_dotnet_gc_total_memory_bytes", "Total managed memory observed by GC.GetTotalMemory.", GC.GetTotalMemory(false));

        AppendCounter(builder, "aegis_authorization_memo_hits_total", "Authorization memo cache hits.", auth.MemoHits);
        AppendCounter(builder, "aegis_authorization_memo_misses_total", "Authorization memo cache misses.", auth.MemoMisses);
        AppendCounter(builder, "aegis_authorization_parse_cache_hits_total", "Authorization model parse cache hits.", auth.ParseCacheHits);
        AppendCounter(builder, "aegis_authorization_parse_cache_misses_total", "Authorization model parse cache misses.", auth.ParseCacheMisses);
        AppendCounter(builder, "aegis_authorization_db_queries_total", "Authorization relationship database queries.", auth.DbQueries);
        AppendCounter(builder, "aegis_authorization_db_results_total", "Authorization relationship database rows returned.", auth.DbResults);

        return builder.ToString();
    }

    private static void AppendGauge(StringBuilder builder, string name, string help, double value)
    {
        AppendHeader(builder, name, help, "gauge");
        builder.Append(name).Append(' ').AppendLine(value.ToString("R", CultureInfo.InvariantCulture));
    }

    private static void AppendGauge(StringBuilder builder, string name, string help, long value)
    {
        AppendHeader(builder, name, help, "gauge");
        builder.Append(name).Append(' ').AppendLine(value.ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendCounter(StringBuilder builder, string name, string help, long value)
    {
        AppendHeader(builder, name, help, "counter");
        builder.Append(name).Append(' ').AppendLine(value.ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendHeader(StringBuilder builder, string name, string help, string type)
    {
        builder.Append("# HELP ").Append(name).Append(' ').AppendLine(help);
        builder.Append("# TYPE ").Append(name).Append(' ').AppendLine(type);
    }
}
