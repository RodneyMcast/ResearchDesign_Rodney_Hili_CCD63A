using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AskAnythingInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Image backgroundImage; 

    [Header("Colors")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.0f);   
    [SerializeField] private Color focusedColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);

    [Header("Routing")]
    [SerializeField] private ScreenState iphoneState;

    [Header("generic Results_Assistant state")]
    [SerializeField] private ScreenState resultsAssistantState; 
    [SerializeField] private SearchResultSet iphoneResults;

    [SerializeField] private SearchIntentRegistry intentRegistry;

    [Header("No match fallback")]
    [SerializeField] private ScreenState noMatchState;              
    [SerializeField] private SearchResultSet noMatchResults;        


    private void Reset()
    {
        inputField = GetComponent<TMP_InputField>();
        backgroundImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (inputField == null) inputField = GetComponent<TMP_InputField>();
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();

        if (backgroundImage != null)
            backgroundImage.color = idleColor;

        inputField.onSelect.AddListener(_ => OnFocused());

        inputField.onSubmit.AddListener(OnSubmitted);

    }

    private void OnDestroy()
    {
        inputField.onSelect.RemoveAllListeners();
        inputField.onSubmit.RemoveAllListeners();
        inputField.onEndEdit.RemoveAllListeners();
    }

    private void OnFocused()
    {
        if (backgroundImage != null)
            backgroundImage.color = focusedColor;

        if (GameManager.Instance != null)
            GameManager.Instance.RecordAction("ask_anything_focused");
    }

    private void OnSubmitted(string text)
    {
        HandleText(text, submittedByEnter: true);
    }

    private void OnEndEditBackup(string text)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            HandleText(text, submittedByEnter: true);
        }
    }

    private void HandleText(string text, bool submittedByEnter)
    {
        if (!submittedByEnter) return;

        var cleaned = (text ?? "").Trim();

        if (GameManager.Instance != null)
            GameManager.Instance.RecordAction($"ask_anything_submitted:{cleaned}");

        if (SearchRouter.TryResolve(intentRegistry, cleaned, out var intent))
    {
         if (intent.resultsSet != null)
            ScreenController.Instance.SetActiveSearchResults(intent.resultsSet);

         if (intent.targetState != null)
            ScreenController.Instance.LoadState(intent.targetState, pushToHistory: true);

        GameManager.Instance?.RecordAction($"transition_intent:{intent.intentId}");
    }
    else
    {
        GameManager.Instance?.RecordAction("transition_intent:NO_MATCH");

        if (noMatchResults != null)
            ScreenController.Instance.SetActiveSearchResults(noMatchResults);

    
        if (noMatchState != null)
            ScreenController.Instance.LoadState(noMatchState, pushToHistory: true);
    }


        if (backgroundImage != null)
            backgroundImage.color = idleColor;
    }
}
