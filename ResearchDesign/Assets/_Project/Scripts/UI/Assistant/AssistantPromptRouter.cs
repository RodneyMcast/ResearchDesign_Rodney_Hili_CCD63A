using System;
using System.Linq;

public static class AssistantPromptRouter
{
    public static bool TryResolve(
        AssistantPromptRegistry registry,
        string userPrompt,
        out AssistantPromptRegistry.Entry bestEntry)
    {
        bestEntry = null;
        if (registry == null || registry.entries == null) return false;

        var promptTokens = Tokenize(userPrompt);
        var prompt = string.Join(" ", promptTokens);
        if (string.IsNullOrEmpty(prompt)) return false;

        int bestScore = int.MinValue;

        foreach (var entry in registry.entries)
        {
            if (entry == null || entry.keywords == null || entry.keywords.Count == 0) continue;

            int entryBest = int.MinValue;

            foreach (var rawK in entry.keywords)
            {
                var keywordTokens = Tokenize(rawK);
                var k = string.Join(" ", keywordTokens);
                if (string.IsNullOrEmpty(k)) continue;

                int strength = int.MinValue;
                bool isSingleWordKeyword = keywordTokens.Length == 1;

                if (prompt == k)
                {
                    strength = 3000;
                }
                else if (isSingleWordKeyword)
                {
                    if (entry.allowContainsMatch && promptTokens.Contains(k))
                        strength = 2500 + Math.Min(200, k.Length);
                }
                else if (entry.allowContainsMatch && ContainsPhrase(promptTokens, keywordTokens))
                {
                    strength = 2000 + Math.Min(200, k.Length);
                }
                else if (entry.maxEditDistance > 0)
                {
                    int dist = GetBestPhraseFuzzyDistance(prompt, promptTokens, k, keywordTokens.Length, entry.maxEditDistance);

                    if (dist != int.MaxValue)
                    {
                        strength = 1000 - (dist * 50);
                    }
                }

                if (strength > entryBest)
                    entryBest = strength;
            }

            if (entryBest == int.MinValue) continue;

            int score = entryBest + (entry.priority * 10);

            if (score > bestScore)
            {
                bestScore = score;
                bestEntry = entry;
            }
        }

        return bestEntry != null;
    }

    private static string[] Tokenize(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Array.Empty<string>();

        var normalizedChars = s
            .Trim()
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : ' ')
            .ToArray();

        return new string(normalizedChars)
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool ContainsPhrase(string[] promptTokens, string[] keywordTokens)
    {
        if (promptTokens == null || keywordTokens == null || keywordTokens.Length == 0)
            return false;

        if (promptTokens.Length < keywordTokens.Length)
            return false;

        for (int start = 0; start <= promptTokens.Length - keywordTokens.Length; start++)
        {
            bool matched = true;

            for (int i = 0; i < keywordTokens.Length; i++)
            {
                if (!string.Equals(promptTokens[start + i], keywordTokens[i], StringComparison.Ordinal))
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
                return true;
        }

        return false;
    }

    private static int GetBestPhraseFuzzyDistance(string prompt, string[] promptTokens, string keyword, int keywordTokenCount, int configuredMaxDistance)
    {
        int best = GetAllowedPhraseDistance(prompt, keyword, configuredMaxDistance);

        if (promptTokens == null || keywordTokenCount <= 0 || promptTokens.Length < keywordTokenCount)
            return best;

        for (int start = 0; start <= promptTokens.Length - keywordTokenCount; start++)
        {
            var candidate = string.Join(" ", promptTokens.Skip(start).Take(keywordTokenCount));
            int distance = GetAllowedPhraseDistance(candidate, keyword, configuredMaxDistance);
            if (distance < best)
                best = distance;
        }

        return best;
    }

    private static int GetAllowedPhraseDistance(string candidate, string keyword, int configuredMaxDistance)
    {
        if (string.IsNullOrEmpty(candidate) || string.IsNullOrEmpty(keyword) || configuredMaxDistance <= 0)
            return int.MaxValue;

        if (Math.Abs(candidate.Length - keyword.Length) > configuredMaxDistance)
            return int.MaxValue;

        int distance = Levenshtein(candidate, keyword);
        return distance <= configuredMaxDistance ? distance : int.MaxValue;
    }

    private static int Levenshtein(string a, string b)
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
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        return d[a.Length, b.Length];
    }
}
