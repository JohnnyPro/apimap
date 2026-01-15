namespace Routefind.Cli.Search;

/// <summary>
/// Provides fuzzy search capabilities for route matching.
/// </summary>
public static class FuzzyMatcher
{
    /// <summary>
    /// Checks if the target string fuzzy-matches the search pattern.
    /// Case-insensitive, supports partial matching and simple fuzzy logic.
    /// </summary>
    /// <param name="target">The string to search in.</param>
    /// <param name="pattern">The search pattern.</param>
    /// <returns>A match score (0 = no match, higher = better match).</returns>
    public static int Match(string target, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return 100;
        if (string.IsNullOrEmpty(target)) return 0;

        target = target.ToLowerInvariant();
        pattern = pattern.ToLowerInvariant();

        // Exact match
        if (target == pattern) return 1000;

        // Contains match
        if (target.Contains(pattern)) return 500 + (pattern.Length * 10);

        // Starts with match
        if (target.StartsWith(pattern)) return 400 + (pattern.Length * 10);

        // Ends with match
        if (target.EndsWith(pattern)) return 300 + (pattern.Length * 10);

        // Path segment match (e.g., "users" matches "/api/users/{id}")
        var segments = target.Split('/', '\\', '.').Where(s => !string.IsNullOrEmpty(s)).ToArray();
        foreach (var segment in segments)
        {
            if (segment == pattern) return 250;
            if (segment.Contains(pattern)) return 200 + (pattern.Length * 5);
            if (segment.StartsWith(pattern)) return 150 + (pattern.Length * 5);
        }

        // Subsequence match (all chars in pattern appear in order in target)
        if (IsSubsequence(target, pattern))
        {
            return 100 + (pattern.Length * 2);
        }

        return 0;
    }

    /// <summary>
    /// Checks if pattern is a subsequence of target.
    /// </summary>
    private static bool IsSubsequence(string target, string pattern)
    {
        int patternIndex = 0;
        foreach (var c in target)
        {
            if (patternIndex < pattern.Length && c == pattern[patternIndex])
            {
                patternIndex++;
            }
        }
        return patternIndex == pattern.Length;
    }

    /// <summary>
    /// Matches a controller name.
    /// </summary>
    public static int MatchController(string controllerName, string pattern)
    {
        // Also try without "Controller" suffix
        var shortName = controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
            ? controllerName[..^10]
            : controllerName;

        return Math.Max(Match(controllerName, pattern), Match(shortName, pattern));
    }
}
