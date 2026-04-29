using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TabManager : MonoBehaviour
{

    
    public static TabManager Instance { get; private set; }

    [Header("UI (optional)")]
    [SerializeField] private GameObject tab1UI;   
    [SerializeField] private Button newTabButton; 

    [Header("Initial")]
    [SerializeField] private ScreenState defaultStateForNewTab;
    [SerializeField] private bool defaultAssistantOpenForNewTab = false;
    [SerializeField] private bool syncFirstTabWithScreenStartup = true;

    [Header("Tabs")]
    [SerializeField] private List<TabSnapshot> tabs = new();
    [SerializeField] private int activeTabIndex = 0;

    private ScreenController SC => ScreenController.Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private IEnumerator Start()
    {
        // Wait one frame so ScreenController.Start has loaded its configured start state first.
        yield return null;

        if (tabs.Count == 0)
        {
            tabs.Add(new TabSnapshot("Tab 1")
            {
                currentState = defaultStateForNewTab,
                assistantOpen = defaultAssistantOpenForNewTab,
                activeSearchResults = null
            });
        }

        if (syncFirstTabWithScreenStartup && tabs.Count > 0 && SC != null)
        {
            tabs[0].currentState = SC.CurrentState != null ? SC.CurrentState : defaultStateForNewTab;
            tabs[0].assistantOpen = SC.AssistantOpen;
            tabs[0].activeSearchResults = SC.ActiveSearchResults;
        }

        activeTabIndex = Mathf.Clamp(activeTabIndex, 0, tabs.Count - 1);

        ApplyTab(activeTabIndex);
        RefreshTabUI();
    }

    
    public void CreateNewTab()
{
    SaveCurrentTab();

    
    if (tab1UI != null)
        tab1UI.SetActive(true);

    
    if (newTabButton != null)
        newTabButton.interactable = false;

    var newIndex = tabs.Count + 1;
    tabs.Add(new TabSnapshot($"Tab {newIndex}")
    {
        currentState = defaultStateForNewTab,
        assistantOpen = defaultAssistantOpenForNewTab,
        activeSearchResults = null
    });

    activeTabIndex = tabs.Count - 1;
    ApplyTab(activeTabIndex);
    RefreshTabUI();

    GameManager.Instance?.RecordAction("tab_new_created_once");
}


    
    public void SwitchToTab(int index)
    {
        if (index < 0 || index >= tabs.Count) return;
        if (index == activeTabIndex) return;

        SaveCurrentTab();

        activeTabIndex = index;
        ApplyTab(activeTabIndex);
        RefreshTabUI();

        GameManager.Instance?.RecordAction($"tab_switched:{index}");
    }

    private void SaveCurrentTab()
    {
        if (SC == null) return;
        if (activeTabIndex < 0 || activeTabIndex >= tabs.Count) return;

        var t = tabs[activeTabIndex];
        t.currentState = SC.CurrentState;                 
        t.assistantOpen = SC.AssistantOpen;               
        t.activeSearchResults = SC.ActiveSearchResults;   
    }

    private void ApplyTab(int index)
    {
        if (SC == null) return;

        var t = tabs[index];

        
        SC.SetActiveSearchResults(t.activeSearchResults);
        SC.SetAssistantOpen(t.assistantOpen);

        
        if (t.currentState != null)
            SC.LoadState(t.currentState, pushToHistory: false);

        GameManager.Instance?.RecordAction($"tab_applied:{index}");
    }

    private void RefreshTabUI()
    {
        
    }

    

   
    public int GetTabCount() => tabs.Count;
    public int GetActiveTabIndex() => activeTabIndex;
}
