namespace Routefind.Core.Index;

/// <summary>
/// Represents a single discovered HTTP route.
/// </summary>
public sealed class RouteDefinition
{
    /// <summary>
    /// Unique identifier for this route (GUID string).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The type of entry, e.g., "endpoint" or "controller".
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE, PATCH). Null for controllers.
    /// </summary>
    public string? HttpMethod { get; init; }

    /// <summary>
    /// The route path (e.g., "/api/users/{id}").
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Source file location where the route is defined.
    /// </summary>
    public required SourceLocation Source { get; init; }

    /// <summary>
    /// Symbol information (controller and action names).
    /// </summary>
    public required RouteSymbols Symbols { get; init; }
}
