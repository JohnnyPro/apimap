namespace Routefind.Cli.Search;

using Routefind.Core.Index;

/// <summary>
/// A search result with its match score.
/// </summary>
public sealed record SearchResult(RouteDefinition Route, int Score, string MatchedOn);
