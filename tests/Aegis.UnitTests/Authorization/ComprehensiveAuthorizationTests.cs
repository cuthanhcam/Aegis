using Aegis.Application.Interfaces;
using Aegis.Application.Services;
using Aegis.Authorization.Core.Engine;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Permissions;
using Aegis.Contracts.Relationships;
using Aegis.Infrastructure.Authorization;
using Aegis.Infrastructure.Persistence;

namespace Aegis.UnitTests.Authorization;

/// <summary>
/// Comprehensive authorization tests aligned with current application contracts.
/// </summary>
public sealed class ComprehensiveAuthorizationFlowTests
{
    private readonly IRelationshipStore _relationshipStore;
    private readonly InMemoryRbacStore _rbacStore;
    private readonly IAuditStore _auditStore;
    private readonly InMemoryStoreRegistry _storeRegistry;

    public ComprehensiveAuthorizationFlowTests()
    {
        _relationshipStore = new InMemoryRelationshipStore();
        _rbacStore = new InMemoryRbacStore();
        _auditStore = new InMemoryAuditStore();
        _storeRegistry = new InMemoryStoreRegistry();
    }

    private IPermissionAppService CreatePermissionService()
    {
        var engine = new AuthorizationEngine(_relationshipStore, _rbacStore);
        return PermissionAppService.CreateForTests(engine, _auditStore, _storeRegistry, _storeRegistry);
    }

