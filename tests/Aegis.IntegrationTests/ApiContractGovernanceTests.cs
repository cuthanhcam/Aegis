using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;

namespace Aegis.IntegrationTests;

public sealed class ApiContractGovernanceTests : IClassFixture<TestApiFactory>
{
    private const string NativeApiPrefix = "api/v1/";
    private readonly TestApiFactory _factory;

    public ApiContractGovernanceTests(TestApiFactory factory) => _factory = factory;

    [Fact]
    public void PublicControllerActions_AreExplicitlyVersionedAndUseHttpMethodConstraints()
    {
        var actionProvider = _factory.AppServices.GetRequiredService<IActionDescriptorCollectionProvider>();
        var actions = actionProvider.ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
            .OrderBy(action => action.ControllerName).ThenBy(action => action.ActionName).ToArray();

        Assert.NotEmpty(actions);
        foreach (var action in actions)
        {
            var route = action.AttributeRouteInfo?.Template;
            Assert.True(route?.StartsWith(NativeApiPrefix, StringComparison.OrdinalIgnoreCase) == true,
                $"{action.ControllerName}.{action.ActionName} must use an explicit /api/v1 route; found '{route ?? "<none>"}'.");
            Assert.True(action.ActionConstraints?.OfType<Microsoft.AspNetCore.Mvc.ActionConstraints.HttpMethodActionConstraint>().Any() == true,
                $"{action.ControllerName}.{action.ActionName} must declare an HTTP method attribute.");
        }
    }

    [Fact]
    public void OpenApiV1_IsResolvableAndCanBeExported()
    {
        var document = _factory.AppServices.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        Assert.Equal("v1", document.Info.Version);
        Assert.Contains(document.Servers, server => server.Url == "/");
        Assert.NotEmpty(document.Paths);
        Assert.All(document.Paths.Keys, path => Assert.True(
            path.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase) || path == "/metrics",
            $"OpenAPI path '{path}' must be versioned or an approved operational endpoint."));
        Assert.Contains("AegisApiError", document.Components.Schemas.Keys);
        Assert.DoesNotContain("ApiError", document.Components.Schemas.Keys);

        var outputPath = Environment.GetEnvironmentVariable("AEGIS_OPENAPI_OUTPUT");
        if (string.IsNullOrWhiteSpace(outputPath)) return;

        var fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        using var stream = File.Create(fullPath);
        using var textWriter = new StreamWriter(stream);
        document.SerializeAsV3(new OpenApiJsonWriter(textWriter));
    }
}
