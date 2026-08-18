using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Aegis.IntegrationTests;

public sealed class ModelLifecycleAssertionEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Model_update_requires_current_strong_entity_tag()
    {
        await using var factory = new TestApiFactory();
        var seed = await SeedPhaseOneAsync(factory.AppServices);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, seed.TenantId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "authorization_admin");

        var resource = $"/api/v1/stores/{seed.StoreId}/authorization-models/{seed.FirstModelId}";
        var get = await client.GetAsync(resource);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("\"1\"", get.Headers.ETag?.Tag);

        var updateBody = new CreateAuthorizationModelRequestDto(
            "1.1",
            "type user\ntype document\n  define viewer: [user]\n  define editor: [user]");
        var missing = await client.PutAsJsonAsync(resource, updateBody);
        Assert.Equal((HttpStatusCode)428, missing.StatusCode);
        var missingPayload = await missing.Content.ReadFromJsonAsync<ApiResponse<string>>(JsonOptions);
        Assert.Equal(NativeErrorCodes.PreconditionRequired, missingPayload!.Error!.Code);

        using var acceptedRequest = new HttpRequestMessage(HttpMethod.Put, resource)
        {
            Content = JsonContent.Create(updateBody),
        };
        acceptedRequest.Headers.IfMatch.Add(new EntityTagHeaderValue("\"1\""));
        var accepted = await client.SendAsync(acceptedRequest);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal("\"2\"", accepted.Headers.ETag?.Tag);

        using var staleRequest = new HttpRequestMessage(HttpMethod.Put, resource)
        {
            Content = JsonContent.Create(updateBody),
        };
        staleRequest.Headers.IfMatch.Add(new EntityTagHeaderValue("\"1\""));
        var stale = await client.SendAsync(staleRequest);
        Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        var stalePayload = await stale.Content.ReadFromJsonAsync<ApiResponse<string>>(JsonOptions);
        Assert.Equal(NativeErrorCodes.ConcurrencyConflict, stalePayload!.Error!.Code);
    }

    [Fact]
    public async Task Model_lifecycle_and_assertion_runner_endpoints_cover_phase_one_flows()
    {
        await using var factory = new TestApiFactory();
        var seed = await SeedPhaseOneAsync(factory.AppServices);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, seed.TenantId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "authorization_admin");

        var publishFirst = await client.PostAsync(
            $"/api/v1/stores/{seed.StoreId}/authorization-models/{seed.FirstModelId}/publish",
            content: null);
        Assert.Equal(HttpStatusCode.OK, publishFirst.StatusCode);
        var firstPublishPayload = await publishFirst.Content.ReadFromJsonAsync<ApiResponse<PublishAuthorizationModelResponseDto>>(JsonOptions);
        Assert.True(firstPublishPayload!.Success);
        Assert.Equal(seed.FirstModelId, firstPublishPayload.Data!.ActiveModelId);

        var publishSecond = await client.PostAsync(
            $"/api/v1/stores/{seed.StoreId}/authorization-models/{seed.SecondModelId}/publish",
            content: null);
        Assert.Equal(HttpStatusCode.OK, publishSecond.StatusCode);
        var secondPublishPayload = await publishSecond.Content.ReadFromJsonAsync<ApiResponse<PublishAuthorizationModelResponseDto>>(JsonOptions);
        Assert.True(secondPublishPayload!.Success);
        Assert.Equal(seed.SecondModelId, secondPublishPayload.Data!.ActiveModelId);

        var rollback = await client.PostAsync(
            $"/api/v1/stores/{seed.StoreId}/authorization-models/{seed.FirstModelId}/rollback",
            content: null);
        Assert.Equal(HttpStatusCode.OK, rollback.StatusCode);
        var rollbackPayload = await rollback.Content.ReadFromJsonAsync<ApiResponse<RollbackAuthorizationModelResponseDto>>(JsonOptions);
        Assert.True(rollbackPayload!.Success);
        Assert.Equal(seed.FirstModelId, rollbackPayload.Data!.ActiveModelId);

        var diff = await client.GetAsync(
            $"/api/v1/stores/{seed.StoreId}/authorization-models/{seed.FirstModelId}/diff/{seed.SecondModelId}");
        Assert.Equal(HttpStatusCode.OK, diff.StatusCode);
        var diffPayload = await diff.Content.ReadFromJsonAsync<ApiResponse<AuthorizationModelDiffDto>>(JsonOptions);
        Assert.True(diffPayload!.Success);
        Assert.Contains(diffPayload.Data!.AddedRelations, x => x.Type == "document" && x.Relation == "editor");

        var writeAssertions = await client.PostAsJsonAsync(
            $"/api/v1/stores/{seed.StoreId}/assertions/{seed.FirstModelId}",
            new AegisCompatWriteAssertionsRequestDto(
            [
                new AegisCompatAssertionDto(new AegisCompatTupleKeyDto("user:anne", "viewer", "document:roadmap"), true),
                new AegisCompatAssertionDto(new AegisCompatTupleKeyDto("user:bob", "viewer", "document:roadmap"), true),
            ]));
        Assert.Equal(HttpStatusCode.OK, writeAssertions.StatusCode);

        var runAssertions = await client.PostAsync(
            $"/api/v1/stores/{seed.StoreId}/assertions/{seed.FirstModelId}/run",
            content: null);
        Assert.Equal(HttpStatusCode.OK, runAssertions.StatusCode);
        var runPayload = await runAssertions.Content.ReadFromJsonAsync<ApiResponse<AegisAssertionRunRecordDto>>(JsonOptions);
        Assert.True(runPayload!.Success);
        Assert.Equal(2, runPayload.Data!.Summary.Total);
        Assert.Equal(1, runPayload.Data.Summary.Passed);
        Assert.Equal(1, runPayload.Data.Summary.Failed);
        Assert.Contains(runPayload.Data.Results, x => x.TupleKey.User == "user:anne" && x.Passed);
        Assert.Contains(runPayload.Data.Results, x => x.TupleKey.User == "user:bob" && !x.Passed);
    }

    private static async Task<(string TenantId, string StoreId, string FirstModelId, string SecondModelId)> SeedPhaseOneAsync(IServiceProvider services)
    {
        const string tenantId = "tenant-a";
        const string firstModel = """
            type user
            type document
              define viewer: [user]
            """;
        const string secondModel = """
            type user
            type document
              define viewer: [user]
              define editor: [user]
            """;

        using var scope = services.CreateScope();
        var storeRegistry = scope.ServiceProvider.GetRequiredService<IStoreRegistry>();
        var modelRegistry = scope.ServiceProvider.GetRequiredService<IAuthorizationModelRegistry>();
        var relationshipStore = scope.ServiceProvider.GetRequiredService<IRelationshipStore>();

        var store = await storeRegistry.CreateForTenantAsync(tenantId, "phase-one-store");
        var first = await modelRegistry.CreateAsync(store.Id, "1.1", firstModel);
        var second = await modelRegistry.CreateAsync(store.Id, "1.1", secondModel);

        await relationshipStore.UpsertAsync(
            tenantId,
            new RelationshipTuple(new Subject("user:anne"), "viewer", new ObjectRef("document:roadmap"), RelationshipEffect.Allow, DateTimeOffset.UtcNow),
            CancellationToken.None,
            storeId: store.Id);

        return (tenantId, store.Id, first.Id, second.Id);
    }
}
