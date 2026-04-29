using UnityEngine;

public class SearchUIManager : MonoBehaviour
{
    [Header("Variants (enable ONE at a time)")]
    [SerializeField] private GameObject normalSearch;
    [SerializeField] private GameObject normalSearchAssistant;

    [SerializeField] private GameObject resultsSearch;
    [SerializeField] private GameObject resultsSearchAssistant;

    [Header("Optional")]
    [SerializeField] private bool debugLogs = true;

    public void Apply(ScreenState state, bool assistantOpen)
    {
        
        SetAll(false);

        if (state == null)
        {
            if (debugLogs) Debug.Log("[SearchUIManager] No state, hiding search.");
            return;
        }

        switch (state.searchLayout)
        {
            case SearchLayoutType.None:
                if (debugLogs) Debug.Log("[SearchUIManager] SearchLayout=None -> hide all.");
                break;

            case SearchLayoutType.Normal:
                if (assistantOpen) normalSearchAssistant?.SetActive(true);
                else normalSearch?.SetActive(true);

                if (debugLogs) Debug.Log($"[SearchUIManager] SearchLayout=Normal assistantOpen={assistantOpen}");
                break;

            case SearchLayoutType.Results:
                if (assistantOpen) resultsSearchAssistant?.SetActive(true);
                else resultsSearch?.SetActive(true);

                if (debugLogs) Debug.Log($"[SearchUIManager] SearchLayout=Results assistantOpen={assistantOpen}");
                break;
        }
    }

    private void SetAll(bool active)
    {
        if (normalSearch) normalSearch.SetActive(active);
        if (normalSearchAssistant) normalSearchAssistant.SetActive(active);
        if (resultsSearch) resultsSearch.SetActive(active);
        if (resultsSearchAssistant) resultsSearchAssistant.SetActive(active);
    }
}
