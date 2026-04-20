using System.Text.Json;
using Aegis.Authorization.Core.Models;
using Aegis.Authorization.RBAC;

namespace Aegis.UnitTests.Authorization;

public class RbacPermissionEvaluatorTests
{
    [Fact]
    public async Task HasPermissionAsync_ReturnsFalse_WhenDenyGrantMatches()
    {
        var grants = new List<RbacPermissionGrant>
        {
            new("user", "read", "doc:*", IsDeny: false),
            new("user:alice", "read", "doc:1", IsDeny: true)
        };

        var evaluator = new RbacPermissionEvaluator((_, _, _) => Task.FromResult<IReadOnlyList<RbacPermissionGrant>>(grants));
        var request = BuildRequest("tenant-a", "user:alice", "read", "doc:1");

        var allowed = await evaluator.HasPermissionAsync(request);

        Assert.False(allowed);
    }

    [Fact]
    public async Task HasPermissionAsync_ReturnsTrue_ForTypeAndWildcardMatch()
    {
        var grants = new List<RbacPermissionGrant>
        {
            new("user", "read", "doc:*", IsDeny: false)
        };

        var evaluator = new RbacPermissionEvaluator((_, _, _) => Task.FromResult<IReadOnlyList<RbacPermissionGrant>>(grants));
        var request = BuildRequest("tenant-a", "user:alice", "read", "doc:42");

        var allowed = await evaluator.HasPermissionAsync(request);

        Assert.True(allowed);
    }

    [Fact]
    public async Task HasPermissionAsync_UsesAbacCondition_WhenProvided()
    {
        var grants = new List<RbacPermissionGrant>
        {
            new("user:alice", "read", "doc:1", IsDeny: false, ConditionName: "feature_enabled")
        };

        var evaluator = new RbacPermissionEvaluator((_, _, _) => Task.FromResult<IReadOnlyList<RbacPermissionGrant>>(grants));

        var deniedByCondition = BuildRequest(
            "tenant-a",
            "user:alice",
            "read",
            "doc:1",
            new Dictionary<string, JsonElement>
            {
                ["feature_enabled"] = Json("false")
            });

        var allowedByCondition = BuildRequest(
            "tenant-a",
            "user:alice",
            "read",
            "doc:1",
            new Dictionary<string, JsonElement>
            {
                ["feature_enabled"] = Json("true")
            });

        Assert.False(await evaluator.HasPermissionAsync(deniedByCondition));
        Assert.True(await evaluator.HasPermissionAsync(allowedByCondition));
    }

    private static CheckRequest BuildRequest(
        string tenantId,
        string subject,
        string relation,
        string obj,
        IReadOnlyDictionary<string, JsonElement>? context = null)
    {
        return new CheckRequest(
            TenantId: tenantId,
            Subject: new Subject(subject),
            Relation: relation,
            Object: new ObjectRef(obj),
            Context: context);
    }

    private static JsonElement Json(string raw)
    {
        return JsonDocument.Parse(raw).RootElement.Clone();
    }
}
