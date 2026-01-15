using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Routefind.Discovery.AspNet.Roslyn;

/// <summary>
/// Loads solutions and projects using Roslyn's MSBuildWorkspace.
/// </summary>
public sealed class SolutionLoader : IDisposable
{
    private static bool _msBuildRegistered;
    private static readonly object _registrationLock = new();

    private MSBuildWorkspace? _workspace;

    /// <summary>
    /// Ensures MSBuild is registered. Must be called before loading any solutions.
    /// </summary>
    public static void EnsureMSBuildRegistered()
    {
        if (_msBuildRegistered) return;

        lock (_registrationLock)
        {
            if (_msBuildRegistered) return;

            // Register the default MSBuild instance
            MSBuildLocator.RegisterDefaults();
            _msBuildRegistered = true;
        }
    }

    /// <summary>
    /// Loads a solution file.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln file.</param>
    /// <param name="output">TextWriter for progress messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded solution.</returns>
    public async Task<Solution> LoadSolutionAsync(
        string solutionPath,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        EnsureMSBuildRegistered();

        _workspace = MSBuildWorkspace.Create();
        _workspace.WorkspaceFailed += (sender, args) =>
        {
            if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            {
                output.WriteLine($"  Warning: {args.Diagnostic.Message}");
            }
        };

        output.WriteLine($"Loading solution: {Path.GetFileName(solutionPath)}");
        var solution = await _workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);

        output.WriteLine($"  Loaded {solution.ProjectIds.Count} project(s)");
        return solution;
    }

    /// <summary>
    /// Loads a single project file.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <param name="output">TextWriter for progress messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded project.</returns>
    public async Task<Project> LoadProjectAsync(
        string projectPath,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        EnsureMSBuildRegistered();

        _workspace = MSBuildWorkspace.Create();
        _workspace.WorkspaceFailed += (sender, args) =>
        {
            if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            {
                output.WriteLine($"  Warning: {args.Diagnostic.Message}");
            }
        };

        output.WriteLine($"Loading project: {Path.GetFileName(projectPath)}");
        var project = await _workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);

        output.WriteLine($"  Loaded project: {project.Name}");
        return project;
    }

    public void Dispose()
    {
        _workspace?.Dispose();
    }
}
