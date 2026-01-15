using Routefind.Core.Discovery;
using Routefind.Discovery.AspNet;

namespace Routefind.Cli.Prompts;

/// <summary>
/// Prompts the user to select a project type when no index exists.
/// </summary>
public static class ProjectTypePrompt
{
    private static readonly IDiscoveryBackend[] AvailableBackends =
    [
        new AspNetDiscoveryBackend()
    ];

    /// <summary>
    /// Prompts the user to select a project type and returns the corresponding discovery backend.
    /// </summary>
    /// <returns>The selected discovery backend, or null if cancelled.</returns>
    public static IDiscoveryBackend? Prompt()
    {
        Console.WriteLine();
        Console.WriteLine("No route index found.");
        Console.WriteLine("What type of project is this?");
        Console.WriteLine();

        for (int i = 0; i < AvailableBackends.Length; i++)
        {
            Console.WriteLine($"  [{i + 1}] {AvailableBackends[i].Name}");
        }

        Console.WriteLine();
        Console.Write("Select option: ");

        var input = Console.ReadLine()?.Trim();

        if (int.TryParse(input, out var selection) && selection >= 1 && selection <= AvailableBackends.Length)
        {
            return AvailableBackends[selection - 1];
        }

        Console.WriteLine("Invalid selection.");
        return null;
    }

    /// <summary>
    /// Gets the default backend (ASP.NET) without prompting.
    /// </summary>
    public static IDiscoveryBackend GetDefaultBackend() => AvailableBackends[0];

    /// <summary>
    /// Gets the backend by name.
    /// </summary>
    public static IDiscoveryBackend? GetBackendByName(string name)
    {
        return AvailableBackends.FirstOrDefault(b =>
            b.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            b.Framework.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
