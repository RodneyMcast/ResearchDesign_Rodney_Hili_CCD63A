using System;
using System.Linq;

public static class SearchRouter
{
    public static bool TryResolve(
    SearchIntentRegistry registry,
    string userText,
    out SearchIntentRegistry.Intent matchedIntent)
{
    matchedIntent = null;
    if (registry == null) return false;

    var cleaned = (userText ?? "").Trim();
    if (cleaned.Length == 0) return false;

    var input = cleaned.ToLower();

    var tokens = input
        .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ':', ';', '-', '_', '/', '\\', '(', ')', '[', ']' },
               StringSplitOptions.RemoveEmptyEntries);

    SearchIntentRegistry.Intent best = null;
    int bestPriority = int.MinValue;
    int bestScore = int.MaxValue; 

    foreach (var intent in registry.intents)
    {
        if (intent == null || intent.keywords == null || intent.keywords.Count == 0)
            continue;

        var keys = intent.keywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToLower())
            .ToArray();

        
        int matchScore = int.MaxValue;
        bool matched = false;

        if (keys.Any(k => input == k))
        {
            matched = true;
            matchScore = 0;
        }
        else if (intent.allowContainsMatch && keys.Any(k => input.Contains(k)))
        {
            matched = true;
            matchScore = 1;
        }
        else if (intent.maxEditDistance > 0)
        {
            foreach (var t in tokens)
            {
                foreach (var k in keys)
                {
                    if (LevenshteinDistance(t, k) <= intent.maxEditDistance)
                    {
                        matched = true;
                        matchScore = 2;
                        goto FuzzyDone;
                    }
                }
            }
        }
    FuzzyDone:

        if (!matched) continue;

        
        if (intent.priority > bestPriority ||
            (intent.priority == bestPriority && matchScore < bestScore))
        {
            best = intent;
            bestPriority = intent.priority;
            bestScore = matchScore;
        }
    }

    matchedIntent = best;
    return matchedIntent != null;
}


static int LevenshteinDistance(string a, string b)
{
    if (a == b) return 0;
    if (a.Length == 0) return b.Length;
    if (b.Length == 0) return a.Length;

    int[,] d = new int[a.Length + 1, b.Length + 1];

    for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
    for (int j = 0; j <= b.Length; j++) d[0, j] = j;

    for (int i = 1; i <= a.Length; i++)
    {
        for (int j = 1; j <= b.Length; j++)
        {
            int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;

            d[i, j] = Min(
                d[i - 1, j] + 1,      
                d[i, j - 1] + 1,      
                d[i - 1, j - 1] + cost 
            );
        }
    }

    return d[a.Length, b.Length];
}

    static int Min(int x, int y, int z) => (x < y) ? ((x < z) ? x : z) : ((y < z) ? y : z);
}

