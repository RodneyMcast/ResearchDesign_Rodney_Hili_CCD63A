using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AgenticBrowser/Search Intent Registry")]
public class SearchIntentRegistry : ScriptableObject
{
    [Serializable]
    public class Intent
    {
        [Header("Name (for your logs/debug)")]
        public string intentId = "IPHONE";

        [Header("Matching")]
        [Tooltip("Exact keywords (lower/upper doesn’t matter). Example: iphone")]
        public List<string> keywords = new();

        [Tooltip("If true, 'iphone near me' matches keyword 'iphone'.")]
        public bool allowContainsMatch = true;

        [Tooltip("Max edit distance for typos (0 disables fuzzy). 1-2 is usually enough.")]
        [Range(0, 3)] public int maxEditDistance = 2;

        [Header("Route To")]
        public ScreenState targetState;          
        public SearchResultSet resultsSet;       


        [Header("Priority")]
        [Tooltip("Higher = wins if multiple intents match. Example: iPhone=100, Phone=10")]
        public int priority = 0;

    }

    public List<Intent> intents = new();
}
