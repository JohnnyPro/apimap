namespace Routefind.Cli.Search;

public static class FuzzyMatcher
{
    private const int BaseExactScore = 1000;
    private const int LastSegmentBonus = 400; // Key to making 'user' at end > 'users' in middle

    /// <summary>
    /// Specialized matcher for URL paths or file paths.
    /// Prioritizes exact matches, suffix matches, and contiguous segment matches.
    /// This is designed to handle path-like queries against path-like targets.
    /// </summary>
    public static int MatchPath(string target, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return 100;
        if (string.IsNullOrEmpty(target)) return 0;

        // Invariant culture and case for robust comparison
        var targetLower = target.ToLowerInvariant();
        var patternLower = pattern.ToLowerInvariant();

        // 1. Highest priority: Exact match
        if (targetLower == patternLower) return 50000;

        // 2. High priority: The entire pattern is a suffix of the target.
        // e.g., pattern "a/b" matches "x/y/a/b" but not "x/a/b/c"
        if (targetLower.EndsWith(patternLower)) return 40000 + pattern.Length;
        
        var targetSegments = targetLower.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        var patternSegments = patternLower.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

        if (patternSegments.Length == 0) return 100;

        // 3. Main logic: Find the best "window" where pattern segments match contiguously in the target
        int bestWindowScore = 0;
        if (targetSegments.Length >= patternSegments.Length)
        {
            for (int i = 0; i <= targetSegments.Length - patternSegments.Length; i++)
            {
                int currentWindowScore = 0;
                bool allSegmentsMatch = true;

                for (int j = 0; j < patternSegments.Length; j++)
                {
                    string pSeg = patternSegments[j];
                    string tSeg = targetSegments[i + j];
                    
                    double similarity = CalculateSimilarity(tSeg, pSeg);
                    
                    if (similarity >= 0.65) // Must be a reasonably good match
                    {
                        // Base score on similarity
                        currentWindowScore += (int)(1000 * similarity);
                        // Recency bonus: matches later in the path are better
                        currentWindowScore += (i + j) * 20; 
                    }
                    else
                    {
                        allSegmentsMatch = false;
                        break;
                    }
                }

                if (allSegmentsMatch)
                {
                    // This window is a valid sequence match.
                    // Bonus if the match is a suffix of the target segments.
                    if (i + patternSegments.Length == targetSegments.Length)
                    {
                        currentWindowScore += 10000;
                    }
                    
                    bestWindowScore = Math.Max(bestWindowScore, currentWindowScore);
                }
            }
        }
        
        if (bestWindowScore > 0) return bestWindowScore;
        
        // 4. Fallback: If no contiguous match, find the best single segment match for the *last* pattern segment.
        // This handles cases where user just types the last part of a URL.
        var lastPatternSegment = patternSegments.Last();
        int bestSingleSegmentScore = 0;

        for (int i = 0; i < targetSegments.Length; i++)
        {
            var tSeg = targetSegments[i];
            double similarity = CalculateSimilarity(tSeg, lastPatternSegment);

            if (similarity > 0.6)
            {
                int segmentScore = (int)(800 * similarity);

                // Strong recency bonus for single segment matching
                if (i == targetSegments.Length - 1) segmentScore += 500;
                else if (i == targetSegments.Length - 2) segmentScore += 250;

                segmentScore += i * 10; // Standard recency
                
                bestSingleSegmentScore = Math.Max(bestSingleSegmentScore, segmentScore);
            }
        }
        
        if (bestSingleSegmentScore > 0) return bestSingleSegmentScore;

        // 5. Final fallback: check for subsequence on the whole string, low score.
        if (IsSubsequence(targetLower, patternLower))
        {
            return 100 + pattern.Length;
        }

        return 0;
    }

    /// <summary>
    /// General fuzzy match for individual words (Controllers, Actions).
    /// </summary>
    public static int MatchWord(string target, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return 100;
        if (string.IsNullOrEmpty(target)) return 0;

        target = target.ToLowerInvariant();
        pattern = pattern.ToLowerInvariant();

        if (target == pattern) return 1000;
        
        double similarity = CalculateSimilarity(target, pattern);
        if (similarity > 0.5) return (int)(900 * similarity);

        if (target.Contains(pattern)) return 400;
        if (IsSubsequence(target, pattern)) return 100;

        return 0;
    }

    public static int MatchController(string controllerName, string pattern)
    {
        var shortName = controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
            ? controllerName[..^10]
            : controllerName;

        return Math.Max(MatchWord(controllerName, pattern), MatchWord(shortName, pattern));
    }

    /// <summary>
    /// Calculates a similarity ratio between 0 and 1.
    /// Handles the "1 off on length 2 is bad, 1 off on 7 is fine" requirement.
    /// </summary>
    private static double CalculateSimilarity(string source, string target)
    {
        int distance = LevenshteinDistance(source, target);
        int maxLength = Math.Max(source.Length, target.Length);
        if (maxLength == 0) return 1.0;

        // Formula: 1 - (Distance / MaxLength)
        // If pattern is "us" (len 2) and target is "u" (len 1), distance is 1. Ratio = 0.5 (Bad)
        // If pattern is "settings" (len 8) and target is "setting", distance is 1. Ratio = 0.87 (Good)
        return 1.0 - ((double)distance / maxLength);
    }

    private static int LevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) ;
        for (int j = 0; j <= m; d[0, j] = j++) ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }

    private static bool IsSubsequence(string target, string pattern)
    {
        int patternIndex = 0;
        foreach (var c in target)
        {
            if (patternIndex < pattern.Length && c == pattern[patternIndex])
                patternIndex++;
        }
        return patternIndex == pattern.Length;
    }
}