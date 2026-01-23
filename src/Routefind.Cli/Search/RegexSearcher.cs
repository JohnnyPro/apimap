namespace Routefind.Cli.Search;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Routefind.Core.Index;
using Routefind.Cli.Commands; // Added for SearchType

public class RegexSearcher : ISearcher
{
    public IEnumerable<SearchResult> Search(string query, IEnumerable<RouteDefinition> routes, SearchType searchType, string? methodFilter)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<SearchResult>();
        }

        var regex = new Regex(query, RegexOptions.IgnoreCase | RegexOptions.Compiled);
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

            string matchedOn = string.Empty;
            int score = 0; // Default score

            if (regex.IsMatch(route.Path ?? string.Empty))
            {
                matchedOn = route.Path ?? string.Empty;
                score = 1000; // High score for a direct regex match
            }
            else if (regex.IsMatch(route.HttpMethod ?? string.Empty)) // Corrected from route.Method
            {
                matchedOn = route.HttpMethod ?? string.Empty; // Corrected from route.Method
                score = 900;
            }
            else if (regex.IsMatch(route.Symbols.Controller ?? string.Empty)) // Corrected from route.Controller
            {
                matchedOn = route.Symbols.Controller ?? string.Empty; // Corrected from route.Controller
                score = 800;
            }
            else if (regex.IsMatch(route.Symbols.Action ?? string.Empty)) // Corrected from route.Action
            {
                matchedOn = route.Symbols.Action ?? string.Empty; // Corrected from route.Action
                score = 700;
            }

            if (score > 0)
            {
                results.Add(new SearchResult(route, score, matchedOn));
            }
        }

        // Order results by score (descending)
        return results.OrderByDescending(r => r.Score).ToList();
    }
}