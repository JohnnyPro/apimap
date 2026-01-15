namespace Routefind.Core.Index;

/// <summary>
/// Contains metadata about the indexed project.
/// </summary>
public sealed class ProjectInfo
{
    /// <summary>
    /// Absolute path to the repository root.
    /// </summary>
    public required string Root { get; init; }

    /// <summary>
    /// Programming language (e.g., "csharp").
    /// </summary>
    public required string Language { get; init; }

    /// <summary>
    /// Framework (e.g., "aspnet").
    /// </summary>
    public required string Framework { get; init; }
}
