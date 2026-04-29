using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TutorialSequenceManager : MonoBehaviour
{
    [Serializable]
    public class TutorialStep
    {
        [Tooltip("Tutorial object to show for this step.")]
        public GameObject tutorialObject;

        [Tooltip("If checked, the vertical progress bar is shown while this step is active.")]
        public bool showVerticalProgressBar;
    }

    [Header("Tutorial Steps")]
    [SerializeField] private List<TutorialStep> steps = new();

    [Header("Progress Bar")]
    [SerializeField] private GamificationManager gamificationManager;

    [Header("Flow")]
    [SerializeField] private bool showFirstStepOnEnable = true;
    [SerializeField] private bool hideLastStepWhenComplete = true;
    [SerializeField] private bool disableManagerWhenComplete = true;

    [Header("Scene Transition")]
    [SerializeField] private bool loadSceneOnComplete = false;
#if UNITY_EDITOR
    [SerializeField] private SceneAsset nextSceneAsset;
#endif
    [SerializeField] private string nextSceneName;
    [SerializeField] private float delayBeforeSceneLoad = 0f;

    [SerializeField] private UnityEvent onSequenceCompleted;

    private int _currentStepIndex = -1;
    private bool _sequenceCompleted;
    private Coroutine _sceneLoadRoutine;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (nextSceneAsset != null)
            nextSceneName = nextSceneAsset.name;
    }
#endif

    private void Awake()
    {
        HideAllStepObjects();
    }

    private void OnEnable()
    {
        _sequenceCompleted = false;
        _currentStepIndex = -1;
        HideAllStepObjects();

        if (showFirstStepOnEnable)
            ShowStep(0);
    }

    private void Update()
    {
        if (_sequenceCompleted || steps == null || steps.Count == 0)
            return;

        if (WasAdvancePressed())
            AdvanceToNextStep();
    }

    public void AdvanceToNextStep()
    {
        if (_sequenceCompleted)
            return;

        if (_currentStepIndex < 0)
        {
            ShowStep(0);
            return;
        }

        int nextIndex = _currentStepIndex + 1;
        if (nextIndex >= steps.Count)
        {
            CompleteSequence();
            return;
        }

        ShowStep(nextIndex);
    }

    public void RestartSequence()
    {
        _sequenceCompleted = false;
        _currentStepIndex = -1;
        HideAllStepObjects();

        if (showFirstStepOnEnable)
            ShowStep(0);
    }

    private void ShowStep(int stepIndex)
    {
        if (steps == null || stepIndex < 0 || stepIndex >= steps.Count)
            return;

        HideAllStepObjects();
        _currentStepIndex = stepIndex;

        TutorialStep step = steps[stepIndex];
        if (step.tutorialObject != null)
            step.tutorialObject.SetActive(true);

        ApplyProgressBarState(step.showVerticalProgressBar);
    }

    private void CompleteSequence()
    {
        _sequenceCompleted = true;

        if (hideLastStepWhenComplete)
            HideAllStepObjects();

        onSequenceCompleted?.Invoke();

        if (loadSceneOnComplete && !string.IsNullOrWhiteSpace(nextSceneName))
        {
            if (_sceneLoadRoutine != null)
                StopCoroutine(_sceneLoadRoutine);

            _sceneLoadRoutine = StartCoroutine(LoadSceneAfterDelay());
        }

        if (disableManagerWhenComplete)
            enabled = false;
    }

    private void HideAllStepObjects()
    {
        if (steps == null)
            return;

        foreach (TutorialStep step in steps)
        {
            if (step?.tutorialObject != null)
                step.tutorialObject.SetActive(false);
        }
    }

    private void ApplyProgressBarState(bool shouldShow)
    {
        if (gamificationManager == null)
            gamificationManager = FindFirstObjectByType<GamificationManager>();

        if (gamificationManager != null)
            gamificationManager.SetProgressBarVisible(shouldShow);
    }

    private static bool WasAdvancePressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool keyboardPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool mousePressed = Mouse.current != null
                            && (Mouse.current.leftButton.wasPressedThisFrame
                                || Mouse.current.rightButton.wasPressedThisFrame
                                || Mouse.current.middleButton.wasPressedThisFrame);

        return keyboardPressed || mousePressed;
#else
        return Input.anyKeyDown
               || Input.GetMouseButtonDown(0)
               || Input.GetMouseButtonDown(1)
               || Input.GetMouseButtonDown(2);
#endif
    }

    private System.Collections.IEnumerator LoadSceneAfterDelay()
    {
        if (delayBeforeSceneLoad > 0f)
            yield return new WaitForSeconds(delayBeforeSceneLoad);

        SceneManager.LoadScene(nextSceneName);
    }
}
