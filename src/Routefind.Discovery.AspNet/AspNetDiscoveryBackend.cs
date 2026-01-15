using Routefind.Core.Cache;
using Routefind.Core.Discovery;
using Routefind.Core.Index;
using Routefind.Discovery.AspNet.Roslyn;

namespace Routefind.Discovery.AspNet;

/// <summary>
/// Discovery backend for ASP.NET Web API projects using Roslyn.
/// </summary>
public sealed class AspNetDiscoveryBackend : IDiscoveryBackend
{
    /// <inheritdoc />
    public string Name => "ASP.NET Web API";

    /// <inheritdoc />
    public string Language => "csharp";

    /// <inheritdoc />
    public string Framework => "aspnet";

    /// <inheritdoc />
    public async Task<RouteIndex> DiscoverAsync(DiscoveryContext context, CancellationToken cancellationToken = default)
    {
        var routes = new List<RouteDefinition>();
        var output = context.Output;

        output.WriteLine("Discovering ASP.NET routes...");

        // Find solution or project files
        var solutionFile = FindSolutionFile(context.RepositoryRoot);
        var projectFiles = FindProjectFiles(context.RepositoryRoot);

        if (solutionFile == null && projectFiles.Length == 0)
        {
            output.WriteLine("No .sln or .csproj files found.");
            return CreateIndex(context.RepositoryRoot, routes);
        }

        using var loader = new SolutionLoader();
        var controllerScanner = new ControllerScanner();
        var routeParser = new RouteAttributeParser();

        if (solutionFile != null)
        {
            var solution = await loader.LoadSolutionAsync(solutionFile, output, cancellationToken);

            foreach (var project in solution.Projects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var projectRoutes = await ScanProjectAsync(project, controllerScanner, routeParser, context.RepositoryRoot, output, cancellationToken);
                routes.AddRange(projectRoutes);
            }
        }
        else
        {
            foreach (var projectFile in projectFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var project = await loader.LoadProjectAsync(projectFile, output, cancellationToken);
                var projectRoutes = await ScanProjectAsync(project, controllerScanner, routeParser, context.RepositoryRoot, output, cancellationToken);
                routes.AddRange(projectRoutes);
            }
        }

        output.WriteLine($"Discovered {routes.Count} route(s).");
        return CreateIndex(context.RepositoryRoot, routes);
    }

    private async Task<List<RouteDefinition>> ScanProjectAsync(
        Microsoft.CodeAnalysis.Project project,
        ControllerScanner controllerScanner,
        RouteAttributeParser routeParser,
        string repositoryRoot,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var routes = new List<RouteDefinition>();

        var compilation = await project.GetCompilationAsync(cancellationToken);
        if (compilation == null)
        {
            output.WriteLine($"  Could not compile project: {project.Name}");
            return routes;
        }

        output.WriteLine($"  Scanning: {project.Name}");

        var controllers = controllerScanner.FindControllers(compilation).ToList();
        output.WriteLine($"    Found {controllers.Count} controller(s)");

        foreach (var controller in controllers)
        {
            var location = controller.ClassDeclaration.GetLocation().GetLineSpan();
            var relativePath = Path.GetRelativePath(repositoryRoot, controller.FilePath);

            var controllerRoute = new RouteDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Type = "controller",
                HttpMethod = null,
                Path = controller.RoutePrefix ?? "",
                Source = new SourceLocation
                {
                    File = relativePath,
                    Line = location.StartLinePosition.Line + 1
                },
                Symbols = new RouteSymbols
                {
                    Controller = controller.ClassSymbol.Name,
                    Action = null
                }
            };
            routes.Add(controllerRoute);
        }

        foreach (var controller in controllers)
        {
            var semanticModel = compilation.GetSemanticModel(controller.ClassDeclaration.SyntaxTree);
            var controllerRoutes = routeParser.ParseActions(controller, semanticModel, repositoryRoot).ToList();

            if (controllerRoutes.Count > 0)
            {
                output.WriteLine($"      {controller.ClassSymbol.Name}: {controllerRoutes.Count} route(s)");
            }

            routes.AddRange(controllerRoutes);
        }

        return routes;
    }

    private string? FindSolutionFile(string repositoryRoot)
    {
        var solutionFiles = Directory.GetFiles(repositoryRoot, "*.sln", SearchOption.TopDirectoryOnly);
        return solutionFiles.FirstOrDefault();
    }

    private string[] FindProjectFiles(string repositoryRoot)
    {
        return Directory.GetFiles(repositoryRoot, "*.csproj", SearchOption.AllDirectories);
    }

    private RouteIndex CreateIndex(string repositoryRoot, List<RouteDefinition> routes)
    {
        return new RouteIndex
        {
            Version = IndexStore.SchemaVersion,
            GeneratedAt = DateTime.UtcNow.ToString("o"),
            Project = new ProjectInfo
            {
                Root = repositoryRoot,
                Language = Language,
                Framework = Framework
            },
            Routes = routes
        };
    }
}
