using UnityEngine;

public class GamificationManager : MonoBehaviour
{
    public static event System.Action<bool> OnProgressBarVisibilityChanged;

    [Header("UI References")]
    [Tooltip("Assign the GameObject that contains your Progress Bar.")]
    public GameObject progressBarUI;

    [Tooltip("Assign the Robot GameObject. It shows when the progress bar is hidden (inverse).")]
    public GameObject robotUI;

    [Header("Settings")]
    public bool hideOnStart = true;

    private bool _isVisible;
    private bool _hasInitializedVisibility;
    public bool IsVisible => _isVisible;

    private void Start()
    {
        if (_hasInitializedVisibility)
            return;

        _isVisible = !hideOnStart;
        ApplyVisibility();
        _hasInitializedVisibility = true;
    }

    public void ToggleProgressBar()
    {
        _isVisible = !_isVisible;
        ApplyVisibility();
        _hasInitializedVisibility = true;
    }

    public void SetProgressBarVisible(bool isVisible)
    {
        if (_hasInitializedVisibility && _isVisible == isVisible)
            return;

        _isVisible = isVisible;
        ApplyVisibility();
        _hasInitializedVisibility = true;
    }

    private void ApplyVisibility()
    {
        if (progressBarUI != null)
            progressBarUI.SetActive(_isVisible);

        if (robotUI != null)
            robotUI.SetActive(!_isVisible);

        OnProgressBarVisibilityChanged?.Invoke(_isVisible);
        GameManager.Instance?.RecordAction($"telemetry_progress_bar_visible:{_isVisible}");
    }
}
