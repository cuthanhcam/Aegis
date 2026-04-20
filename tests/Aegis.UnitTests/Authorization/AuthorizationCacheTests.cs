using System.Text.Json;
using Aegis.Authorization.Caching;
using Aegis.Authorization.Core.Models;

namespace Aegis.UnitTests.Authorization;

public class AuthorizationCacheTests
{
    [Fact]
    public void TryGet_ReturnsTrue_AfterSet()
    {
        var cache = new AuthorizationCache(TimeSpan.FromMinutes(1));
        var request = BuildRequest("tenant-a", "user:alice", "read", "doc:1");
        var expected = new DecisionResult(true, "ALLOW", "ALLOW_RBAC", Array.Empty<TraceStep>());

        cache.Set(request, includeTrace: false, expected);

        var found = cache.TryGet(request, includeTrace: false, out var actual);

        Assert.True(found);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryGet_ReturnsFalse_WhenExpired()
    {
        var cache = new AuthorizationCache(TimeSpan.Zero);
        var request = BuildRequest("tenant-a", "user:alice", "read", "doc:1");

        cache.Set(request, includeTrace: false, new DecisionResult(true, "ALLOW", "ALLOW_RBAC", Array.Empty<TraceStep>()));

        var found = cache.TryGet(request, includeTrace: false, out _);

        Assert.False(found);
    }

    [Fact]
    public void InvalidateTenant_RemovesOnlyTenantEntries()
    {
        var cache = new AuthorizationCache(TimeSpan.FromMinutes(1));

        var t1 = BuildRequest("tenant-a", "user:alice", "read", "doc:1");
        var t2 = BuildRequest("tenant-b", "user:bob", "read", "doc:2");
        var result = new DecisionResult(true, "ALLOW", "ALLOW_RBAC", Array.Empty<TraceStep>());

        cache.Set(t1, includeTrace: false, result);
        cache.Set(t2, includeTrace: false, result);

        var removed = cache.InvalidateTenant("tenant-a");

        Assert.Equal(1, removed);
        Assert.False(cache.TryGet(t1, includeTrace: false, out _));
        Assert.True(cache.TryGet(t2, includeTrace: false, out _));
    }

    private static CheckRequest BuildRequest(
        string tenantId,
        string subject,
        string relation,
        string obj)
    {
        return new CheckRequest(
            TenantId: tenantId,
            Subject: new Subject(subject),
            Relation: relation,
            Object: new ObjectRef(obj),
            ContextualTuples: null,
            Context: new Dictionary<string, JsonElement>
            {
                ["is_owner"] = Json("true")
            });
    }

    private static JsonElement Json(string raw)
    {
        return JsonDocument.Parse(raw).RootElement.Clone();
    }
}
