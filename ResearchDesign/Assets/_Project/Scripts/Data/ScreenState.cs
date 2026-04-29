using UnityEngine;

public enum SearchLayoutType { None, Normal, Results }

public enum ResultsTab
{
    None,
    Assistant,
    Links,
    Images
}

[CreateAssetMenu(menuName = "AgenticBrowser/Screen State")]
public class ScreenState : ScriptableObject
{
    public string stateId;

    [Header("Default Screenshots (used for non-results states)")]
    public Sprite assistantClosed;
    public Sprite assistantOpen;

    [Header("Search UI")]
    public SearchLayoutType searchLayout = SearchLayoutType.Normal;

    [Header("Results Template")]
    public ResultsTab resultsTab = ResultsTab.None;
}
