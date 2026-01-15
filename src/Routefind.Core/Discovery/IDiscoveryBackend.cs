using Routefind.Core.Index;

namespace Routefind.Core.Discovery;

/// <summary>
/// Interface for discovery backends that scan codebases for route definitions.
/// Each backend is responsible for a specific language/framework combination.
/// </summary>
public interface IDiscoveryBackend
{
    /// <summary>
    /// Gets the display name of this discovery backend.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the language this backend targets (e.g., "csharp").
    /// </summary>
    string Language { get; }

    /// <summary>
    /// Gets the framework this backend targets (e.g., "aspnet").
    /// </summary>
    string Framework { get; }

    /// <summary>
    /// Discovers all routes in the given context.
    /// </summary>
    /// <param name="context">The discovery context containing repository information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A RouteIndex containing all discovered routes.</returns>
    Task<RouteIndex> DiscoverAsync(DiscoveryContext context, CancellationToken cancellationToken = default);
}
