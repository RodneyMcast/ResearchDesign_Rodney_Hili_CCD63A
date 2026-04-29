using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HotspotButton : MonoBehaviour
{
    public enum HotspotAction
    {
        ToggleAssistant,
        GoBack,
        LoadState
    }

    [Header("Logging")]
    [SerializeField] private string actionId = "UNSET_ACTION";

    [Header("Action")]
    [SerializeField] private HotspotAction action = HotspotAction.ToggleAssistant;

    [Header("LoadState (only used if action = LoadState)")]
    [SerializeField] private ScreenState targetState;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClicked);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClicked);
    }

    private void OnClicked()
    {
        
        if (ScreenController.Instance == null)
        {
            Debug.LogWarning("[HotspotButton] ScreenController not found.");
            return;
        }

        switch (action)
        {
            case HotspotAction.ToggleAssistant:
                ScreenController.Instance.ToggleAssistant();
                break;

            case HotspotAction.GoBack:
                ScreenController.Instance.GoBack();
                break;

            case HotspotAction.LoadState:
                ScreenController.Instance.LoadState(targetState, pushToHistory: true);
                break;
        }

        
        if (GameManager.Instance != null)
            GameManager.Instance.RecordAction(actionId);
    }
}
