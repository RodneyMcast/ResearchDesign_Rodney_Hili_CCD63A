using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<string> OnActionRecorded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);

        Debug.Log("[GameManager] Awake -> Instance set");
    }

    public void RecordAction(string actionId)
    {
        var id = (actionId ?? "").Trim();
        Debug.Log($"[GameManager] Action recorded: {id}");
        OnActionRecorded?.Invoke(id);
    }
}