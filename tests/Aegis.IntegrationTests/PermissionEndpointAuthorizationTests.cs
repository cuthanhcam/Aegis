using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Aegis.Contracts.Permissions;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aegis.IntegrationTests;

public sealed class PermissionEndpointAuthorizationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Check_without_authentication_returns_401()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/check?tenantId=tenant-a", new CheckRequestDto("user:alice", "viewer", "document:1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Tenant_scoped_endpoint_with_mismatched_claim_returns_403()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, "tenant-b");

        var response = await client.GetAsync("/api/v1/tenants/tenant-a/roles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<RoleDto>>>(JsonOptions);
        Assert.False(payload!.Success);
        Assert.Equal("TENANT_FORBIDDEN", payload.Error!.Code);
    }

    [Fact]
    public async Task Check_with_mismatched_tenant_header_returns_400_contract_error()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, "tenant-a");
        client.DefaultRequestHeaders.Add(TestAuthHandler.RequestTenantHeader, "tenant-b");

        var response = await client.PostAsJsonAsync("/api/v1/check?tenantId=tenant-a", new CheckRequestDto("user:alice", "viewer", "document:1"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<CheckResponseDto>>(JsonOptions);
        Assert.False(payload!.Success);
        Assert.Equal("TENANT_MISMATCH", payload.Error!.Code);
    }

    [Fact]
    public async Task Check_returns_expected_rbac_reason_code_when_permission_matches()
    {
        await using var factory = new TestApiFactory();
        await SeedDirectAllowAsync(factory.AppServices);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, "tenant-a");

        var response = await client.PostAsJsonAsync("/api/v1/check?tenantId=tenant-a", new CheckRequestDto("user:alice", "viewer", "document:123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<CheckResponseDto>>(JsonOptions);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.True(payload.Data!.Allowed);
        Assert.Equal("ALLOW_REBAC_DIRECT", payload.Data.ReasonCode);
    }

    [Fact]
    public async Task Permissions_list_includes_condition_name_in_payload()
    {
        await using var factory = new TestApiFactory();
        await SeedPermissionAsync(factory.AppServices, "tenant-a", "viewer", "document:*", "feature_enabled");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, "tenant-a");

        var response = await client.GetAsync("/api/v1/tenants/tenant-a/permissions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PermissionDto>>>(JsonOptions);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        var permission = Assert.Single(payload.Data!);
        Assert.Equal("viewer", permission.Relation);
        Assert.Equal("document:*", permission.Object);
        Assert.Equal("feature_enabled", permission.ConditionName);
    }

    [Fact]
    public async Task Permissions_get_includes_condition_name_in_payload()
    {
        await using var factory = new TestApiFactory();
        await SeedPermissionAsync(factory.AppServices, "tenant-a", "viewer", "document:*", "feature_enabled");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, "tenant-a");

        var response = await client.GetAsync("/api/v1/tenants/tenant-a/permissions/resolve?relation=viewer&object=document:*");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PermissionDto>>(JsonOptions);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.Equal("viewer", payload.Data!.Relation);
        Assert.Equal("document:*", payload.Data.Object);
        Assert.Equal("feature_enabled", payload.Data.ConditionName);
    }

    private static async Task SeedDirectAllowAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var relationshipStore = scope.ServiceProvider.GetRequiredService<IRelationshipStore>();

        await relationshipStore.UpsertAsync(
            "tenant-a",
            new RelationshipTuple(new Subject("user:alice"), "viewer", new ObjectRef("document:123"), RelationshipEffect.Allow, DateTimeOffset.UtcNow));
    }

    private static async Task SeedPermissionAsync(IServiceProvider services, string tenantId, string relation, string objectRef, string conditionName)
    {
        using var scope = services.CreateScope();
        var adminStore = scope.ServiceProvider.GetRequiredService<IRbacAdminStore>();
        await adminStore.UpsertPermissionAsync(tenantId, relation, objectRef, conditionName);
    }
}

internal sealed class TestApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "InMemory",
            });
        });
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.PostConfigure<Microsoft.AspNetCore.Authorization.AuthorizationOptions>(options =>
            {
                options.InvokeHandlersAfterFailure = true;
            });
        });
    }

    public IServiceProvider AppServices => Server.Services;
}

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestScheme";
    public const string AuthenticatedHeader = "X-Test-Authenticated";
    public const string TenantHeader = "X-Test-Tenant";
    public const string RequestTenantHeader = "X-Tenant-Id";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AuthenticatedHeader, out var authenticatedValue)
            || !string.Equals(authenticatedValue.ToString(), "true", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user:alice"),
        };

        if (Request.Headers.TryGetValue(TenantHeader, out var tenantValue) && !string.IsNullOrWhiteSpace(tenantValue))
        {
            claims.Add(new Claim("tenant_id", tenantValue.ToString()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
