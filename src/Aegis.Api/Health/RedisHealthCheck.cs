using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aegis.Api.Health;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;

    public RedisHealthCheck(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var key = $"health:ready:{Guid.NewGuid():N}";
        var value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture);

        try
        {
            await _cache.SetStringAsync(
                key,
                value,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10) },
                cancellationToken);

            var roundTrip = await _cache.GetStringAsync(key, cancellationToken);
            return string.Equals(roundTrip, value, StringComparison.Ordinal)
                ? HealthCheckResult.Healthy("Redis is reachable.")
                : HealthCheckResult.Unhealthy("Redis did not return the expected value.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unavailable.", ex);
        }
        finally
        {
            try
            {
                await _cache.RemoveAsync(key, CancellationToken.None);
            }
            catch
            {
            }
        }
    }
}
