using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Common;
using Aegis.Contracts.Relationships;
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
using Npgsql;
using Aegis.Contracts.Compatibility;

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

    [Fact]
    public async Task Store_relationships_list_supports_empty_filters()
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

        var response = await client.GetAsync($"/api/v1/stores/{seed.StoreId}/relationships");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<RelationshipTupleDto>>>(JsonOptions);
        Assert.Single(payload!.Data!);
        Assert.Equal("user:anne", payload.Data![0].Subject);
    }

    [Fact]
    public async Task Store_delete_cascades_operational_state_atomically_and_retains_audit()
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

        await using var factory = new ContainerApiFactory(postgres.GetConnectionString(), redis.GetConnectionString());
        var dataSource = factory.AppServices.GetRequiredService<NpgsqlDataSource>();
        var reconciliation = new Aegis.Infrastructure.Persistence.StoreConstraintReconciliationService(dataSource);
        await using (var legacyConnection = await dataSource.OpenConnectionAsync())
        {
            await using var legacyCommand = legacyConnection.CreateCommand();
            legacyCommand.CommandText = """
                ALTER TABLE relationships DISABLE TRIGGER ALL;
                INSERT INTO relationships
                    (id, tenant_id, store_id, subject, relation, object_ref, effect, created_at, updated_at)
                VALUES
                    (@id, 'legacy-tenant', 'orphan-store', 'user:legacy', 'viewer', 'document:legacy', 'Allow', NOW(), NOW());
                ALTER TABLE relationships ENABLE TRIGGER ALL;
                """;
            legacyCommand.Parameters.AddWithValue("id", Guid.NewGuid());
            await legacyCommand.ExecuteNonQueryAsync();
        }

        var blockedValidation = await reconciliation.AuditAsync(validate: true);
        Assert.Equal(1, blockedValidation.TotalViolations);
        Assert.False(blockedValidation.ValidationCompleted);
        Assert.Contains(
            blockedValidation.Tables,
            x => x.Table == "relationships"
                 && x.ViolationCount == 1
                 && x.Samples.Any(sample => sample.StoreId == "orphan-store"));

        await using (var cleanupConnection = await dataSource.OpenConnectionAsync())
        await using (var cleanupCommand = new NpgsqlCommand(
                         "DELETE FROM relationships WHERE tenant_id = 'legacy-tenant' AND store_id = 'orphan-store';",
                         cleanupConnection))
        {
            await cleanupCommand.ExecuteNonQueryAsync();
        }

        var seed = await SeedAsync(factory.AppServices);

        using (var scope = factory.AppServices.CreateScope())
        {
            var services = scope.ServiceProvider;
            var rbac = services.GetRequiredService<IRbacAdminStore>();
            var assertions = services.GetRequiredService<IAssertionRepository>();
            var runs = services.GetRequiredService<IAssertionRunStore>();
            var audit = services.GetRequiredService<IAuditStore>();
            var deletion = services.GetRequiredService<IStoreDeletionRepository>();

            await rbac.UpsertRoleInStoreAsync(seed.TenantId, seed.StoreId, "viewer", "Viewer");
            await assertions.ReplaceAsync(
                seed.StoreId,
                seed.AuthorizationModelId,
                [new AegisCompatAssertionDto(new AegisCompatTupleKeyDto("user:anne", "viewer", "document:roadmap"), true)]);
            var now = DateTimeOffset.UtcNow;
            await runs.SaveAsync(new AegisAssertionRunRecordDto(
                "run-atomic-delete",
                seed.StoreId,
                seed.AuthorizationModelId,
                1,
                now,
                now,
                new AegisAssertionRunSummaryDto(0, 0, 0),
                []));
            await audit.WriteAsync(new AuditEvent(
                seed.TenantId,
                "check",
                "user:anne",
                "viewer",
                "document:roadmap",
                "Allow",
                "RELATIONSHIP_MATCH",
                now,
                seed.StoreId));

            Assert.False(await deletion.DeleteAsync("another-tenant", seed.StoreId));
            Assert.True(await deletion.DeleteAsync(seed.TenantId, seed.StoreId));
        }

        await using var connection = new NpgsqlConnection(postgres.GetConnectionString());
        await connection.OpenAsync();
        foreach (var table in new[]
                 {
                     "stores", "authorization_models", "relationships", "relationship_changes",
                     "rbac_roles", "assertion_sets", "assertion_run_records",
                 })
        {
            await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {table} WHERE store_id = @store_id;", connection);
            if (table == "stores")
            {
                command.CommandText = "SELECT COUNT(*) FROM stores WHERE id = @store_id;";
            }

            command.Parameters.AddWithValue("store_id", seed.StoreId);
            Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);
        }

        await using var auditCommand = new NpgsqlCommand(
            "SELECT COUNT(*) FROM audit_events WHERE store_id = @store_id;",
            connection);
        auditCommand.Parameters.AddWithValue("store_id", seed.StoreId);
        Assert.Equal(1L, (long)(await auditCommand.ExecuteScalarAsync())!);

        var validated = await reconciliation.AuditAsync(validate: true);
        Assert.Equal(0, validated.TotalViolations);
        Assert.True(validated.ValidationCompleted);
        Assert.All(validated.Tables, table => Assert.True(table.Validated));
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

        var store = await storeRegistry.CreateForTenantAsync(tenantId, "container-store");
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
        private readonly Dictionary<string, string?> _previousEnvironment = new(StringComparer.Ordinal);

        public ContainerApiFactory(string postgresConnectionString, string redisConnectionString)
        {
            _postgresConnectionString = postgresConnectionString;
            _redisConnectionString = redisConnectionString;
            SetEnvironment("Storage__Provider", "Postgres");
            SetEnvironment("Cache__Provider", "Redis");
            SetEnvironment("Cache__DecisionTtlSeconds", "60");
            SetEnvironment("Cache__Redis__Configuration", _redisConnectionString);
            SetEnvironment("ConnectionStrings__Aegis", _postgresConnectionString);
        }

        public IServiceProvider AppServices => Server.Services;

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            foreach (var pair in _previousEnvironment)
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }

        private void SetEnvironment(string key, string value)
        {
            if (!_previousEnvironment.ContainsKey(key))
            {
                _previousEnvironment[key] = Environment.GetEnvironmentVariable(key);
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
