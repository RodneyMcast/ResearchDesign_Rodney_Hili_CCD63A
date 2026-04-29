using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    [Serializable]
    public class BridgeResponse
    {
        public string type;
        public string status;
        public string documentId;
        public string participantId;
        public string levelId;
        public string payload;
        public string message;
    }

    [Serializable]
    private class UserSavePayload
    {
        public string id;
        public string participant_id;
    }

    [Serializable]
    private class AttemptSavePayload
    {
        public string participantId;
    }

    public static FirebaseManager Instance { get; private set; }

    public event Action<BridgeResponse> OnBridgeResponseReceived;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SavePlayerData(string json);

    [DllImport("__Internal")]
    private static extern void GetPlayerData(string playerId);

    [DllImport("__Internal")]
    private static extern void SubmitLevelAttempt(string json);
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        var existing = FindFirstObjectByType<FirebaseManager>();
        if (existing != null)
        {
            existing.InitializeInstance();
            return;
        }

        var go = new GameObject("FirebaseManager");
        go.AddComponent<FirebaseManager>();
    }

    private void Awake()
    {
        InitializeInstance();
    }

    private void InitializeInstance()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("[FirebaseManager] Duplicate instance detected. Destroying only the duplicate FirebaseManager component.");
            Destroy(this);
            return;
        }

        Instance = this;
        gameObject.name = "FirebaseManager";

        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }

    public void UploadData(string jsonData)
    {
        var payload = string.IsNullOrWhiteSpace(jsonData) ? "{}" : jsonData;
        if (!TryGetParticipantIdForUserSave(payload, out _))
        {
            Debug.LogWarning("[FirebaseManager] Skipping user save because the participant ID is empty.");
            return;
        }

        GameManager.Instance?.RecordAction("telemetry_firebase_user_save_requested");

#if UNITY_WEBGL && !UNITY_EDITOR
        SavePlayerData(payload);
#else
        Debug.Log($"[FirebaseManager] WebGL user save requested: {payload}");
#endif
    }

    public void RequestData(string id)
    {
        var playerId = (id ?? string.Empty).Trim();
        if (playerId.Length == 0)
        {
            Debug.LogWarning("[FirebaseManager] RequestData called with an empty player ID.");
            return;
        }

        GameManager.Instance?.RecordAction($"telemetry_firebase_user_get_requested:{playerId}");

#if UNITY_WEBGL && !UNITY_EDITOR
        GetPlayerData(playerId);
#else
        Debug.Log($"[FirebaseManager] WebGL user get requested for: {playerId}");
#endif
    }

    public void UploadLevelAttempt(string jsonData)
    {
        var payload = string.IsNullOrWhiteSpace(jsonData) ? "{}" : jsonData;
        if (!TryGetParticipantIdForAttemptSave(payload, out _))
        {
            Debug.LogWarning("[FirebaseManager] Skipping attempt save because the participant ID is empty.");
            return;
        }

        GameManager.Instance?.RecordAction("telemetry_firebase_attempt_save_requested");

#if UNITY_WEBGL && !UNITY_EDITOR
        SubmitLevelAttempt(payload);
#else
        Debug.Log($"[FirebaseManager] WebGL attempt save requested: {payload}");
#endif
    }

    public void ReceiveFromJS(string data)
    {
        var raw = string.IsNullOrWhiteSpace(data) ? "{}" : data;
        Debug.Log($"[FirebaseManager] Received from JS: {raw}");

        BridgeResponse response = null;
        try
        {
            response = JsonUtility.FromJson<BridgeResponse>(raw);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FirebaseManager] Failed to parse bridge response: {ex.Message}");
        }

        if (response == null)
        {
            response = new BridgeResponse
            {
                type = "unknown",
                status = "raw",
                payload = raw,
                message = "Unparsed bridge response"
            };
        }

        var type = string.IsNullOrWhiteSpace(response.type) ? "unknown" : response.type.Trim();
        var status = string.IsNullOrWhiteSpace(response.status) ? "unknown" : response.status.Trim();
        var documentId = string.IsNullOrWhiteSpace(response.documentId) ? "none" : response.documentId.Trim();
        GameManager.Instance?.RecordAction($"telemetry_firebase_{type}_{status}:{documentId}");

        OnBridgeResponseReceived?.Invoke(response);
    }

    private static bool TryGetParticipantIdForUserSave(string jsonData, out string participantId)
    {
        participantId = string.Empty;

        try
        {
            var payload = JsonUtility.FromJson<UserSavePayload>(jsonData);
            participantId = (payload?.participant_id ?? payload?.id ?? string.Empty).Trim();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FirebaseManager] Failed to read the user save payload: {ex.Message}");
            return false;
        }

        return participantId.Length > 0;
    }

    private static bool TryGetParticipantIdForAttemptSave(string jsonData, out string participantId)
    {
        participantId = string.Empty;

        try
        {
            var payload = JsonUtility.FromJson<AttemptSavePayload>(jsonData);
            participantId = (payload?.participantId ?? string.Empty).Trim();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FirebaseManager] Failed to read the attempt save payload: {ex.Message}");
            return false;
        }

        return participantId.Length > 0;
    }
}
