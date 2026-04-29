using System.Linq;
using UnityEngine;

public class ToggleObjectButton : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool useThisObjectIfTargetIsMissing;

    [Header("Optional Achievement Refresh")]
    [SerializeField] private bool refreshAchievementsOnPress;
    [SerializeField] private AchievementManager achievementManager;

    public void OnButtonPressed()
    {
        var target = ResolveTarget();
        if (target == null)
        {
            Debug.LogWarning("[ToggleObjectButton] No target object assigned.");
            return;
        }

        target.SetActive(!target.activeSelf);

        if (!refreshAchievementsOnPress)
            return;

        var managerToRefresh = ResolveAchievementManager(target);
        if (managerToRefresh == null)
        {
            Debug.LogWarning("[ToggleObjectButton] refreshAchievementsOnPress is enabled, but no AchievementManager was found.");
            return;
        }

        Debug.Log($"[ToggleObjectButton] Refreshing achievements with manager '{managerToRefresh.gameObject.name}'.");
        managerToRefresh.ForceRefresh();
    }

    private GameObject ResolveTarget()
    {
        if (targetObject != null)
            return targetObject;

        return useThisObjectIfTargetIsMissing ? gameObject : null;
    }

    private AchievementManager ResolveAchievementManager(GameObject target)
    {
        if (achievementManager != null)
            return achievementManager;

        if (AchievementManager.Instance != null)
        {
            achievementManager = AchievementManager.Instance;
            return achievementManager;
        }

        if (target != null)
        {
            achievementManager = target.GetComponent<AchievementManager>();
            if (achievementManager != null)
                return achievementManager;

            achievementManager = target.GetComponentInChildren<AchievementManager>(true);
            if (achievementManager != null)
                return achievementManager;
        }

        achievementManager = FindFirstObjectByType<AchievementManager>();
        if (achievementManager != null)
            return achievementManager;

        achievementManager = Resources.FindObjectsOfTypeAll<AchievementManager>()
            .FirstOrDefault(manager => manager != null && manager.gameObject.scene.IsValid());

        return achievementManager;
    }
}
