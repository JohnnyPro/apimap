namespace Routefind.Core.Index;

/// <summary>
/// The root model for the route index stored in .apimap/index.json.
/// </summary>
public sealed class RouteIndex
{
    /// <summary>
    /// Schema version for forward compatibility.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// ISO-8601 timestamp when the index was generated.
    /// </summary>
    public required string GeneratedAt { get; init; }

    /// <summary>
    /// Project metadata.
    /// </summary>
    public required ProjectInfo Project { get; init; }

    /// <summary>
    /// List of discovered routes.
    /// </summary>
    public required List<RouteDefinition> Routes { get; init; }
}
