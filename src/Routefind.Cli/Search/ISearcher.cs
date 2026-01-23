namespace Routefind.Cli.Search;

using Routefind.Core.Index;
using Routefind.Cli.Commands; // Added for SearchType

public interface ISearcher
{
    IEnumerable<SearchResult> Search(string query, IEnumerable<RouteDefinition> routes, SearchType searchType, string? methodFilter);
}
