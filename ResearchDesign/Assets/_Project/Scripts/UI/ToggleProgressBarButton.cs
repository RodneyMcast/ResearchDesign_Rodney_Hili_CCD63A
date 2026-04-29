using UnityEngine;

public class ToggleProgressBarButton : MonoBehaviour
{
    public void OnButtonPressed()
    {
        var gm = FindObjectOfType<GamificationManager>();
        if (gm != null)
            gm.ToggleProgressBar();
        else
            Debug.LogWarning("[ToggleProgressBarButton] No GamificationManager found in scene.");
    }
}
