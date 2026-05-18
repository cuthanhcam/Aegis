using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Aegis.Contracts.Permissions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Testcontainers.PostgreSql;

namespace Aegis.IntegrationTests;

public sealed class LivePostgresPermissionHarnessTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private string? _connectionString;

    private static readonly JsonSerializerOptions TestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Check_enforces_abac_condition_from_live_postgres_grant()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return;
        }

        await using var factory = new PostgresApiFactory(_connectionString);
        await SeedConditionalGrantAsync(factory.AppServices);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, "tenant-live");

        var allowedResponse = await client.PostAsJsonAsync(
            "/api/v1/check?tenantId=tenant-live",
            new CheckRequestDto(
                "user:alice",
                "viewer",
                "document:123",
                Context: new Dictionary<string, JsonElement>
                {
                    ["feature_enabled"] = JsonDocument.Parse("true").RootElement.Clone(),
                }));

        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        var allowedPayload = await allowedResponse.Content.ReadFromJsonAsync<ApiResponse<CheckResponseDto>>(TestJsonOptions);
        Assert.True(allowedPayload!.Success);
        Assert.True(allowedPayload.Data!.Allowed);
        Assert.Equal("ALLOW_RBAC_ROLE", allowedPayload.Data.ReasonCode);

        var deniedResponse = await client.PostAsJsonAsync(
            "/api/v1/check?tenantId=tenant-live",
            new CheckRequestDto(
                "user:alice",
                "viewer",
                "document:123",
                Context: new Dictionary<string, JsonElement>
                {
                    ["feature_enabled"] = JsonDocument.Parse("false").RootElement.Clone(),
                }));

        Assert.Equal(HttpStatusCode.OK, deniedResponse.StatusCode);
        var deniedPayload = await deniedResponse.Content.ReadFromJsonAsync<ApiResponse<CheckResponseDto>>(TestJsonOptions);
        Assert.True(deniedPayload!.Success);
        Assert.False(deniedPayload.Data!.Allowed);
        Assert.Equal("DENY_NOT_FOUND", deniedPayload.Data.ReasonCode);
    }

    public async Task InitializeAsync()
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable("AEGIS_TEST_POSTGRES_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            _connectionString = configuredConnectionString;
            return;
        }

        try
        {
            _postgresContainer = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("aegis")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _postgresContainer.StartAsync();
            _connectionString = _postgresContainer.GetConnectionString();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            _connectionString = null;
        }
    }

    public async Task DisposeAsync()
    {
        if (_postgresContainer is not null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    private static async Task SeedConditionalGrantAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var rbac = scope.ServiceProvider.GetRequiredService<IRbacAdminStore>();

        await rbac.CreateUserAsync("tenant-live", "user:alice", null, null);
        await rbac.UpsertRoleAsync("tenant-live", "reader", "Reader role");
        await rbac.UpsertPermissionAsync("tenant-live", "viewer", "document:*", "feature_enabled");
        await rbac.AssignPermissionToRoleAsync("tenant-live", "reader", "viewer", "document:*", "feature_enabled");
        await rbac.AssignRoleToUserAsync("tenant-live", "user:alice", "reader");
    }
}

internal sealed class PostgresApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public PostgresApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Postgres",
                ["ConnectionStrings:Aegis"] = _connectionString,
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

    public IServiceProvider AppServices => Server.Services;
}
