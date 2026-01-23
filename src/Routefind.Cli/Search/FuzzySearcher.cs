namespace Routefind.Cli.Search;

using System.Collections.Generic;
using System.Linq;
using Routefind.Core.Index;
using Routefind.Cli.Commands; // Added for SearchType

public class FuzzySearcher : ISearcher
{
    public IEnumerable<SearchResult> Search(string query, IEnumerable<RouteDefinition> routes, SearchType searchType, string? methodFilter)
    {
        var results = new List<SearchResult>();

        IEnumerable<RouteDefinition> validRoutes = routes;

        // Apply SearchType filter
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
            // Apply Method filter
            if (!string.IsNullOrEmpty(methodFilter) &&
                (route.HttpMethod == null || !route.HttpMethod.Equals(methodFilter, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            int score = 0;
            string matchedOn = "";

            if (string.IsNullOrEmpty(query))
            {
                // No pattern = match all with a base score
                score = 100;
                matchedOn = route.Path ?? string.Empty;
            }
            else
            {
                var pathScore = FuzzyMatcher.MatchPath(route.Path ?? string.Empty, query);
                var controllerScore = FuzzyMatcher.MatchController(route.Symbols.Controller ?? string.Empty, query);
                var actionScore = route.Symbols.Action is null ? 0 : FuzzyMatcher.MatchWord(route.Symbols.Action, query);
                var methodMatchScore = route.HttpMethod is null ? 0 : FuzzyMatcher.MatchWord(route.HttpMethod, query);


                // This part of the logic is directly copied from the original SearchCommand.Search
                switch (searchType)
                {
                    case SearchType.Controller:
                        score = Math.Max(pathScore, controllerScore);
                        // Prioritize Controller match if present and relevant, otherwise Path
                        matchedOn = pathScore > controllerScore ? route.Path ?? string.Empty : route.Symbols.Controller ?? string.Empty;
                        break;
                    case SearchType.Endpoint:
                        // For endpoints, consider Path, Controller, Action, and Method
                        var scores = new List<int>() { pathScore, controllerScore, actionScore, methodMatchScore };
                        score = scores.Max();

                        if (score == pathScore)
                            matchedOn = route.Path ?? string.Empty;
                        else if (score == controllerScore)
                            matchedOn = route.Symbols.Controller ?? string.Empty;
                        else if (score == actionScore)
                            matchedOn = route.Symbols.Action ?? string.Empty;
                        else
                            matchedOn = route.HttpMethod ?? string.Empty;
                        break;
                    default: // SearchType.All
                        // For all, prioritize Path or Controller
                        score = Math.Max(pathScore, controllerScore);
                        matchedOn = pathScore > controllerScore ? route.Path ?? string.Empty : route.Symbols.Controller ?? string.Empty;
                        break;
                }
            }

            if (score > 0)
            {
                results.Add(new SearchResult(route, score, matchedOn));
            }
        }

        // Order by score (descending) and then by path (ascending) as a tie-breaker
        return results.OrderByDescending(r => r.Score).ThenBy(r => r.Route.Path).ToList();
    }
}