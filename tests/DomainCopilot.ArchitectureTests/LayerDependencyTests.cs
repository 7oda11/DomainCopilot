using System.Xml.Linq;

namespace DomainCopilot.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Domain_has_no_project_dependencies()
    {
        Assert.Empty(GetProjectReferences("src/DomainCopilot.Domain/DomainCopilot.Domain.csproj"));
    }

    [Fact]
    public void Application_depends_only_on_Domain()
    {
        AssertProjectReferences(
            "src/DomainCopilot.Application/DomainCopilot.Application.csproj",
            "DomainCopilot.Domain");
    }

    [Fact]
    public void Infrastructure_depends_on_Application_and_Domain()
    {
        AssertProjectReferences(
            "src/DomainCopilot.Infrastructure/DomainCopilot.Infrastructure.csproj",
            "DomainCopilot.Application",
            "DomainCopilot.Domain");
    }

    [Fact]
    public void Api_is_the_outer_composition_root()
    {
        AssertProjectReferences(
            "src/DomainCopilot.Api/DomainCopilot.Api.csproj",
            "DomainCopilot.Application",
            "DomainCopilot.Infrastructure");
    }

    private static void AssertProjectReferences(string projectPath, params string[] expectedNames)
    {
        var actualNames = GetProjectReferences(projectPath)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(expectedNames.Order(StringComparer.OrdinalIgnoreCase), actualNames);
    }

    private static IReadOnlyList<string> GetProjectReferences(string relativeProjectPath)
    {
        var projectPath = Path.Combine(SolutionRoot, relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
        var project = XDocument.Load(projectPath);

        return project
            .Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension((string?)reference.Attribute("Include")))
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();
    }

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DomainCopilot.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the DomainCopilot solution root.");
    }
}
