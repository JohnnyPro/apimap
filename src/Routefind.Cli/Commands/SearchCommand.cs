using System.CommandLine;
using System.Diagnostics;
using Routefind.Cli.Configuration;
using Routefind.Cli.Search;
using Routefind.Core.Cache;
using Routefind.Core.Discovery;
using Routefind.Core.Index;
using Routefind.Cli.Prompts;

namespace Routefind.Cli.Commands;

/// <summary>
/// Search result type.
/// </summary>
public enum SearchType
{
    /// <summary>Both controllers and endpoints.</summary>
    All,
    /// <summary>Controllers only.</summary>
    Controller,
    /// <summary>Endpoints only.</summary>
    Endpoint
}

/// <summary>
/// A search result with its match score.
/// </summary>
public sealed record SearchResult(RouteDefinition Route, int Score, string MatchedOn);

/// <summary>
/// Command to search routes with fuzzy matching.
/// </summary>
public static class SearchCommand
{
    public static Command Create(CliConfig config)
    {
        var searchArg = new Argument<string?>("pattern", () => null, "Search pattern for routes (fuzzy, case-insensitive)");

        var typeOption = new Option<string?>(
            ["--type", "-t"],
            "Filter by type: 'c' or 'controller', 'e' or 'endpoint'");

        var methodOption = new Option<string?>(
            ["--method", "-m"],
            "Filter by HTTP method (implies -t e). E.g., GET, POST, PUT, DELETE");
        var limitOption = new Option<int?>(
            ["--limit", "-l"],
            "Limit the number of matched outputted");
        var openOption = new Option<bool>(
            ["--open", "-o"],
            "Open the first search result in the configured default editor.");

        limitOption.AddValidator(result =>
        {
            if (result.Tokens.Count > 0)
            {
                if (!int.TryParse(result.Tokens[0].Value, out var value))
                {
                    result.ErrorMessage = $"'{result.Tokens[0].Value}' is not a valid integer for --limit.";
                }
                else if (value <= 0)
                {
                    result.ErrorMessage = "--limit must be greater than 0.";
                }
            }
        });
        var command = new Command("search", "Search for routes (fuzzy matching)")
        {
            searchArg,
            typeOption,
            methodOption,
            limitOption,
            openOption
        };

        command.SetHandler(async (string? pattern, string? type, string? method, int? limit, bool open) =>
        {
            await ExecuteSearchAsync(pattern, type, method, limit, open, config);
        }, searchArg, typeOption, methodOption, limitOption, openOption);

        return command;
    }

    public static async Task ExecuteSearchAsync(string? pattern, string? typeFilter, string? methodFilter, int? limit, bool open, CliConfig config)
    {
        var repositoryRoot = Environment.CurrentDirectory;
        var indexStore = new IndexStore(repositoryRoot);

        // Ensure index exists
        RouteIndex index;
        if (!indexStore.Exists())
        {
            var backend = ProjectTypePrompt.Prompt();
            if (backend == null) return;

            var context = new DiscoveryContext
            {
                RepositoryRoot = repositoryRoot,
                Output = Console.Out
            };

            Console.WriteLine();
            index = await backend.DiscoverAsync(context);
            await indexStore.SaveAsync(index);
            Console.WriteLine();
        }
        else
        {
            index = await indexStore.LoadAsync() ?? throw new InvalidOperationException("Failed to load index");
        }

        // Parse type filter
        var searchType = ParseSearchType(typeFilter);

        // Method filter implies endpoint type
        if (!string.IsNullOrEmpty(methodFilter))
        {
            searchType = SearchType.Endpoint;
        }

        // Perform search
        var results = Search(index, pattern, searchType, methodFilter);

        if (results.Count == 0)
        {
            Console.WriteLine("No matches found.");
            return;
        }

        var originalResultCount = results.Count;
        if (limit.HasValue)
            results = results.Take(limit.Value).ToList();
        else
        {
            // Smart limiting
            var highestScore = results.Select(x => x.Score).Max();
            results = results.Where(x => x.Score > (double)highestScore * 0.4)
                .Take(10) // Limited by default
                .ToList();
        }

        // Display results
        DisplayResults(results, searchType, originalResultCount);

        if (open)
        {
            OpenInEditor(config, results);
        }
    }

    private static void OpenInEditor(CliConfig config, List<SearchResult> results)
    {
        if (results.Count == 0)
        {
            return; // Nothing to open
        }

        var editor = config.Editor;
        if (string.IsNullOrWhiteSpace(editor.Default))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No default editor configured. Please set `editor.default` in `.apimap/config.json`.");
            Console.WriteLine("For example: { \"editor\": { \"default\": \"code\" } }");
            Console.ResetColor();
            return;
        }

        if (!editor.Commands.TryGetValue(editor.Default, out var editorCommand) || editorCommand == null)
        {
            // Try to use a sensible default if the command is missing for known editors
            editorCommand = editor.Default switch
            {
                "code" => new EditorCommand { Command = "code -g {FILENAME}:{LINENUMBER}", LaunchType = LaunchType.Background },
                "vim" => new EditorCommand { Command = "vim +{LINENUMBER} {FILENAME}", LaunchType = LaunchType.Inline },
                _ => null
            };

            if (editorCommand == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Editor '{editor.Default}' is not configured. Add a command for it in `.apimap/config.json`.");
                Console.WriteLine($"Example: \"commands\": {{ \"{editor.Default}\": {{ \"command\": \"your-editor {{FILENAME}}:{{LINENUMBER}}\", \"launchType\": \"background\" }} }}");
                Console.ResetColor();
                return;
            }
        }

