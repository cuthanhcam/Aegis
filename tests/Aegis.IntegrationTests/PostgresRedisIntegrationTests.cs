using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Common;
using Aegis.Contracts.Permissions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Aegis.IntegrationTests;

public sealed class PostgresRedisIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Store_check_uses_postgres_state_and_redis_cache_invalidation()
    {
        if (!ShouldRunContainerTests())
        {
            return;
        }

        await using var postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("aegis")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await using var redis = new RedisBuilder("redis:7-alpine").Build();

        await postgres.StartAsync();
        await redis.StartAsync();

        await using var factory = new ContainerApiFactory(
            postgres.GetConnectionString(),
            redis.GetConnectionString());

        var seed = await SeedAsync(factory.AppServices);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, seed.TenantId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "authorization_admin");

        var firstCheck = await client.PostAsJsonAsync(
            $"/api/v1/stores/{seed.StoreId}/check",
            new CheckRequestDto("user:anne", "viewer", "document:roadmap", AuthorizationModelId: seed.AuthorizationModelId));

        Assert.Equal(HttpStatusCode.OK, firstCheck.StatusCode);
        var firstPayload = await firstCheck.Content.ReadFromJsonAsync<ApiResponse<CheckResponseDto>>(JsonOptions);
        Assert.True(firstPayload!.Data!.Allowed);

        using (var scope = factory.AppServices.CreateScope())
        {
            var relationshipStore = scope.ServiceProvider.GetRequiredService<IRelationshipStore>();
            var deleted = await relationshipStore.DeleteAsync(
                seed.TenantId,
                new Subject("user:anne"),
                "viewer",
                new ObjectRef("document:roadmap"),
                cancellationToken: default,
                storeId: seed.StoreId);
            Assert.True(deleted);
        }

        var secondCheck = await client.PostAsJsonAsync(
            $"/api/v1/stores/{seed.StoreId}/check",
            new CheckRequestDto("user:anne", "viewer", "document:roadmap", AuthorizationModelId: seed.AuthorizationModelId));

        Assert.Equal(HttpStatusCode.OK, secondCheck.StatusCode);
        var secondPayload = await secondCheck.Content.ReadFromJsonAsync<ApiResponse<CheckResponseDto>>(JsonOptions);
        Assert.False(secondPayload!.Data!.Allowed);
    }

    private static bool ShouldRunContainerTests()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable("AEGIS_RUN_CONTAINER_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<(string TenantId, string StoreId, string AuthorizationModelId)> SeedAsync(IServiceProvider services)
    {
        const string tenantId = "tenant-container";
        const string model = """
            type user
            type document
              define viewer: [user]
            """;

        using var scope = services.CreateScope();
        var storeRegistry = scope.ServiceProvider.GetRequiredService<IStoreRegistry>();
        var modelRegistry = scope.ServiceProvider.GetRequiredService<IAuthorizationModelRegistry>();
        var relationshipStore = scope.ServiceProvider.GetRequiredService<IRelationshipStore>();

        var store = await storeRegistry.CreateAsync("container-store");
        var authorizationModel = await modelRegistry.CreateAsync(store.Id, "1.1", model);
        await relationshipStore.UpsertAsync(
            tenantId,
            new RelationshipTuple(new Subject("user:anne"), "viewer", new ObjectRef("document:roadmap"), RelationshipEffect.Allow, DateTimeOffset.UtcNow),
            cancellationToken: default,
            storeId: store.Id);

        return (tenantId, store.Id, authorizationModel.Id);
    }

    private sealed class ContainerApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _postgresConnectionString;
        private readonly string _redisConnectionString;

        public ContainerApiFactory(string postgresConnectionString, string redisConnectionString)
        {
            _postgresConnectionString = postgresConnectionString;
            _redisConnectionString = redisConnectionString;
        }

        public IServiceProvider AppServices => Server.Services;

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:Provider"] = "Postgres",
                    ["Cache:Provider"] = "Redis",
                    ["Cache:DecisionTtlSeconds"] = "60",
                    ["Cache:Redis:Configuration"] = _redisConnectionString,
                    ["ConnectionStrings:Aegis"] = _postgresConnectionString,
                    ["RateLimiting:Auth:PermitLimit"] = "10",
                    ["RateLimiting:Auth:WindowSeconds"] = "60",
                });
            });

            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        }
    }
}
