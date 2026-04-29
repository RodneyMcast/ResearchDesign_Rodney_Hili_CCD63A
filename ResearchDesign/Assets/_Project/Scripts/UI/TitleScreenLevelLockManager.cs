using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleScreenLevelLockManager : MonoBehaviour
{
    [Serializable]
    public class LevelButtonEntry
    {
        [Tooltip("For inspector readability only.")]
        public string label = "Level";

        [Tooltip("How many cleared levels are required to unlock this button. Example: level 1 = 0, level 2 = 1.")]
        [Min(0)] public int requiredClearedLevels;

        [Tooltip("The button that loads the level (can still use RestartGame on click).")]
        public Button button;

        [Tooltip("Image that swaps between lock and unlock sprites.")]
        public Image stateImage;

        public Sprite lockedSprite;
        public Sprite unlockedSprite;

        [Tooltip("Optional lock icon/overlay shown while locked.")]
        public GameObject lockedOverlay;
    }

    [Serializable]
    private class AchievementPayload
    {
        public int totalLevelsCleared;
        public AchievementLevelData[] levels;
    }

    [Serializable]
    private class AchievementLevelData
    {
        public AchievementAttemptData[] attempts;
    }

    [Serializable]
    private class AchievementAttemptData
    {
        public string status;
    }

    [Header("References")]
    [SerializeField] private FirebaseManager firebaseManager;
    [SerializeField] private TMP_Text statusText;

    [Header("Entries")]
    [SerializeField] private List<LevelButtonEntry> levelButtons = new List<LevelButtonEntry>();

    [Header("Fetch")]
    [SerializeField] private string participantIdOverride = string.Empty;
    [SerializeField] private bool refreshOnStart = true;
    [SerializeField] private bool refreshOnEnable = true;
    [SerializeField] private float startupRefreshDelaySeconds = 0.25f;
    [SerializeField] private int startupRefreshRetryCount = 5;
    [SerializeField] private float startupRefreshRetryDelaySeconds = 1f;

    private Coroutine refreshRoutine;

    private void Reset()
    {
        firebaseManager = FindFirstObjectByType<FirebaseManager>();
    }

    private void Awake()
    {
        ResolveFirebaseManager();
        ApplyAllLocked("Enter User ID to unlock levels.");
    }

    private void OnEnable()
    {
        ResolveFirebaseManager();

        if (firebaseManager != null)
            firebaseManager.OnBridgeResponseReceived += OnFirebaseBridgeResponseReceived;

        if (GameSessionData.Instance != null)
            GameSessionData.Instance.ParticipantIdChanged += OnParticipantIdChanged;

        if (refreshOnEnable)
            StartRefreshSequence();
    }

    private void Start()
    {
        if (refreshOnStart)
            StartRefreshSequence();
    }

    private void OnDisable()
    {
        if (firebaseManager != null)
            firebaseManager.OnBridgeResponseReceived -= OnFirebaseBridgeResponseReceived;

        if (GameSessionData.Instance != null)
            GameSessionData.Instance.ParticipantIdChanged -= OnParticipantIdChanged;

        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }

    public void ForceRefreshLocks()
    {
        StartRefreshSequence();
    }

    private void StartRefreshSequence()
    {
        if (!isActiveAndEnabled)
            return;

        if (refreshRoutine != null)
            StopCoroutine(refreshRoutine);

        refreshRoutine = StartCoroutine(RefreshWhenReadyRoutine());
    }

    private IEnumerator RefreshWhenReadyRoutine()
    {
        if (startupRefreshDelaySeconds > 0f)
            yield return new WaitForSeconds(startupRefreshDelaySeconds);

        int attemptsRemaining = Mathf.Max(1, startupRefreshRetryCount);
        while (attemptsRemaining-- > 0)
        {
            ResolveFirebaseManager();
            string participantId = ResolveParticipantId();

            if (!string.IsNullOrWhiteSpace(participantId) && firebaseManager != null)
            {
                firebaseManager.RequestData(participantId);
                refreshRoutine = null;
                yield break;
            }

            ApplyAllLocked("Enter User ID to unlock levels.");
            yield return new WaitForSeconds(startupRefreshRetryDelaySeconds);
        }

        refreshRoutine = null;
    }

    private void OnParticipantIdChanged(string participantId)
    {
        if (string.IsNullOrWhiteSpace(participantId))
        {
            ApplyAllLocked("Enter User ID to unlock levels.");
            return;
        }

        StartRefreshSequence();
    }

    private void OnFirebaseBridgeResponseReceived(FirebaseManager.BridgeResponse response)
    {
        if (response == null)
            return;

        if (!string.Equals(response.type, "get", StringComparison.OrdinalIgnoreCase))
            return;

        if (string.Equals(response.status, "not_found", StringComparison.OrdinalIgnoreCase))
        {
            // New user: allow only entries that require zero cleared levels (typically level 1).
            ApplyByClearedLevels(0, hasParticipantId: true, "Level 1 unlocked. Complete levels to unlock the next ones.");
            return;
        }

        if (!string.Equals(response.status, "success", StringComparison.OrdinalIgnoreCase))
        {
            ApplyAllLocked("Could not load progress. Try again.");
            return;
        }

        if (string.IsNullOrWhiteSpace(response.payload))
        {
            ApplyByClearedLevels(0, hasParticipantId: true, "No progress data yet.");
            return;
        }

        AchievementPayload payload = null;
        try
        {
            payload = JsonUtility.FromJson<AchievementPayload>(response.payload);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TitleScreenLevelLockManager] Failed to parse payload: {ex.Message}");
        }

        if (payload == null)
        {
            ApplyByClearedLevels(0, hasParticipantId: true, "No progress data yet.");
            return;
        }

        int completedFromAttempts = CountCompletedLevels(payload.levels);
        int cleared = Mathf.Max(payload.totalLevelsCleared, completedFromAttempts);
        ApplyByClearedLevels(cleared, hasParticipantId: true, $"Unlocked using progress: {cleared} level(s) cleared.");
    }

    private void ApplyByClearedLevels(int clearedLevels, bool hasParticipantId, string message)
    {
        for (int i = 0; i < levelButtons.Count; i++)
        {
            LevelButtonEntry entry = levelButtons[i];
            if (entry == null)
                continue;

            bool unlocked = hasParticipantId && clearedLevels >= Mathf.Max(0, entry.requiredClearedLevels);
            ApplyEntryVisual(entry, unlocked);
        }

        UpdateStatus(message);
    }

    private void ApplyAllLocked(string message)
    {
        for (int i = 0; i < levelButtons.Count; i++)
        {
            LevelButtonEntry entry = levelButtons[i];
            if (entry == null)
                continue;

            ApplyEntryVisual(entry, false);
        }

        UpdateStatus(message);
    }

    private static int CountCompletedLevels(AchievementLevelData[] levels)
    {
        if (levels == null || levels.Length == 0)
            return 0;

        int completedLevelCount = 0;
        for (int i = 0; i < levels.Length; i++)
        {
            AchievementLevelData level = levels[i];
            if (level?.attempts == null)
                continue;

            bool hasCompletedAttempt = false;
            for (int j = 0; j < level.attempts.Length; j++)
            {
                AchievementAttemptData attempt = level.attempts[j];
                string status = (attempt?.status ?? string.Empty).Trim();
                if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    hasCompletedAttempt = true;
                    break;
                }
            }

            if (hasCompletedAttempt)
                completedLevelCount++;
        }

        return completedLevelCount;
    }

    private static void ApplyEntryVisual(LevelButtonEntry entry, bool unlocked)
    {
        if (entry.button != null)
            entry.button.interactable = unlocked;

        if (entry.stateImage != null)
        {
            Sprite target = unlocked ? entry.unlockedSprite : entry.lockedSprite;
            if (target != null)
                entry.stateImage.sprite = target;
        }

        if (entry.lockedOverlay != null)
            entry.lockedOverlay.SetActive(!unlocked);
    }

    private void ResolveFirebaseManager()
    {
        if (FirebaseManager.Instance != null)
        {
            firebaseManager = FirebaseManager.Instance;
            return;
        }

        if (firebaseManager == null)
            firebaseManager = FindFirstObjectByType<FirebaseManager>();
    }

    private string ResolveParticipantId()
    {
        string overrideId = (participantIdOverride ?? string.Empty).Trim();
        if (overrideId.Length > 0)
            return overrideId;

        if (GameSessionData.Instance != null && GameSessionData.Instance.TryGetParticipantId(out string sessionId))
            return sessionId.Trim();

        if (FirebaseTelemetryManager.Instance != null && !string.IsNullOrWhiteSpace(FirebaseTelemetryManager.Instance.CurrentParticipantID))
            return FirebaseTelemetryManager.Instance.CurrentParticipantID.Trim();

        return string.Empty;
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
