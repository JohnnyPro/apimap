namespace Routefind.Core.Discovery;

/// <summary>
/// Context passed to discovery backends containing information about the target repository.
/// </summary>
public sealed record DiscoveryContext
{
    /// <summary>
    /// Absolute path to the repository root.
    /// </summary>
    public required string RepositoryRoot { get; init; }

    /// <summary>
    /// Output writer for progress messages.
    /// </summary>
    public required TextWriter Output { get; init; }
}
