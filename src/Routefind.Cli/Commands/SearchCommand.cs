using System.CommandLine;
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
    public static Command Create()
    {
        var searchArg = new Argument<string?>("pattern", () => null, "Search pattern for routes (fuzzy, case-insensitive)");

        var typeOption = new Option<string?>(
            ["--type", "-t"],
            "Filter by type: 'c' or 'controller', 'e' or 'endpoint'");

        var methodOption = new Option<string?>(
            ["--method", "-m"],
            "Filter by HTTP method (implies -t e). E.g., GET, POST, PUT, DELETE");

        var command = new Command("search", "Search for routes (fuzzy matching)")
        {
            searchArg,
            typeOption,
            methodOption
        };

        command.SetHandler(async (string? pattern, string? type, string? method) =>
        {
            await ExecuteSearchAsync(pattern, type, method);
        }, searchArg, typeOption, methodOption);

        return command;
    }

    public static async Task ExecuteSearchAsync(string? pattern, string? typeFilter, string? methodFilter)
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

        // Display results
        DisplayResults(results, searchType);
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

        foreach (var route in index.Routes)
        {
            // Apply method filter
            if (!string.IsNullOrEmpty(methodFilter) &&
                !route.HttpMethod.Equals(methodFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int score = 0;
            string matchedOn = "";

            if (string.IsNullOrEmpty(pattern))
            {
                // No pattern = match all
                score = 100;
                matchedOn = searchType == SearchType.Controller ? route.Symbols.Controller : route.Path;
            }
            else if (searchType == SearchType.Controller)
            {
                score = FuzzyMatcher.MatchController(route.Symbols.Controller, pattern);
                matchedOn = route.Symbols.Controller;
            }
            else if (searchType == SearchType.Endpoint)
            {
                score = FuzzyMatcher.Match(route.Path, pattern);
                matchedOn = route.Path;
            }
            else
            {
                // Search both path and controller
                var pathScore = FuzzyMatcher.Match(route.Path, pattern);
                var controllerScore = FuzzyMatcher.MatchController(route.Symbols.Controller, pattern);
                var actionScore = FuzzyMatcher.Match(route.Symbols.Action, pattern);

                score = Math.Max(pathScore, Math.Max(controllerScore, actionScore));
                matchedOn = pathScore >= controllerScore ? route.Path : route.Symbols.Controller;
            }

            if (score > 0)
            {
                results.Add(new SearchResult(route, score, matchedOn));
            }
        }

        return results.OrderByDescending(r => r.Score).ThenBy(r => r.Route.Path).ToList();
    }

    private static void DisplayResults(List<SearchResult> results, SearchType searchType)
    {
        if (results.Count == 0)
        {
            Console.WriteLine("No matches found.");
            return;
        }

        if (searchType == SearchType.Controller)
        {
            // Group by controller
            var grouped = results
                .GroupBy(r => r.Route.Symbols.Controller)
                .OrderByDescending(g => g.Max(r => r.Score));

            foreach (var group in grouped)
            {
                var firstRoute = group.First().Route;
                Console.WriteLine($"{group.Key}");
                Console.WriteLine($"  {firstRoute.Source.File}");

                foreach (var result in group.OrderBy(r => r.Route.Source.Line))
                {
                    Console.WriteLine($"    :{result.Route.Source.Line} {result.Route.HttpMethod,-7} {result.Route.Path}");
                }
                Console.WriteLine();
            }
        }
        else
        {
            // Display as endpoint list
            var maxMethodWidth = Math.Max(6, results.Max(r => r.Route.HttpMethod.Length));
            var maxPathWidth = Math.Max(4, Math.Min(50, results.Max(r => r.Route.Path.Length)));

            foreach (var result in results)
            {
                var route = result.Route;
                var method = route.HttpMethod.PadRight(maxMethodWidth);
                var path = route.Path.Length > 50 ? route.Path[..47] + "..." : route.Path.PadRight(maxPathWidth);
                var location = $"{route.Source.File}:{route.Source.Line}";

                Console.WriteLine($"{method}  {path}  {location}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Found {results.Count} result(s)");
    }
}