    [Fact]
    public async Task Check_WithDirectReBAC_AllowsAccess()
    {
        var service = CreatePermissionService();
        var relationshipService = new RelationshipAppService(_relationshipStore);

        await relationshipService.UpsertAsync(
            "tenant-a",
            new RelationshipWriteRequestDto("user:alice", "viewer", "document:doc1", "allow"),
            CancellationToken.None);

        var result = await service.CheckAsync(
            "tenant-a",
            new CheckRequestDto("user:alice", "viewer", "document:doc1"),
            CancellationToken.None);

        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task Check_WithExplicitDeny_DeniesAccess()
    {
        var service = CreatePermissionService();
        var relationshipService = new RelationshipAppService(_relationshipStore);

        await relationshipService.UpsertAsync(
            "tenant-a",
            new RelationshipWriteRequestDto("user:bob", "viewer", "document:secret", "deny"),
            CancellationToken.None);

        var result = await service.CheckAsync(
            "tenant-a",
            new CheckRequestDto("user:bob", "viewer", "document:secret"),
            CancellationToken.None);

        Assert.False(result.Allowed);
    }

    [Fact]
    public async Task Check_AcrossTenants_EnforcesIsolation()
    {
        var service = CreatePermissionService();
        var relationshipService = new RelationshipAppService(_relationshipStore);

        await relationshipService.UpsertAsync(
            "tenant-a",
            new RelationshipWriteRequestDto("user:alice", "viewer", "document:doc1", "allow"),
            CancellationToken.None);

        await relationshipService.UpsertAsync(
            "tenant-b",
            new RelationshipWriteRequestDto("user:alice", "viewer", "document:doc1", "deny"),
            CancellationToken.None);

        var resultA = await service.CheckAsync("tenant-a", new CheckRequestDto("user:alice", "viewer", "document:doc1"), CancellationToken.None);
        var resultB = await service.CheckAsync("tenant-b", new CheckRequestDto("user:alice", "viewer", "document:doc1"), CancellationToken.None);

        Assert.True(resultA.Allowed);
        Assert.False(resultB.Allowed);
    }

    [Fact]
    public async Task Check_WithRbacFallback_AllowsAccessWhenRoleHasPermission()
    {
        var service = CreatePermissionService();

        await _rbacStore.UpsertRoleAsync("tenant-a", "editor", "Editor role");
        await _rbacStore.UpsertPermissionAsync("tenant-a", "viewer", "document:public");
        await _rbacStore.AssignPermissionToRoleAsync("tenant-a", "editor", "viewer", "document:public");
        await _rbacStore.AssignRoleToUserAsync("tenant-a", "user:dave", "editor");

        var result = await service.CheckAsync(
            "tenant-a",
            new CheckRequestDto("user:dave", "viewer", "document:public"),
            CancellationToken.None);

        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task Audit_AfterChecks_ReturnsEntries()
    {
        var service = CreatePermissionService();

        await service.CheckAsync("tenant-a", new CheckRequestDto("user:alice", "viewer", "document:doc1"), CancellationToken.None);
        await service.CheckAsync("tenant-a", new CheckRequestDto("user:bob", "viewer", "document:doc2"), CancellationToken.None);

        var auditEntries = await service.QueryAuditAsync("tenant-a", action: null, decision: null, CancellationToken.None);

        Assert.True(auditEntries.Count >= 2);
        Assert.All(auditEntries, e => Assert.Equal("check", e.Action));
    }
}

/// <summary>
/// Tests for in-memory RBAC store operations.
/// </summary>
public sealed class RbacProviderTests
{
    private readonly InMemoryRbacStore _rbacStore = new();

    [Fact]
    public async Task UpsertRoleAndListRoles_ReturnsCreatedRoles()
    {
        await _rbacStore.UpsertRoleAsync("tenant-1", "viewer", "Viewer");
        await _rbacStore.UpsertRoleAsync("tenant-1", "editor", "Editor");

        var roles = await _rbacStore.GetRolesAsync("tenant-1");

        Assert.Contains(roles, r => r.Name == "viewer");
        Assert.Contains(roles, r => r.Name == "editor");
    }

    [Fact]
    public async Task CreateUserAndGetUser_ReturnsUser()
    {
        await _rbacStore.CreateUserAsync("tenant-1", "user:henry", "henry@example.com", "Henry", CancellationToken.None);

        var user = await _rbacStore.GetUserAsync("tenant-1", "user:henry", CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal("user:henry", user!.UserId);
    }

    [Fact]
    public async Task AssignRoleToUser_GetUserRolesContainsRole()
    {
        await _rbacStore.UpsertRoleAsync("tenant-1", "admin", "Admin");
        await _rbacStore.AssignRoleToUserAsync("tenant-1", "user:alice", "admin", CancellationToken.None);

        var userRoles = await _rbacStore.GetUserRolesAsync("tenant-1", "user:alice", CancellationToken.None);

        Assert.Contains("admin", userRoles.Roles);
    }
}

/// <summary>
/// Tests for low-level relationship store filtering and mutations.
/// </summary>
public sealed class RelationshipStoreTests
{
    private readonly IRelationshipStore _relationshipStore = new InMemoryRelationshipStore();

    [Fact]
    public async Task QueryAsync_WithSubjectFilter_ReturnsMatchingTuples()
    {
        await _relationshipStore.UpsertAsync("tenant-1", new RelationshipTuple(new Subject("user:alice"), "viewer", new ObjectRef("document:doc1"), RelationshipEffect.Allow, DateTimeOffset.UtcNow), CancellationToken.None);
        await _relationshipStore.UpsertAsync("tenant-1", new RelationshipTuple(new Subject("user:bob"), "viewer", new ObjectRef("document:doc1"), RelationshipEffect.Allow, DateTimeOffset.UtcNow), CancellationToken.None);

        var result = await _relationshipStore.QueryAsync("tenant-1", new Subject("user:alice"), null, null, null, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("user:alice", result[0].Subject.Value);
    }

    [Fact]
    public async Task UpsertAsync_WithSameTuple_UpdatesEffect()
    {
        await _relationshipStore.UpsertAsync("tenant-1", new RelationshipTuple(new Subject("user:alice"), "viewer", new ObjectRef("document:doc1"), RelationshipEffect.Allow, DateTimeOffset.UtcNow), CancellationToken.None);
        await _relationshipStore.UpsertAsync("tenant-1", new RelationshipTuple(new Subject("user:alice"), "viewer", new ObjectRef("document:doc1"), RelationshipEffect.Deny, DateTimeOffset.UtcNow), CancellationToken.None);

        var result = await _relationshipStore.QueryAsync("tenant-1", new Subject("user:alice"), "viewer", new ObjectRef("document:doc1"), null, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(RelationshipEffect.Deny, result[0].Effect);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingTuple_RemovesTuple()
    {
        await _relationshipStore.UpsertAsync("tenant-1", new RelationshipTuple(new Subject("user:alice"), "viewer", new ObjectRef("document:doc1"), RelationshipEffect.Allow, DateTimeOffset.UtcNow), CancellationToken.None);

        var deleted = await _relationshipStore.DeleteAsync("tenant-1", new Subject("user:alice"), "viewer", new ObjectRef("document:doc1"), CancellationToken.None);
        var result = await _relationshipStore.QueryAsync("tenant-1", new Subject("user:alice"), "viewer", new ObjectRef("document:doc1"), null, CancellationToken.None);

        Assert.True(deleted);
        Assert.Empty(result);
    }
}
