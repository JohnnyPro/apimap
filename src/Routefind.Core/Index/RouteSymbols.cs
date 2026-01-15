namespace Routefind.Core.Index;

/// <summary>
/// Contains symbol information about the route (controller, action names).
/// </summary>
public sealed class RouteSymbols
{
    /// <summary>
    /// Name of the controller class.
    /// </summary>
    public required string Controller { get; init; }

    /// <summary>
    /// Name of the action method.
    /// </summary>
    public required string Action { get; init; }
}
