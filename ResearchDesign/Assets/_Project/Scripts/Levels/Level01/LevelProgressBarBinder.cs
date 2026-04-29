using System;
using System.Reflection;
using UnityEngine;

public class LevelProgressBarBinder : MonoBehaviour
{
    [SerializeField] private LevelObjectiveTracker tracker;
    [SerializeField] private MonoBehaviour progressBarBehaviour;

    private MethodInfo _setProgressMethod;

    private void Awake()
    {
        CacheSetProgressMethod();
    }

    private void OnEnable()
    {
        if (tracker != null)
            tracker.OnProgressChanged += OnProgressChanged;

        PushCurrent();
    }

    private void OnDisable()
    {
        if (tracker != null)
            tracker.OnProgressChanged -= OnProgressChanged;
    }

    private void CacheSetProgressMethod()
    {
        _setProgressMethod = null;

        if (progressBarBehaviour == null) return;

        var t = progressBarBehaviour.GetType();
        _setProgressMethod = t.GetMethod(
            "SetProgress",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(float) },
            null
        );
    }

    private void OnProgressChanged(int current, int total)
    {
        float p = (total <= 0) ? 0f : Mathf.Clamp01((float)current / total);
        SetBar(p);
    }

    private void PushCurrent()
    {
        if (tracker == null) return;
        SetBar(tracker.GetProgress01());
    }

    private void SetBar(float progress01)
    {
        if (progressBarBehaviour == null)
        {
            Debug.LogWarning("[LevelProgressBarBinder] progressBarBehaviour is not assigned.");
            return;
        }

        if (_setProgressMethod == null)
            CacheSetProgressMethod();

        if (_setProgressMethod == null)
        {
            Debug.LogWarning($"[LevelProgressBarBinder] '{progressBarBehaviour.GetType().Name}' has no SetProgress(float).");
            return;
        }

        _setProgressMethod.Invoke(progressBarBehaviour, new object[] { progress01 });
    }
}