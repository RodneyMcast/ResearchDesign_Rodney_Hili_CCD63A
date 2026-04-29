using System;
using UnityEngine;

public class GameSessionData : MonoBehaviour
{
    private const string DefaultPlayerPrefsKey = "agentic_browser_user_id";

    public static GameSessionData Instance { get; private set; }

    [SerializeField] private string playerPrefsKey = DefaultPlayerPrefsKey;
    [SerializeField] private bool mirrorToPlayerPrefs = true;

    private string currentParticipantId = string.Empty;

    public string CurrentParticipantId => currentParticipantId;

    public bool HasParticipantId => !string.IsNullOrWhiteSpace(currentParticipantId);

    public event Action<string> ParticipantIdChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        var existing = FindFirstObjectByType<GameSessionData>();
        if (existing != null)
        {
            existing.InitializeInstance();
            return;
        }

        var go = new GameObject("GameSessionData");
        go.AddComponent<GameSessionData>();
    }

    private void Awake()
    {
        InitializeInstance();
    }

    private void InitializeInstance()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gameObject.name = "GameSessionData";
        DontDestroyOnLoad(gameObject);
    }

    public void SetParticipantId(string participantId)
    {
        var normalizedParticipantId = (participantId ?? string.Empty).Trim();
        if (normalizedParticipantId.Length == 0)
            return;

        var changed = !string.Equals(currentParticipantId, normalizedParticipantId, StringComparison.Ordinal);
        currentParticipantId = normalizedParticipantId;

        if (mirrorToPlayerPrefs)
        {
            PlayerPrefs.SetString(playerPrefsKey, currentParticipantId);
            PlayerPrefs.Save();
        }

        if (changed)
        {
            Debug.Log($"[GameSessionData] Participant ID set to: {currentParticipantId}");
            ParticipantIdChanged?.Invoke(currentParticipantId);
        }
    }

    public bool TryGetParticipantId(out string participantId)
    {
        participantId = currentParticipantId;
        return !string.IsNullOrWhiteSpace(participantId);
    }
}
