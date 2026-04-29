using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_InputField))]
public class AssistantChatInputController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField inputField;

    [Header("Display")]
    [SerializeField] private TMP_Text promptText;   
    [SerializeField] private TMP_Text outputText;   
    [SerializeField] private ScrollRect scrollRect; 

    [Header("Registry")]
    [SerializeField] private AssistantPromptRegistry promptRegistry;

    [Header("Fallback")]
    [TextArea(2, 8)]
    [SerializeField] private string noMatchResponse =
        "I didn’t get that. Try asking about something like: iphone, booking, privacy, or safety.";

    private void Reset()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    private void Awake()
    {
        if (inputField == null) inputField = GetComponent<TMP_InputField>();

        
        inputField.onSubmit.AddListener(OnSubmitted);
    }

    private void OnDestroy()
    {
        if (inputField != null)
            inputField.onSubmit.RemoveListener(OnSubmitted);
    }

    private void OnSubmitted(string text)
    {
        
        if (ScreenController.Instance != null && !ScreenController.Instance.AssistantOpen)
            return;

        var cleaned = (text ?? "").Trim();
        if (cleaned.Length == 0) return;

        
        if (promptText != null)
            promptText.text = cleaned;

        GameManager.Instance?.RecordAction($"assistant_prompt:{cleaned}");

        
        string response = GetNoMatchResponse();
        bool usedNoMatchResponse = false;
        if (AssistantPromptRouter.TryResolve(promptRegistry, cleaned, out var entry))
        {
            response = entry.responseText;
            GameManager.Instance?.RecordAction($"assistant_response:{entry.entryId}");
        }
        else
        {
            usedNoMatchResponse = true;
            GameManager.Instance?.RecordAction("assistant_response:NO_MATCH");
        }

        if (outputText != null)
            outputText.text = response;

        
        inputField.text = "";
        inputField.ActivateInputField();

        
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        if (usedNoMatchResponse)
            RobotTextManager.Instance?.PlayJump();
    }

    private string GetNoMatchResponse()
    {
        if (promptRegistry != null && !string.IsNullOrWhiteSpace(promptRegistry.noMatchResponseText))
            return promptRegistry.noMatchResponseText;

        return "In this demo this prompt does not exist yet.";
    }
}
