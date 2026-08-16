using System.Xml.Linq;

namespace Aegis.UnitTests.Architecture;

public sealed class ProjectDependencyTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedProjectReferences =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Aegis.SharedKernel"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ["Aegis.Domain"] = new HashSet<string>(["Aegis.SharedKernel"], StringComparer.OrdinalIgnoreCase),
            ["Aegis.Authorization"] = new HashSet<string>(["Aegis.Domain", "Aegis.SharedKernel"], StringComparer.OrdinalIgnoreCase),
            ["Aegis.Contracts"] = new HashSet<string>(["Aegis.SharedKernel"], StringComparer.OrdinalIgnoreCase),
            ["Aegis.Application"] = new HashSet<string>(
                ["Aegis.SharedKernel", "Aegis.Domain", "Aegis.Authorization", "Aegis.Contracts"],
                StringComparer.OrdinalIgnoreCase),
            ["Aegis.Infrastructure"] = new HashSet<string>(
                ["Aegis.SharedKernel", "Aegis.Domain", "Aegis.Authorization", "Aegis.Application"],
                StringComparer.OrdinalIgnoreCase),
            ["Aegis.Api"] = new HashSet<string>(
                ["Aegis.SharedKernel", "Aegis.Contracts", "Aegis.Application", "Aegis.Infrastructure"],
                StringComparer.OrdinalIgnoreCase),
        };

    [Fact]
    public void Production_projects_follow_the_approved_dependency_direction()
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectFiles = Directory.GetFiles(
            Path.Combine(repositoryRoot, "src"),
            "*.csproj",
            SearchOption.AllDirectories);

        var violations = new List<string>();

        foreach (var projectFile in projectFiles)
        {
            var projectName = Path.GetFileNameWithoutExtension(projectFile);
            Assert.True(
                AllowedProjectReferences.TryGetValue(projectName, out var allowedReferences),
                $"Production project '{projectName}' is missing from the architecture dependency policy.");

            var document = XDocument.Load(projectFile);
            var references = document
                .Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Path.GetFileNameWithoutExtension(value!));

            foreach (var reference in references)
            {
                if (!allowedReferences!.Contains(reference))
                {
                    violations.Add($"{projectName} must not reference {reference}.");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Aegis.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Aegis repository root from the test output directory.");
    }
}
