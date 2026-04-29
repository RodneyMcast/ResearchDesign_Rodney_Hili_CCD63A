using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AchievementManager : MonoBehaviour
{
    public enum AchievementType
    {
        LevelScore,
        GameCompleted
    }

    [Serializable]
    public class AchievementEntry
    {
        public string label;
        public AchievementType achievementType = AchievementType.LevelScore;
        public string levelId = "level_1";
        public int requiredScore = 0;
        public GameObject achievementObject;
        public TMP_Text statsText;
    }

    [Serializable]
    private class AchievementPayload
    {
        public string participantId;
        public string currentLevel;
        public int totalLevelsCleared;
        public bool gameCompleted;
        public AchievementLevelData[] levels;
    }

    [Serializable]
    private class AchievementLevelData
    {
        public string levelId;
        public AchievementAttemptData[] attempts;
    }

    [Serializable]
    private class AchievementAttemptData
    {
        public string attemptId;
        public string status;
        public int finalScore;
        public int durationSeconds;
        public int hintsUsed;
        public int mistakesMade;
    }

    public static AchievementManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private FirebaseManager firebaseManager;

    [Header("Fetch")]
    [SerializeField] private string participantIdOverride = string.Empty;
    [SerializeField] private bool refreshOnStart = true;
    [SerializeField] private bool refreshOnEnable;
    [SerializeField] private bool refreshOnSceneLoad = true;
    [SerializeField] private bool refreshAfterFirebaseSave = true;
    [SerializeField] private float startupRefreshDelaySeconds = 0.5f;
    [SerializeField] private int startupRefreshRetryCount = 5;
    [SerializeField] private float startupRefreshRetryDelaySeconds = 1f;

    [Header("Display")]
    [SerializeField] private string lockedText = "N/A";
    [SerializeField] private string gameCompletedText = "Game Cleared";
    [SerializeField] private List<AchievementEntry> achievements = new List<AchievementEntry>();

    private Coroutine refreshRoutine;

    private void Reset()
    {
        firebaseManager = FindFirstObjectByType<FirebaseManager>();
    }

    private void Awake()
    {
        Instance = this;
        ResolveFirebaseManager();
        ApplyLockedState();
        Debug.Log($"[AchievementManager] Awake on '{gameObject.name}'. FirebaseManager found: {firebaseManager != null}");
    }

    private void OnEnable()
    {
        Instance = this;
        ResolveFirebaseManager();

        if (firebaseManager != null)
            firebaseManager.OnBridgeResponseReceived += OnFirebaseBridgeResponseReceived;

        if (GameSessionData.Instance != null)
            GameSessionData.Instance.ParticipantIdChanged += OnParticipantIdChanged;

        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log($"[AchievementManager] Enabled on '{gameObject.name}'. Participant ID available: {!string.IsNullOrWhiteSpace(ResolveParticipantId())}");

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

        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }

        if (ReferenceEquals(Instance, this))
            Instance = null;
    }

    public void RefreshAchievements()
    {
        ResolveFirebaseManager();
        if (firebaseManager == null)
        {
            Debug.LogWarning("[AchievementManager] FirebaseManager was not found. Achievements could not be refreshed.");
            ApplyLockedState();
            return;
        }

        var participantId = ResolveParticipantId();
        if (string.IsNullOrWhiteSpace(participantId))
        {
            Debug.LogWarning("[AchievementManager] No participant ID is available. Achievements were reset to locked.");
            ApplyLockedState();
            return;
        }

        Debug.Log($"[AchievementManager] Requesting achievement data for participant '{participantId}'.");
        firebaseManager.RequestData(participantId);
    }

    public void ForceRefresh()
    {
        Debug.Log("[AchievementManager] ForceRefresh called.");
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

        var attemptsRemaining = Mathf.Max(1, startupRefreshRetryCount);
        while (attemptsRemaining-- > 0)
        {
            ResolveFirebaseManager();
            var participantId = ResolveParticipantId();

            if (firebaseManager != null && !string.IsNullOrWhiteSpace(participantId))
            {
                RefreshAchievements();
                refreshRoutine = null;
                yield break;
            }

            yield return new WaitForSeconds(startupRefreshRetryDelaySeconds);
        }

        Debug.LogWarning("[AchievementManager] Timed out waiting for FirebaseManager or participant ID.");
        ApplyLockedState();
        refreshRoutine = null;
    }

    private void OnParticipantIdChanged(string participantId)
    {
        if (!string.IsNullOrWhiteSpace(participantId))
            StartRefreshSequence();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (refreshOnSceneLoad)
            StartRefreshSequence();
    }

    private void OnFirebaseBridgeResponseReceived(FirebaseManager.BridgeResponse response)
    {
        if (response == null)
            return;

        if (refreshAfterFirebaseSave
            && string.Equals(response.status, "success", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(response.type, "save", StringComparison.OrdinalIgnoreCase)
                || string.Equals(response.type, "attempt", StringComparison.OrdinalIgnoreCase)))
        {
            StartRefreshSequence();
            return;
        }

        if (!string.Equals(response.type, "get", StringComparison.OrdinalIgnoreCase))
            return;

        if (string.Equals(response.status, "not_found", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("[AchievementManager] No Firebase user record was found for this participant ID.");
            ApplyLockedState();
            return;
        }

        if (!string.Equals(response.status, "success", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"[AchievementManager] Achievement fetch failed: {response.message}");
            ApplyLockedState();
            return;
        }

        if (string.IsNullOrWhiteSpace(response.payload))
        {
            Debug.LogWarning("[AchievementManager] Firebase returned an empty achievements payload.");
            ApplyLockedState();
            return;
        }

        AchievementPayload payload;
        try
        {
            payload = JsonUtility.FromJson<AchievementPayload>(response.payload);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AchievementManager] Failed to parse achievement payload: {ex.Message}");
            ApplyLockedState();
            return;
        }

        if (payload == null)
        {
            ApplyLockedState();
            return;
        }

        Debug.Log($"[AchievementManager] Achievement payload received for '{payload.participantId}' with {payload.totalLevelsCleared} cleared levels.");
        ApplyAchievementState(payload);
    }

    private void ApplyAchievementState(AchievementPayload payload)
    {
        foreach (var entry in achievements)
        {
            if (entry == null)
                continue;

            if (entry.achievementType == AchievementType.GameCompleted)
            {
                var unlocked = payload.gameCompleted || payload.totalLevelsCleared >= 4;
                ApplyEntry(entry, unlocked, unlocked ? gameCompletedText : GetNotDoneText(entry));
                continue;
            }

            var normalizedLevelId = NormalizeLevelId(entry.levelId);
            var bestAttempt = FindBestCompletedAttempt(payload, normalizedLevelId);
            var hasCompletedAttempt = bestAttempt != null;
            var unlockedForScore = hasCompletedAttempt && bestAttempt.finalScore >= entry.requiredScore;
            var text = hasCompletedAttempt ? FormatAttempt(bestAttempt) : GetNotDoneText(entry);

            ApplyEntry(entry, unlockedForScore, text);
        }
    }

    private void ApplyEntry(AchievementEntry entry, bool unlocked, string text)
    {
        if (entry.achievementObject != null)
            entry.achievementObject.SetActive(unlocked);

        if (entry.statsText != null)
            entry.statsText.text = string.IsNullOrWhiteSpace(text) ? lockedText : text;
    }

    private void ApplyLockedState()
    {
        foreach (var entry in achievements)
        {
            if (entry == null)
                continue;

            ApplyEntry(entry, false, GetNotDoneText(entry));
        }
    }

    private string GetNotDoneText(AchievementEntry entry)
    {
        var labelText = (entry?.label ?? string.Empty).Trim();
        if (labelText.Length > 0)
            return $"{labelText} not done";

        return string.IsNullOrWhiteSpace(lockedText) ? "Not done" : lockedText;
    }

    private AchievementAttemptData FindBestCompletedAttempt(AchievementPayload payload, string levelId)
    {
        if (payload?.levels == null || string.IsNullOrWhiteSpace(levelId))
            return null;

        AchievementAttemptData bestAttempt = null;
        foreach (var level in payload.levels)
        {
            var currentLevelId = NormalizeLevelId(level?.levelId);
            if (string.IsNullOrWhiteSpace(currentLevelId) || !string.Equals(currentLevelId, levelId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (level.attempts == null)
                continue;

            foreach (var attempt in level.attempts)
            {
                if (attempt == null)
                    continue;

                if (!string.Equals((attempt.status ?? string.Empty).Trim(), "Completed", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (bestAttempt == null || IsBetterAttempt(attempt, bestAttempt))
                    bestAttempt = attempt;
            }
        }

        return bestAttempt;
    }

    private static bool IsBetterAttempt(AchievementAttemptData candidate, AchievementAttemptData currentBest)
    {
        if (candidate.finalScore != currentBest.finalScore)
            return candidate.finalScore > currentBest.finalScore;

        if (candidate.durationSeconds != currentBest.durationSeconds)
            return candidate.durationSeconds < currentBest.durationSeconds;

        if (candidate.hintsUsed != currentBest.hintsUsed)
            return candidate.hintsUsed < currentBest.hintsUsed;

        return candidate.mistakesMade < currentBest.mistakesMade;
    }

    private string ResolveParticipantId()
    {
        var overrideId = (participantIdOverride ?? string.Empty).Trim();
        if (overrideId.Length > 0)
            return overrideId;

        if (GameSessionData.Instance != null && GameSessionData.Instance.TryGetParticipantId(out var sessionId))
            return sessionId.Trim();

        if (FirebaseTelemetryManager.Instance != null && !string.IsNullOrWhiteSpace(FirebaseTelemetryManager.Instance.CurrentParticipantID))
            return FirebaseTelemetryManager.Instance.CurrentParticipantID.Trim();

        return string.Empty;
    }

    private void ResolveFirebaseManager()
    {
        var liveInstance = FirebaseManager.Instance;
        if (liveInstance != null)
        {
            firebaseManager = liveInstance;
            return;
        }

        if (firebaseManager != null)
            return;

        firebaseManager = FindFirstObjectByType<FirebaseManager>();
    }

    private static string NormalizeLevelId(string levelId)
    {
        var trimmed = (levelId ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        var match = Regex.Match(trimmed, "(\\d+)");
        if (match.Success)
            return $"level_{match.Groups[1].Value}";

        return trimmed.Replace(" ", "_").ToLowerInvariant();
    }

    private static string FormatAttempt(AchievementAttemptData attempt)
    {
        return $"Points: {attempt.finalScore} | Time: {attempt.durationSeconds} | Hints used: {attempt.hintsUsed} | Mistakes: {attempt.mistakesMade}";
    }
}
