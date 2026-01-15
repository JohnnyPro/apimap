using System.Text.Json;
using System.Text.Json.Serialization;
using Routefind.Core.Index;

namespace Routefind.Core.Cache;

/// <summary>
/// Handles loading and saving the route index from/to the .apimap directory.
/// </summary>
public sealed class IndexStore
{
    private const string IndexDirectory = ".apimap";
    private const string IndexFileName = "index.json";
    private const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _repositoryRoot;

    /// <summary>
    /// Creates a new IndexStore for the specified repository root.
    /// </summary>
    /// <param name="repositoryRoot">Absolute path to the repository root.</param>
    public IndexStore(string repositoryRoot)
    {
        _repositoryRoot = repositoryRoot;
    }

    /// <summary>
    /// Gets the full path to the index file.
    /// </summary>
    public string IndexPath => Path.Combine(_repositoryRoot, IndexDirectory, IndexFileName);

    /// <summary>
    /// Checks if the index file exists.
    /// </summary>
    public bool Exists() => File.Exists(IndexPath);

    /// <summary>
    /// Loads the route index from disk.
    /// </summary>
    /// <returns>The loaded RouteIndex, or null if the file doesn't exist.</returns>
    public async Task<RouteIndex?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!Exists())
        {
            return null;
        }

        await using var stream = File.OpenRead(IndexPath);
        return await JsonSerializer.DeserializeAsync<RouteIndex>(stream, JsonOptions, cancellationToken);
    }

    /// <summary>
    /// Saves the route index to disk.
    /// </summary>
    /// <param name="index">The route index to save.</param>
    public async Task SaveAsync(RouteIndex index, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(IndexPath)!;
        Directory.CreateDirectory(directory);

        await using var stream = File.Create(IndexPath);
        await JsonSerializer.SerializeAsync(stream, index, JsonOptions, cancellationToken);
    }

    /// <summary>
    /// Gets the current schema version.
    /// </summary>
    public static int SchemaVersion => CurrentVersion;
}
