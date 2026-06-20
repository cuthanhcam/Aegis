using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Common;
using Aegis.Contracts.Query;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Aegis.IntegrationTests;

public sealed class StoreGraphSuccessPathTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task ListUsers_returns_expected_subjects_for_seeded_relation()
    {
        await using var factory = new TestApiFactory();
        var seed = await SeedGraphDataAsync(factory.AppServices);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, seed.TenantId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "authorization_admin");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/stores/{seed.StoreId}/graph/list-users",
            new ListUsersRequestDto("viewer", "document:roadmap", AuthorizationModelId: seed.AuthorizationModelId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ListUsersResponseDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.Contains("user:anne", payload.Data!.Users);
    }

    [Fact]
    public async Task ListObjects_returns_expected_objects_for_seeded_relation()
    {
        await using var factory = new TestApiFactory();
        var seed = await SeedGraphDataAsync(factory.AppServices);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, seed.TenantId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "authorization_admin");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/stores/{seed.StoreId}/graph/list-objects",
            new ListObjectsRequestDto("user:anne", "viewer", "document", AuthorizationModelId: seed.AuthorizationModelId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ListObjectsResponseDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.Contains("document:roadmap", payload.Data!.Objects);
    }

    [Fact]
    public async Task Expand_returns_non_empty_tree_for_seeded_relation()
    {
        await using var factory = new TestApiFactory();
        var seed = await SeedGraphDataAsync(factory.AppServices);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, seed.TenantId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "authorization_admin");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/stores/{seed.StoreId}/graph/expand",
            new ExpandRequestDto("viewer", "document:roadmap", AuthorizationModelId: seed.AuthorizationModelId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ExpandNodeDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.False(string.IsNullOrWhiteSpace(payload.Data!.Node));
        Assert.False(string.IsNullOrWhiteSpace(payload.Data.Kind));
    }

    [Fact]
    public async Task ListUsers_returns_support_ticket_viewers_for_support_store_shape()
    {
        await using var factory = new TestApiFactory();
        var seed = await SeedSupportGraphDataAsync(factory.AppServices);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, seed.TenantId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "authorization_admin");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/stores/{seed.StoreId}/graph/list-users",
            new ListUsersRequestDto("viewer", "ticket:INC-1001", AuthorizationModelId: seed.AuthorizationModelId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ListUsersResponseDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.Contains("user:agent1", payload.Data!.Users);
    }

    private static async Task<(string TenantId, string StoreId, string AuthorizationModelId)> SeedGraphDataAsync(IServiceProvider services)
    {
        const string tenantId = "tenant-a";
        const string model = """
                        type user
            type document
              define viewer: [user]
            """;

        using var scope = services.CreateScope();
        var storeRegistry = scope.ServiceProvider.GetRequiredService<IStoreRegistry>();
        var modelRegistry = scope.ServiceProvider.GetRequiredService<IAuthorizationModelRegistry>();
        var relationshipStore = scope.ServiceProvider.GetRequiredService<IRelationshipStore>();

        var store = await storeRegistry.CreateForTenantAsync(tenantId, "graph-success-store");
        var authorizationModel = await modelRegistry.CreateAsync(store.Id, "1.1", model);

        await relationshipStore.UpsertAsync(
            tenantId,
            new RelationshipTuple(new Subject("user:anne"), "viewer", new ObjectRef("document:roadmap"), RelationshipEffect.Allow, DateTimeOffset.UtcNow),
            CancellationToken.None,
            storeId: store.Id);

        return (tenantId, store.Id, authorizationModel.Id);
    }

    private static async Task<(string TenantId, string StoreId, string AuthorizationModelId)> SeedSupportGraphDataAsync(IServiceProvider services)
    {
        const string tenantId = "tenant-a";
        const string model = """
            type user
            type queue
              define manager: [user]
              define agent: [user]
              define viewer: [user] or agent or manager
            type ticket
              define assignee: [user]
              define viewer: [user] or assignee
            """;

        using var scope = services.CreateScope();
        var storeRegistry = scope.ServiceProvider.GetRequiredService<IStoreRegistry>();
        var modelRegistry = scope.ServiceProvider.GetRequiredService<IAuthorizationModelRegistry>();
        var relationshipStore = scope.ServiceProvider.GetRequiredService<IRelationshipStore>();

        var store = await storeRegistry.CreateForTenantAsync(tenantId, "support-graph-store");
        var authorizationModel = await modelRegistry.CreateAsync(store.Id, "1.1", model);

        await relationshipStore.UpsertAsync(
            tenantId,
            new RelationshipTuple(new Subject("user:agent1"), "assignee", new ObjectRef("ticket:INC-1001"), RelationshipEffect.Allow, DateTimeOffset.UtcNow),
            CancellationToken.None,
            storeId: store.Id);

        return (tenantId, store.Id, authorizationModel.Id);
    }
}
