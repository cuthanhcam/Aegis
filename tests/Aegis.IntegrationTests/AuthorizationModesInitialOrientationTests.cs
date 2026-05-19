using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Common;
using Aegis.Contracts.Permissions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Aegis.IntegrationTests;

public sealed class AuthorizationModesInitialOrientationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Initial_orientation_validates_rebac_rbac_and_abac_flows()
    {
        await using var factory = new TestApiFactory();
        await SeedInitialOrientationAsync(factory.AppServices);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, "tenant-a");

        var rebacResponse = await client.PostAsJsonAsync(
            "/api/v1/check?tenantId=tenant-a",
            new CheckRequestDto("user:bob", "viewer", "document:rebac-1"));

        Assert.Equal(HttpStatusCode.OK, rebacResponse.StatusCode);
        var rebacPayload = await rebacResponse.Content.ReadFromJsonAsync<ApiResponse<CheckResponseDto>>(JsonOptions);
        Assert.True(rebacPayload!.Success);
        Assert.True(rebacPayload.Data!.Allowed);
        Assert.Equal("ALLOW_REBAC_DIRECT", rebacPayload.Data.ReasonCode);

        var rbacResponse = await client.PostAsJsonAsync(
            "/api/v1/check?tenantId=tenant-a",
            new CheckRequestDto("user:alice", "viewer", "document:rbac-1"));

        Assert.Equal(HttpStatusCode.OK, rbacResponse.StatusCode);
        var rbacPayload = await rbacResponse.Content.ReadFromJsonAsync<ApiResponse<CheckResponseDto>>(JsonOptions);
        Assert.True(rbacPayload!.Success);
        Assert.True(rbacPayload.Data!.Allowed);
        Assert.Equal("ALLOW_RBAC_ROLE", rbacPayload.Data.ReasonCode);

        var abacDeniedResponse = await client.PostAsJsonAsync(
            "/api/v1/check?tenantId=tenant-a",
            new CheckRequestDto(
                "user:alice",
                "viewer",
                "document:abac-1",
                Context: new Dictionary<string, JsonElement>
                {
                    ["feature_enabled"] = JsonDocument.Parse("false").RootElement.Clone(),
                }));

        Assert.Equal(HttpStatusCode.OK, abacDeniedResponse.StatusCode);
        var abacDeniedPayload = await abacDeniedResponse.Content.ReadFromJsonAsync<ApiResponse<CheckResponseDto>>(JsonOptions);
        Assert.True(abacDeniedPayload!.Success);
        Assert.False(abacDeniedPayload.Data!.Allowed);
        Assert.Equal("DENY_NOT_FOUND", abacDeniedPayload.Data.ReasonCode);

        var abacAllowedResponse = await client.PostAsJsonAsync(
            "/api/v1/check?tenantId=tenant-a",
            new CheckRequestDto(
                "user:alice",
                "viewer",
                "document:abac-1",
                Context: new Dictionary<string, JsonElement>
                {
                    ["feature_enabled"] = JsonDocument.Parse("true").RootElement.Clone(),
                }));

        Assert.Equal(HttpStatusCode.OK, abacAllowedResponse.StatusCode);
        var abacAllowedPayload = await abacAllowedResponse.Content.ReadFromJsonAsync<ApiResponse<CheckResponseDto>>(JsonOptions);
        Assert.True(abacAllowedPayload!.Success);
        Assert.True(abacAllowedPayload.Data!.Allowed);
        Assert.Equal("ALLOW_RBAC_ROLE", abacAllowedPayload.Data.ReasonCode);
    }

    private static async Task SeedInitialOrientationAsync(IServiceProvider services)
    {
        const string model = """
            type document
              define viewer: viewer from parent
            type folder
              define viewer: this
            """;

        using var scope = services.CreateScope();
        var modelRegistry = scope.ServiceProvider.GetRequiredService<IAuthorizationModelRegistry>();
        var relationshipStore = scope.ServiceProvider.GetRequiredService<IRelationshipStore>();
        var rbacAdminStore = scope.ServiceProvider.GetRequiredService<IRbacAdminStore>();

        await modelRegistry.CreateAsync("tenant-a", "1.1", model);

        await relationshipStore.UpsertAsync(
            "tenant-a",
            new RelationshipTuple(new Subject("folder:eng"), "parent", new ObjectRef("document:rebac-1"), RelationshipEffect.Allow, DateTimeOffset.UtcNow));
        await relationshipStore.UpsertAsync(
            "tenant-a",
            new RelationshipTuple(new Subject("user:bob"), "viewer", new ObjectRef("folder:eng"), RelationshipEffect.Allow, DateTimeOffset.UtcNow));

        await rbacAdminStore.CreateUserAsync("tenant-a", "user:alice", null, null);
        await rbacAdminStore.UpsertRoleAsync("tenant-a", "reader", "Default reader role");

        await rbacAdminStore.UpsertPermissionAsync("tenant-a", "viewer", "document:rbac-1");
        await rbacAdminStore.AssignPermissionToRoleAsync("tenant-a", "reader", "viewer", "document:rbac-1");

        await rbacAdminStore.UpsertPermissionAsync("tenant-a", "viewer", "document:abac-1", "feature_enabled");
        await rbacAdminStore.AssignPermissionToRoleAsync("tenant-a", "reader", "viewer", "document:abac-1", "feature_enabled");

        await rbacAdminStore.AssignRoleToUserAsync("tenant-a", "user:alice", "reader");
    }
}
