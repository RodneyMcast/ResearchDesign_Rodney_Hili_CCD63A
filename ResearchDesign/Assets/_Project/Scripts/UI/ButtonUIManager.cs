using UnityEngine;
using System.Collections.Generic;

public class ButtonUIManager : MonoBehaviour
{
    [Header("Buttons for 'Assistant Closed' state")]
    [SerializeField] private List<GameObject> closedStateButtons;

    [Header("Buttons for 'Assistant Open' state")]
    [SerializeField] private List<GameObject> openStateButtons;

    public void Apply(ScreenState state, bool assistantOpen)
    {
        
        bool isResultsPage = (state != null && state.resultsTab != ResultsTab.None);

        
        ToggleGroup(closedStateButtons, false);
        ToggleGroup(openStateButtons, false);

        if (!isResultsPage) return;

        
        if (assistantOpen)
        {
            ToggleGroup(openStateButtons, true);
        }
        else
        {
            ToggleGroup(closedStateButtons, true);
        }
    }

    private void ToggleGroup(List<GameObject> group, bool active)
    {
        foreach (var btn in group)
        {
            if (btn != null) btn.SetActive(active);
        }
    }
}