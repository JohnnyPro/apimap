using System.CommandLine;
using Routefind.Core.Cache;
using Routefind.Core.Discovery;
using Routefind.Cli.Prompts;

namespace Routefind.Cli.Commands;

/// <summary>
/// Command to discover routes and create/update the index.
/// </summary>
public static class DiscoverCommand
{
    public static Command Create()
    {
        var command = new Command("discover", "Discover routes and create/update the index");

        var typeOption = new Option<string?>(
            ["--type", "-t"],
            "Project type (e.g., 'aspnet'). If not specified, will prompt.");

        command.AddOption(typeOption);

        command.SetHandler(async (string? type) =>
        {
            await ExecuteAsync(type);
        }, typeOption);

        return command;
    }

    public static Command CreateRediscover()
    {
        var command = new Command("rediscover", "Alias for discover - forces fresh route discovery");

        var typeOption = new Option<string?>(
            ["--type", "-t"],
            "Project type (e.g., 'aspnet'). If not specified, will prompt.");

        command.AddOption(typeOption);

        command.SetHandler(async (string? type) =>
        {
            await ExecuteAsync(type);
        }, typeOption);

        return command;
    }

    private static async Task ExecuteAsync(string? type)
    {
        var repositoryRoot = Environment.CurrentDirectory;
        var indexStore = new IndexStore(repositoryRoot);

        // Get discovery backend
        IDiscoveryBackend? backend;

        if (!string.IsNullOrEmpty(type))
        {
            backend = ProjectTypePrompt.GetBackendByName(type);
            if (backend == null)
            {
                Console.WriteLine($"Unknown project type: {type}");
                Console.WriteLine("Available types: aspnet");
                return;
            }
        }
        else
        {
            backend = ProjectTypePrompt.Prompt();
            if (backend == null)
            {
                return;
            }
        }

        // Run discovery
        var context = new DiscoveryContext
        {
            RepositoryRoot = repositoryRoot,
            Output = Console.Out
        };

        try
        {
            var index = await backend.DiscoverAsync(context);
            await indexStore.SaveAsync(index);

            Console.WriteLine();
            Console.WriteLine($"Index saved to: {indexStore.IndexPath}");
            Console.WriteLine($"Total routes: {index.Routes.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during discovery: {ex.Message}");
        }
    }
}
