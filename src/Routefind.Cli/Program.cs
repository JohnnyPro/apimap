using System.CommandLine;
using System.CommandLine.Parsing;
using Routefind.Cli.Commands;

namespace Routefind.Cli;

public class Program
{
    // These are the only words that prevent the auto-injection of "search"
    private static readonly HashSet<string> CommandKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "list", "discover", "rediscover", "search", "help", "-h", "--help", "-?"
    };

    public static async Task<int> Main(string[] args)
    {
        // Assume user wants to search unless the other commands are specified
        if (args.Length > 0 && !CommandKeywords.Contains(args[0]))
        {
            args = ["search", .. args];
        }

        var configService = new Configuration.ConfigService();
        var cliConfig = configService.Config;
        
        var rootCommand = new RootCommand("apimap - Discover and search HTTP routes")
        {
            Name = "apimap"
        };

        rootCommand.AddCommand(SearchCommand.Create(cliConfig));
        rootCommand.AddCommand(ListCommand.Create());
        rootCommand.AddCommand(DiscoverCommand.Create());
        rootCommand.AddCommand(DiscoverCommand.CreateRediscover());

        return await rootCommand.InvokeAsync(args);
    }
}