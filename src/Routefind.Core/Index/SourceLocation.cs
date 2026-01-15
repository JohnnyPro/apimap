namespace Routefind.Core.Index;

/// <summary>
/// Represents the source file location of a route definition.
/// </summary>
public sealed class SourceLocation
{
    /// <summary>
    /// Relative path from repository root to the source file.
    /// </summary>
    public required string File { get; init; }

    /// <summary>
    /// Line number where the action method is defined (1-indexed).
    /// </summary>
    public required int Line { get; init; }
}
