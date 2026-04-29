using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AssistantChatManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text userTextField;
    [SerializeField] private TMP_Text aiTextField;
    [SerializeField] private GameObject assistantPanel;
    [SerializeField] private TMP_InputField sidebarInput;

    [Header("Optional")]
    [SerializeField] private ScrollRect scrollRect;

    
    public GameObject GetPanel() => assistantPanel;

    public void SetPanelVisible(bool visible)
    {
        if (assistantPanel != null)
            assistantPanel.SetActive(visible);
    }

    
    public void DisplayResponse(string userPrompt, string aiResponse)
    {
        if (userTextField != null)
            userTextField.text = userPrompt;

        if (aiTextField != null)
            aiTextField.text = aiResponse;

        
        if (sidebarInput != null)
        {
            sidebarInput.text = "";
            sidebarInput.ActivateInputField();
        }

       
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    
    public void Clear()
    {
        if (userTextField != null) userTextField.text = "";
        if (aiTextField != null) aiTextField.text = "";
        if (sidebarInput != null) sidebarInput.text = "";
    }
}
