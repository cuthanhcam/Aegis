using Aegis.Authorization.Core.Models;
using Aegis.Infrastructure.Authorization;

namespace Aegis.UnitTests.Authorization;

public class InMemoryRbacStoreTests
{
    [Fact]
    public async Task HasPermissionAsync_AllowsExactPermissionMatch()
    {
        var store = new InMemoryRbacStore();
        await store.UpsertRoleAsync("tenant-a", "editor", "Editor role");
        await store.AssignPermissionToRoleAsync("tenant-a", "editor", "viewer", "document:public");
        await store.AssignRoleToUserAsync("tenant-a", "user:alice", "editor");

        var request = new CheckRequest(
            TenantId: "tenant-a",
            Subject: new Subject("user:alice"),
            Relation: "viewer",
            Object: new ObjectRef("document:public"));

        var allowed = await store.HasPermissionAsync(request, CancellationToken.None);

        Assert.True(allowed);
    }

    [Fact]
    public async Task HasPermissionAsync_AllowsTypeWildcardObjectPattern()
    {
        var store = new InMemoryRbacStore();
        await store.UpsertRoleAsync("tenant-a", "editor", "Editor role");
        await store.AssignPermissionToRoleAsync("tenant-a", "editor", "viewer", "document:*");
        await store.AssignRoleToUserAsync("tenant-a", "user:alice", "editor");

        var request = new CheckRequest(
            TenantId: "tenant-a",
            Subject: new Subject("user:alice"),
            Relation: "viewer",
            Object: new ObjectRef("document:abc"));

        var allowed = await store.HasPermissionAsync(request, CancellationToken.None);

        Assert.True(allowed);
    }

    [Fact]
    public async Task HasPermissionAsync_DeniesWhenRelationDoesNotMatch()
    {
        var store = new InMemoryRbacStore();
        await store.UpsertRoleAsync("tenant-a", "editor", "Editor role");
        await store.AssignPermissionToRoleAsync("tenant-a", "editor", "viewer", "document:*");
        await store.AssignRoleToUserAsync("tenant-a", "user:alice", "editor");

        var request = new CheckRequest(
            TenantId: "tenant-a",
            Subject: new Subject("user:alice"),
            Relation: "owner",
            Object: new ObjectRef("document:abc"));

        var allowed = await store.HasPermissionAsync(request, CancellationToken.None);

        Assert.False(allowed);
    }

    [Fact]
    public async Task HasPermissionAsync_EnforcesTenantIsolation()
    {
        var store = new InMemoryRbacStore();
        await store.UpsertRoleAsync("tenant-a", "editor", "Editor role");
        await store.AssignPermissionToRoleAsync("tenant-a", "editor", "viewer", "document:*");
        await store.AssignRoleToUserAsync("tenant-a", "user:alice", "editor");

        var request = new CheckRequest(
            TenantId: "tenant-b",
            Subject: new Subject("user:alice"),
            Relation: "viewer",
            Object: new ObjectRef("document:abc"));

        var allowed = await store.HasPermissionAsync(request, CancellationToken.None);

        Assert.False(allowed);
    }
}
