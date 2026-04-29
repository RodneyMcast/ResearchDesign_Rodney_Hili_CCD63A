using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AgenticBrowser/Assistant Prompt Registry", fileName = "APR_New")]
public class AssistantPromptRegistry : ScriptableObject
{
    [Header("Fallback")]
    [TextArea(2, 8)]
    public string noMatchResponseText = "In this demo this prompt does not exist yet.";

    [Header("Prompt Entries")]
    public List<Entry> entries = new List<Entry>();

    [Serializable]
    public class Entry
    {
        public string entryId = "UNSET";
        public int priority = 0;

        [TextArea(2, 6)]
        public List<string> keywords = new List<string>();

        public bool allowContainsMatch = true;

        [Tooltip("0 = off. 1-3 is usually enough for typos.")]
        public int maxEditDistance = 0;

        [TextArea(5, 30)]
        public string responseText = "TODO";
    }
}
