using System.CommandLine;
using Routefind.Cli.Commands;

namespace Routefind.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("routefind - Discover and list HTTP routes from your codebase");

        // Add commands
        rootCommand.AddCommand(ListCommand.Create());
        rootCommand.AddCommand(DiscoverCommand.Create());
        rootCommand.AddCommand(DiscoverCommand.CreateRediscover());

        return await rootCommand.InvokeAsync(args);
    }
}
