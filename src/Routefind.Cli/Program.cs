using System.CommandLine;
using System.CommandLine.Parsing;
using Routefind.Cli.Commands;

namespace Routefind.Cli;

public class Program
{
    private static readonly HashSet<string> ReservedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "list", "discover", "rediscover", "search", "help", "--help", "-h", "-?"
    };

    public static async Task<int> Main(string[] args)
    {
        // Check if the first argument looks like a search pattern (not a command)
        // This enables: apimap 'settings/' instead of apimap search 'settings/'
        if (args.Length > 0 && !ReservedCommands.Contains(args[0]) && !args[0].StartsWith('-'))
        {
            // Prepend "search" to make it the default action
            args = ["search", .. args];
        }

        var rootCommand = new RootCommand("apimap - Discover and search HTTP routes from your codebase")
        {
            Name = "apimap"
        };

        // Add search as the primary command (also works as default via args rewriting)
        rootCommand.AddCommand(SearchCommand.Create());

        // Add other commands
        rootCommand.AddCommand(ListCommand.Create());
        rootCommand.AddCommand(DiscoverCommand.Create());
        rootCommand.AddCommand(DiscoverCommand.CreateRediscover());

        return await rootCommand.InvokeAsync(args);
    }
}
