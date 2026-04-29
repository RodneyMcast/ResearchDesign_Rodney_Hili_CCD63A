using UnityEngine;
using UnityReminder = UnityEngine.UI;
using UnityEngine.UI;
using System.Collections.Generic;

public class ScreenController : MonoBehaviour
{
    public static ScreenController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Image browserBackground;

    [Header("Starting State")]
    [SerializeField] private ScreenState startState;

    [Header("Search")]
    [SerializeField] private SearchUIManager searchUIManager;

    [Header("Buttons")]
    [SerializeField] private ButtonUIManager buttonUIManager;

    [Header("Search Results")]
    [SerializeField] private SearchResultSet activeSearchResults;
    public void SetActiveSearchResults(SearchResultSet set) => activeSearchResults = set;

    [Header("Assistant Logic")]
    [SerializeField] private AssistantChatManager assistantChatManager;

    private ScreenState _currentState;
    private bool _assistantOpen;

    private readonly Stack<ScreenState> _history = new Stack<ScreenState>();

    public ScreenState CurrentState => _currentState;
    public bool AssistantOpen => _assistantOpen;

    public event System.Action<ScreenState> OnStateChanged;

    public SearchResultSet ActiveSearchResults => activeSearchResults;

    private void RecordTelemetry(string actionId)
    {
        GameManager.Instance?.RecordAction(actionId);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetAssistantOpen(bool open)
    {
        _assistantOpen = open;
        AssistantSidebarUI.Instance?.SetOpen(open);
        ApplyCurrentVisual();
    }

    private void Start()
    {
        if (startState != null)
        {
            LoadState(startState, pushToHistory: false);
        }
        else
        {
            Debug.LogWarning("[ScreenController] No startState set.");
        }
    }

    public void LoadState(ScreenState newState, bool pushToHistory = true)
    {
        if (newState == null)
        {
            Debug.LogWarning("[ScreenController] Tried to load null state.");
            return;
        }

        if (pushToHistory && _currentState != null)
        {
            _history.Push(_currentState);
        }

        _currentState = newState;

        ApplyCurrentVisual();
        searchUIManager?.Apply(_currentState, _assistantOpen);
        buttonUIManager?.Apply(_currentState, _assistantOpen);
        AssistantSidebarUI.Instance?.SetOpen(_assistantOpen);

        OnStateChanged?.Invoke(_currentState);

        RecordTelemetry($"telemetry_state_loaded:{newState.stateId}");
    }

    public void ToggleAssistant()
    {
        if (_currentState == null)
        {
            Debug.LogWarning("[ScreenController] No current state set.");
            return;
        }

        _assistantOpen = !_assistantOpen;

        AssistantSidebarUI.Instance?.SetOpen(_assistantOpen);

        ApplyCurrentVisual();
        searchUIManager?.Apply(_currentState, _assistantOpen);
        buttonUIManager?.Apply(_currentState, _assistantOpen);

        RecordTelemetry($"telemetry_assistant_toggled:{(_assistantOpen ? "open" : "closed")}");
    }

    public void GoBack()
    {
        if (_history.Count == 0)
        {
            Debug.Log("[ScreenController] No previous state in history.");
            return;
        }

        _currentState = _history.Pop();
        ApplyCurrentVisual();

        OnStateChanged?.Invoke(_currentState);

        RecordTelemetry($"telemetry_goback_state:{_currentState.stateId}");
    }

    private void ApplyCurrentVisual()
    {
        if (browserBackground == null || _currentState == null) return;

        Sprite spriteToShow = null;

        if (_currentState.resultsTab != ResultsTab.None)
        {
            if (activeSearchResults == null)
            {
                Debug.LogWarning("[ScreenController] Results state active but no SearchResultSet selected.");
                return;
            }

            spriteToShow = GetResultsSprite(_currentState.resultsTab, _assistantOpen);
        }
        else
        {
            spriteToShow = _assistantOpen ? _currentState.assistantOpen : _currentState.assistantClosed;
        }

        if (spriteToShow == null)
        {
            Debug.LogWarning($"[ScreenController] Missing sprite for state '{_currentState.stateId}'.");
            return;
        }

        browserBackground.sprite = spriteToShow;
    }

    private Sprite GetResultsSprite(ResultsTab tab, bool assistantOpen)
    {
        switch (tab)
        {
            case ResultsTab.Assistant:
                return assistantOpen ? activeSearchResults.assistantOpen : activeSearchResults.assistantClosed;

            case ResultsTab.Links:
                return assistantOpen ? activeSearchResults.linksOpen : activeSearchResults.linksClosed;

            case ResultsTab.Images:
                return assistantOpen ? activeSearchResults.imagesOpen : activeSearchResults.imagesClosed;
        }

        return null;
    }
}
