using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FirebaseTelemetryManager : MonoBehaviour
{
    [Serializable]
    private class AttemptPayload
    {
        public string participantId;
        public string levelId;
        public string currentLevel;
        public string levelScene;
        public string status;
        public int durationSeconds;
        public int finalScore;
        public int mistakesMade;
        public int hintsUsed;
        public List<string> mistakeDetails;
        public List<string> hintDetails;
        public List<string> actionStream;
    }

    public static FirebaseTelemetryManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private FirebaseManager firebaseManager;

    [Header("Settings")]
    [SerializeField] private bool autoSubmitOnRestart = true;
    [SerializeField] private bool autoSubmitOnCompletion = true;
    [SerializeField] private float completedFallbackDelaySeconds = 6f;

    public string CurrentParticipantID => GetCurrentParticipantID();

    private string currentParticipantId = string.Empty;
    private string currentSceneName = string.Empty;
    private string currentLevelId = string.Empty;

    private float attemptStartTime;
    private int finalScore;
    private int mistakeCount;
    private int hintCount;
    private int durationSeconds;

    private bool attemptOpen;
    private bool subscribedToGameManager;
    private bool pendingCompletedSubmit;

    private Coroutine waitForGameManagerRoutine;
    private Coroutine completionFallbackRoutine;

    private GameManager subscribedGameManager;
    private GameSessionData gameSessionData;

    private readonly List<string> actionStream = new List<string>();
    private readonly List<string> mistakeDetails = new List<string>();
    private readonly List<string> hintDetails = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        var existing = FindFirstObjectByType<FirebaseTelemetryManager>();
        if (existing != null)
        {
            existing.InitializeInstance();
            return;
        }

        var go = new GameObject("FirebaseTelemetryManager");
        go.AddComponent<FirebaseTelemetryManager>();
    }

    private void Awake()
    {
        InitializeInstance();
        ResolveFirebaseManager();
        ResolveGameSessionData();
        SyncParticipantIdFromSession();
    }

    private void OnEnable()
    {
        ResolveFirebaseManager();
        ResolveGameSessionData();
        SyncParticipantIdFromSession();

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (firebaseManager != null)
            firebaseManager.OnBridgeResponseReceived += OnFirebaseBridgeResponseReceived;

        StartWaitingForCurrentGameManager();
        BeginAttemptForScene(SceneManager.GetActiveScene());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeFromGameManager();

        if (firebaseManager != null)
            firebaseManager.OnBridgeResponseReceived -= OnFirebaseBridgeResponseReceived;

        if (waitForGameManagerRoutine != null)
        {
            StopCoroutine(waitForGameManagerRoutine);
            waitForGameManagerRoutine = null;
        }

        if (completionFallbackRoutine != null)
        {
            StopCoroutine(completionFallbackRoutine);
            completionFallbackRoutine = null;
        }
    }

    private void InitializeInstance()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gameObject.name = "FirebaseTelemetryManager";

        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }

    private void ResolveFirebaseManager()
    {
        if (firebaseManager != null)
            return;

        firebaseManager = FirebaseManager.Instance;
        if (firebaseManager == null)
            firebaseManager = FindFirstObjectByType<FirebaseManager>();
    }

    private void ResolveGameSessionData()
    {
        if (gameSessionData != null)
            return;

        gameSessionData = GameSessionData.Instance;
        if (gameSessionData == null)
            gameSessionData = FindFirstObjectByType<GameSessionData>();
    }

    private void StartWaitingForCurrentGameManager()
    {
        if (waitForGameManagerRoutine != null)
            StopCoroutine(waitForGameManagerRoutine);

        waitForGameManagerRoutine = StartCoroutine(WaitForGameManagerThenSubscribe());
    }

    private IEnumerator WaitForGameManagerThenSubscribe()
    {
        while (GameManager.Instance == null)
            yield return null;

        SubscribeToCurrentGameManager();
        waitForGameManagerRoutine = null;
    }

    private void SubscribeToCurrentGameManager()
    {
        var currentGameManager = GameManager.Instance;
        if (currentGameManager == null)
            return;

        if (subscribedToGameManager && ReferenceEquals(subscribedGameManager, currentGameManager))
            return;

        UnsubscribeFromGameManager();

        currentGameManager.OnActionRecorded -= RecordEvent;
        currentGameManager.OnActionRecorded += RecordEvent;

        subscribedGameManager = currentGameManager;
        subscribedToGameManager = true;

        Debug.Log($"[FirebaseTelemetryManager] Bound to GameManager in scene '{SceneManager.GetActiveScene().name}'.");
    }

    private void UnsubscribeFromGameManager()
    {
        if (!subscribedToGameManager)
            return;

        if (subscribedGameManager != null)
            subscribedGameManager.OnActionRecorded -= RecordEvent;

        subscribedGameManager = null;
        subscribedToGameManager = false;
    }

    private void SyncParticipantIdFromSession()
    {
        ResolveGameSessionData();

        if (gameSessionData != null && gameSessionData.TryGetParticipantId(out var sessionParticipantId))
            currentParticipantId = sessionParticipantId.Trim();
    }

    private string GetCurrentParticipantID()
    {
        if (string.IsNullOrWhiteSpace(currentParticipantId))
            SyncParticipantIdFromSession();

        return currentParticipantId;
    }

    public void SetCurrentParticipantID(string participantId)
    {
        var normalizedParticipantId = (participantId ?? string.Empty).Trim();
        if (normalizedParticipantId.Length == 0)
            return;

        currentParticipantId = normalizedParticipantId;

        ResolveGameSessionData();
        gameSessionData?.SetParticipantId(normalizedParticipantId);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SyncParticipantIdFromSession();
        StartWaitingForCurrentGameManager();

        if (attemptOpen && HasAttemptData() && !string.Equals(currentSceneName, scene.name, StringComparison.Ordinal))
        {
            if (pendingCompletedSubmit)
                SubmitLevelData("Completed");
            else if (autoSubmitOnRestart)
                SubmitLevelData("Abandoned");
        }

        BeginAttemptForScene(scene);
    }

    private void BeginAttemptForScene(Scene scene)
    {
        if (!scene.IsValid())
            return;

        currentSceneName = scene.name;
        currentLevelId = BuildLevelId(scene.name);
        attemptStartTime = Time.time;
        finalScore = 0;
        mistakeCount = 0;
        hintCount = 0;
        durationSeconds = 0;
        pendingCompletedSubmit = false;
        attemptOpen = true;

        actionStream.Clear();
        mistakeDetails.Clear();
        hintDetails.Clear();

        if (completionFallbackRoutine != null)
        {
            StopCoroutine(completionFallbackRoutine);
            completionFallbackRoutine = null;
        }
    }

    public void RecordEvent(string rawEvent)
    {
        var id = (rawEvent ?? string.Empty).Trim();
        if (id.Length == 0)
            return;

        if (id.StartsWith("user_id_submitted:", StringComparison.OrdinalIgnoreCase))
            SetCurrentParticipantID(ExtractValue(id));

        if (id.StartsWith("telemetry_firebase_", StringComparison.OrdinalIgnoreCase))
            return;

        if (attemptOpen)
            actionStream.Add(id);

        ParseEvent(id);
    }

    private void ParseEvent(string id)
    {
        if (id.StartsWith("telemetry_level_wrong_action:", StringComparison.OrdinalIgnoreCase))
        {
            mistakeCount++;
            var detail = ExtractValue(id);
            if (!string.IsNullOrEmpty(detail))
                mistakeDetails.Add(detail);
            return;
        }

        if (id.StartsWith("hint_requested", StringComparison.OrdinalIgnoreCase))
        {
            hintCount++;
            var detail = ExtractValue(id);
            hintDetails.Add(string.IsNullOrEmpty(detail) ? "hint_requested" : detail);
            return;
        }

        if (id.StartsWith("telemetry_level_score:", StringComparison.OrdinalIgnoreCase))
        {
            finalScore = ParseIntValue(id, finalScore);
            return;
        }

        if (id.StartsWith("telemetry_level_time_seconds:", StringComparison.OrdinalIgnoreCase))
        {
            durationSeconds = ParseIntValue(id, durationSeconds);
            return;
        }

        if (id.StartsWith("telemetry_level_mistakes:", StringComparison.OrdinalIgnoreCase))
        {
            mistakeCount = ParseIntValue(id, mistakeCount);
            return;
        }

        if (id.StartsWith("telemetry_level_hints:", StringComparison.OrdinalIgnoreCase))
        {
            hintCount = ParseIntValue(id, hintCount);
            return;
        }

        if (id.StartsWith("telemetry_level_scene:", StringComparison.OrdinalIgnoreCase))
        {
            var sceneName = ExtractValue(id);
            if (!string.IsNullOrEmpty(sceneName))
                currentSceneName = sceneName;
            return;
        }

        if (id.Equals("telemetry_level_completed", StringComparison.OrdinalIgnoreCase))
        {
            pendingCompletedSubmit = true;

            if (autoSubmitOnCompletion)
            {
                if (completionFallbackRoutine != null)
                    StopCoroutine(completionFallbackRoutine);
                completionFallbackRoutine = StartCoroutine(CompletionFallbackRoutine());
            }

            return;
        }

        if (id.StartsWith("telemetry_level_transition_scene:", StringComparison.OrdinalIgnoreCase))
        {
            if (pendingCompletedSubmit)
                SubmitLevelData("Completed");
            return;
        }

        if (id.Equals("ui_restart_pressed", StringComparison.OrdinalIgnoreCase) && autoSubmitOnRestart)
            SubmitLevelData("Abandoned");
    }

    private IEnumerator CompletionFallbackRoutine()
    {
        yield return new WaitForSeconds(completedFallbackDelaySeconds);

        if (pendingCompletedSubmit)
            SubmitLevelData("Completed");

        completionFallbackRoutine = null;
    }

    public void SubmitLevelData(string status)
    {
        if (!attemptOpen)
            return;

        if (!HasAttemptData())
        {
            attemptOpen = false;
            return;
        }

        var participantId = GetCurrentParticipantID();
        if (string.IsNullOrWhiteSpace(participantId))
        {
            Debug.LogWarning("[FirebaseTelemetryManager] No participant ID available. Attempt data was not uploaded.");
            attemptOpen = false;
            return;
        }

        ResolveFirebaseManager();
        if (firebaseManager == null)
        {
            Debug.LogWarning("[FirebaseTelemetryManager] FirebaseManager was not found. Attempt data was not uploaded.");
            attemptOpen = false;
            return;
        }

        var payload = new AttemptPayload
        {
            participantId = participantId,
            levelId = currentLevelId,
            currentLevel = currentSceneName,
            levelScene = currentSceneName,
            status = string.IsNullOrWhiteSpace(status) ? "Abandoned" : status,
            durationSeconds = durationSeconds > 0 ? durationSeconds : Mathf.Max(0, Mathf.RoundToInt(Time.time - attemptStartTime)),
            finalScore = finalScore,
            mistakesMade = mistakeCount,
            hintsUsed = hintCount,
            mistakeDetails = new List<string>(mistakeDetails),
            hintDetails = new List<string>(hintDetails),
            actionStream = new List<string>(actionStream)
        };

        Debug.Log($"[FirebaseTelemetryManager] Submitting {payload.status} attempt for {payload.participantId} / {payload.levelId}.");

        attemptOpen = false;
        pendingCompletedSubmit = false;

        if (completionFallbackRoutine != null)
        {
            StopCoroutine(completionFallbackRoutine);
            completionFallbackRoutine = null;
        }

        firebaseManager.UploadLevelAttempt(JsonUtility.ToJson(payload));
    }

    private void OnFirebaseBridgeResponseReceived(FirebaseManager.BridgeResponse response)
    {
        if (response == null)
            return;

        if (!string.Equals(response.type, "attempt", StringComparison.OrdinalIgnoreCase))
            return;

        if (string.Equals(response.status, "success", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[FirebaseTelemetryManager] Attempt upload succeeded for {response.participantId} / {response.levelId} / {response.documentId}.");
            return;
        }

        Debug.LogWarning($"[FirebaseTelemetryManager] Attempt upload failed: {response.message}");
    }

    private bool HasAttemptData()
    {
        return actionStream.Count > 0
            || mistakeDetails.Count > 0
            || hintDetails.Count > 0
            || finalScore != 0
            || mistakeCount != 0
            || hintCount != 0;
    }

    private static string BuildLevelId(string sceneName)
    {
        var trimmed = (sceneName ?? string.Empty).Trim();
        var match = Regex.Match(trimmed, "(\\d+)");
        if (match.Success)
            return $"level_{match.Groups[1].Value}";

        var lowered = trimmed.ToLowerInvariant();
        lowered = Regex.Replace(lowered, "[^a-z0-9]+", "_").Trim('_');
        return string.IsNullOrEmpty(lowered) ? "level_unknown" : $"level_{lowered}";
    }

    private static string ExtractValue(string actionId)
    {
        var index = actionId.IndexOf(':');
        if (index < 0 || index >= actionId.Length - 1)
            return string.Empty;

        return actionId.Substring(index + 1).Trim();
    }

    private static int ParseIntValue(string actionId, int fallback)
    {
        var value = ExtractValue(actionId);
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
