using System;
using UnityEngine;

[Serializable]
public class TabSnapshot
{
    public string tabId;
    public ScreenState currentState;
    public bool assistantOpen;
    public SearchResultSet activeSearchResults;

    public TabSnapshot(string id)
    {
        tabId = id;
    }
}
