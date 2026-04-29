using UnityEngine;

public class HintButton : MonoBehaviour
{
    [SerializeField] private LevelObjectiveTracker tracker;
    [SerializeField] private string actionId = "hint_requested";
    [SerializeField] private RobotTextManager textManager;
    [Tooltip("Optional. Drag the LevelStatsManager here to count hint usage for scoring.")]
    [SerializeField] private LevelStatsManager statsManager;

    private void Start()
    {
        if (statsManager == null) statsManager = FindFirstObjectByType<LevelStatsManager>();
    }

    public void ShowHint()
    {
        if (tracker == null)
        {
            Debug.LogWarning("[HintButton] No LevelObjectiveTracker assigned.");
            return;
        }

        if (textManager == null)
        {
            Debug.LogWarning("[HintButton] No RobotTextManager assigned.");
            return;
        }

        string hint = tracker.GetCurrentHint();
        string milestone = tracker.GetCurrentStepActionId();
        textManager.ShowHintText(hint);

        statsManager?.RegisterHint();

        if (GameManager.Instance != null && !string.IsNullOrEmpty(actionId))
        {
            var trimmedActionId = actionId.Trim();
            if (!string.IsNullOrEmpty(milestone))
                GameManager.Instance.RecordAction($"{trimmedActionId}:{milestone}");
            else
                GameManager.Instance.RecordAction(trimmedActionId);
        }
    }
}
