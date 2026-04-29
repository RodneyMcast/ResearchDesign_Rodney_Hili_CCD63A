using UnityEngine;

public class MissionButton : MonoBehaviour
{
    [SerializeField] private RobotTextManager textManager;
    [SerializeField] private string actionId = "mission_requested";

    [Header("Mission Content")]
    [TextArea(2, 8)]
    [SerializeField] private string missionText;

    public void ShowMission()
    {
        if (textManager == null)
        {
            Debug.LogWarning("[MissionButton] No RobotTextManager assigned.");
            return;
        }

        var message = (missionText ?? string.Empty).Trim();
        if (message.Length == 0)
        {
            Debug.LogWarning("[MissionButton] No mission text assigned.");
            return;
        }

        // Use the same hint text box flow as HintButton so both buttons share one display.
        textManager.ShowHintText(message);

        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(actionId))
            GameManager.Instance.RecordAction(actionId.Trim());
    }
}
