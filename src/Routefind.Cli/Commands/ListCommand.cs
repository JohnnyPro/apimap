using System.CommandLine;
using Routefind.Core.Cache;
using Routefind.Core.Discovery;
using Routefind.Core.Index;
using Routefind.Cli.Prompts;

namespace Routefind.Cli.Commands;

/// <summary>
/// Command to list all discovered routes from the index.
/// </summary>
public static class ListCommand
{
    public static Command Create()
    {
        var command = new Command("list", "List all discovered routes");

        var methodOption = new Option<string?>(
            ["--method", "-m"],
            "Filter by HTTP method (e.g., GET, POST)");

        var pathOption = new Option<string?>(
            ["--path", "-p"],
            "Filter by path (contains search)");

        command.AddOption(methodOption);
        command.AddOption(pathOption);

        command.SetHandler(async (string? method, string? path) =>
        {
            await ExecuteAsync(method, path);
        }, methodOption, pathOption);

        return command;
    }

    private static async Task ExecuteAsync(string? methodFilter, string? pathFilter)
    {
        var repositoryRoot = Environment.CurrentDirectory;
        var indexStore = new IndexStore(repositoryRoot);

        // Check if index exists
        if (!indexStore.Exists())
        {
            // Prompt for project type and run discovery
            var backend = ProjectTypePrompt.Prompt();
            if (backend == null)
            {
                return;
            }

            var context = new DiscoveryContext
            {
                RepositoryRoot = repositoryRoot,
                Output = Console.Out
            };

            Console.WriteLine();
            var index = await backend.DiscoverAsync(context);
            await indexStore.SaveAsync(index);
            Console.WriteLine();

            DisplayRoutes(index, methodFilter, pathFilter);
        }
        else
        {
            var index = await indexStore.LoadAsync();
            if (index == null)
            {
                Console.WriteLine("Error: Could not load index.");
                return;
            }

            DisplayRoutes(index, methodFilter, pathFilter);
        }
    }

    private static void DisplayRoutes(RouteIndex index, string? methodFilter, string? pathFilter)
    {
        var routes = index.Routes.AsEnumerable();

        // Apply filters
        if (!string.IsNullOrEmpty(methodFilter))
        {
            routes = routes.Where(r => r.HttpMethod.Equals(methodFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(pathFilter))
        {
            routes = routes.Where(r => r.Path.Contains(pathFilter, StringComparison.OrdinalIgnoreCase));
        }

        var routeList = routes.OrderBy(r => r.Path).ThenBy(r => r.HttpMethod).ToList();

        if (routeList.Count == 0)
        {
            Console.WriteLine("No routes found.");
            return;
        }

        // Calculate column widths
        var maxMethodWidth = Math.Max(6, routeList.Max(r => r.HttpMethod.Length));
        var maxPathWidth = Math.Max(4, routeList.Max(r => r.Path.Length));

        foreach (var route in routeList)
        {
            var method = route.HttpMethod.PadRight(maxMethodWidth);
            var path = route.Path.PadRight(maxPathWidth);
            var location = $"{route.Source.File}:{route.Source.Line}";

            Console.WriteLine($"{method}  {path}  {location}");
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {routeList.Count} route(s)");
    }
}
