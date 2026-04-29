using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AssistantSidebarUI : MonoBehaviour
{
    public static AssistantSidebarUI Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject assistantPanel;

    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Message Text")]
    [SerializeField] private TMP_Text userText;
    [SerializeField] private TMP_Text aiText;

    [Header("Input")]
    [SerializeField] private TMP_InputField assistantInput;

    [Header("Registry")]
    [SerializeField] private AssistantPromptRegistry promptRegistry;

    [TextArea(2, 6)]
    [SerializeField] private string noMatchResponse =
        "I got your prompt, but I don’t have a scripted response for that yet.";

    private bool _isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (assistantInput != null)
        {
            assistantInput.onSubmit.AddListener(OnSubmit);
        }
    }

    private void OnDestroy()
    {
        if (assistantInput != null)
        {
            assistantInput.onSubmit.RemoveListener(OnSubmit);
        }
    }

public void SetOpen(bool open)
{
    _isOpen = open;

    if (assistantPanel != null)
        assistantPanel.SetActive(open);

    if (assistantInput != null)
    {
        assistantInput.interactable = open;

        if (open)
        {
            assistantInput.ActivateInputField();
            SnapScrollToTop();
        }
        else
        {
            assistantInput.text = "";
        }
    }
}


    private void OnSubmit(string text)
    {
        if (!_isOpen) return;

        var cleaned = (text ?? "").Trim();
        if (string.IsNullOrEmpty(cleaned)) return;

        assistantInput.text = "";
        assistantInput.ActivateInputField();

        if (userText != null)
            userText.text = cleaned;

        string response = GetNoMatchResponse();
        bool usedNoMatchResponse = false;

        if (AssistantPromptRouter.TryResolve(promptRegistry, cleaned, out var entry))
        {
            response = entry.responseText;
            GameManager.Instance?.RecordAction($"assistant_intent:{entry.entryId}");
        }
        else
        {
            usedNoMatchResponse = true;
            GameManager.Instance?.RecordAction("assistant_intent:NO_MATCH");
        }

        if (aiText != null)
            aiText.text = response;

        GameManager.Instance?.RecordAction($"assistant_prompt:{cleaned}");
        GameManager.Instance?.RecordAction("assistant_response_shown");

        if (usedNoMatchResponse)
            RobotTextManager.Instance?.PlayJump();

        SnapScrollToTop();
    }

    private string GetNoMatchResponse()
    {
        if (promptRegistry != null && !string.IsNullOrWhiteSpace(promptRegistry.noMatchResponseText))
            return promptRegistry.noMatchResponseText;

        return "In this demo this prompt does not exist yet.";
    }

    private void ForceScrollToBottom()
    {
        if (scrollRect == null) return;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }

    private void SnapScrollToTop()
{
    if (scrollRect == null) return;
    Canvas.ForceUpdateCanvases();
    scrollRect.verticalNormalizedPosition = 1f;
    Canvas.ForceUpdateCanvases();
}

    public void SetVisible(bool visible)
{
    if (assistantPanel != null)
        assistantPanel.SetActive(visible);
}

}