        var filesToOpen = results.Take(editor.OpenFileCount);

        foreach (var result in filesToOpen)
        {
            var source = result.Route.Source;
            var commandString = editorCommand.Command
                .Replace("{FILENAME}", source.File)
                .Replace("{LINENUMBER}", source.Line.ToString());

            try
            {
                using var process = new Process();
                var isWindows = OperatingSystem.IsWindows();
                process.StartInfo.FileName = isWindows ? "cmd" : "/bin/sh";
                process.StartInfo.Arguments = isWindows ? $"/c {commandString}" : $"-c \"{commandString}\"";
                process.StartInfo.UseShellExecute = false;

                if (editorCommand.LaunchType == LaunchType.Inline)
                {
                    // For inline editors like vim, we want them to take over the terminal
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.RedirectStandardError = false;
                    process.StartInfo.CreateNoWindow = false;
                }
                else // Background
                {
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                }

                process.Start();

                if (editorCommand.LaunchType == LaunchType.Inline)
                {
                    process.WaitForExit();
                }
                // We don't wait for background editors to close.
                else
                {
                    Console.WriteLine($"Opening with: {commandString}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to open editor with command: {commandString}");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }
    }

    private static SearchType ParseSearchType(string? typeFilter)
    {
        if (string.IsNullOrEmpty(typeFilter)) return SearchType.All;

        return typeFilter.ToLowerInvariant() switch
        {
            "c" or "controller" => SearchType.Controller,
            "e" or "endpoint" => SearchType.Endpoint,
            _ => SearchType.All
        };
    }

    private static List<SearchResult> Search(RouteIndex index, string? pattern, SearchType searchType, string? methodFilter)
    {
        var results = new List<SearchResult>();

        IEnumerable<RouteDefinition> validRoutes = index.Routes;


        switch (searchType)
        {
            case SearchType.Controller:
                validRoutes = validRoutes.Where(x => x.Type == "controller");
                break;
            case SearchType.Endpoint:
                validRoutes = validRoutes.Where(x => x.Type == "endpoint");
                break;
            default:
            case SearchType.All:
                break;
        }
        foreach (var route in validRoutes)
        {
            // Apply method filter
            if (!string.IsNullOrEmpty(methodFilter) &&
                (route.HttpMethod == null || !route.HttpMethod.Equals(methodFilter, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            int score = 0;
            string matchedOn = "";

            if (string.IsNullOrEmpty(pattern))
            {
                // No pattern = match all
                score = 100;
                matchedOn = route.Path;
            }
            else
            {
                var pathScore = FuzzyMatcher.MatchPath(route.Path, pattern);
                var controllerScore = FuzzyMatcher.MatchController(route.Symbols.Controller, pattern);
                var actionScore = route.Symbols.Action is null ? 0 : FuzzyMatcher.MatchWord(route.Symbols.Action, pattern) / 10;
                switch (searchType)
                {
                    case SearchType.Controller:
                        score = Math.Max(pathScore, controllerScore);
                        matchedOn = pathScore > controllerScore ? route.Path : route.Symbols.Controller;
                        break;
                    case SearchType.Endpoint:
                        score = new List<int>() { pathScore, controllerScore, actionScore }.Max();
                        if (score == pathScore)
                            matchedOn = route.Path;
                        else if (score == controllerScore)
                            matchedOn = route.Symbols.Controller;
                        else
                            matchedOn = route.Symbols.Action ?? "";
                        break;
                    default:
                    case SearchType.All:
                        score = Math.Max(pathScore, controllerScore);
                        matchedOn = pathScore > controllerScore ? route.Path : route.Symbols.Controller;
                        break;
                }
            }

            if (score > 0)
            {
                results.Add(new SearchResult(route, score, matchedOn));
            }
        }

        return results.OrderByDescending(r => r.Score).ThenBy(r => r.Route.Path).ToList();
    }

    private static void DisplayResults(List<SearchResult> results, SearchType searchType, int originalCount)
    {
        if (results.Count == 0)
        {
            Console.WriteLine("No matches found.");
            return;
        }

        var maxMethodWidth = Math.Max(6, results.Where(r => r.Route.HttpMethod != null).Max(r => (int?)r.Route.HttpMethod!.Length) ?? 0);
        var maxPathWidth = Math.Max(4, Math.Min(50, results.Max(r => r.Route.Path.Length)));

        foreach (var result in results)
        {
            var route = result.Route;
            var method = (route.HttpMethod ?? "").PadRight(maxMethodWidth);
            var path = route.Path.Length > 50 ? route.Path[..47] + "..." : route.Path.PadRight(maxPathWidth);
            var location = $"{route.Source.File}:{route.Source.Line}";

            Console.WriteLine($"{method} {path}  {location}");
        }

        Console.WriteLine();
        if (results.Count < originalCount)
        {
            Console.WriteLine($"Showing top {results.Count} of {originalCount} result(s). Use --limit to see more.");
        }
        else
        {
            Console.WriteLine($"Found {results.Count} result(s).");
        }
    }
}
