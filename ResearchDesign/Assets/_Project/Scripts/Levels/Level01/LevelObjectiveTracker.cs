using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelObjectiveTracker : MonoBehaviour
{
    private const string TelemetryPrefix = "telemetry_";

    [Serializable]
    public class Step
    {
        public string actionId;
        [Tooltip("When checked, this step can only be completed if the step directly before it has already been completed. When unchecked, this step can be triggered at any point regardless of order.")]
        public bool requiresPreviousStep = false;
        [Tooltip("If enabled, completing this step will load the selected screen state.")]
        public bool changeStateOnComplete = false;
        [Tooltip("State to load when this step completes (only used when Change State On Complete is enabled).")]
        public ScreenState stateToLoad;
        [Tooltip("If true, current state is pushed to history before loading the next state.")]
        public bool pushLoadedStateToHistory = true;
        [TextArea] public string hintText;
        [TextArea] public string successText;
        [Tooltip("Optional. Animated helper object to show when the player is stuck on this step.")]
        public GameObject stuckStepGuideObject;
    }

    [Header("Level Steps (in order)")]
    public List<Step> steps = new();

    [Header("Win Conditions")]
    [Tooltip("If this specific action is recorded, the user wins immediately regardless of their current step.")]
    public string winActionId = "MasterWin";
    [Tooltip("Alternative easter egg actions that also instantly win the level and fill the progress bar.")]
    public List<string> alternativeWinActions = new List<string>();
    public UnityEvent onWin;

    [Header("Level Transition (on Win)")]
    [Tooltip("Wait duration (seconds) before transitioning to next scene.")]
    [SerializeField] private float delayBeforeTransition = 5f;
#if UNITY_EDITOR
    [Tooltip("Optional: drag a Scene asset here. If assigned, level will transition to this scene after win delay.")]
    [SerializeField] private SceneAsset nextSceneAsset;
#endif
    [Tooltip("Runtime scene name. Auto-filled from Scene asset in editor.")]
    [SerializeField] private string nextSceneName;

    [Header("UI References")]
    public RobotTextManager textManager;

    [Header("Stats & Scoring")]
    [Tooltip("Optional. Drag the LevelStatsManager here to enable time, mistake, and hint tracking.")]
    [SerializeField] private LevelStatsManager statsManager;

    [Header("Idle Hint Prompt")]
    [SerializeField] private float idleHintDelaySeconds = 6f;
    [SerializeField] private string idleHintMessage = "If you are stuck click me to get a hint.";

    [Header("Intro Message")]
    [Tooltip("How long the welcome message stays locked before automatic messages are allowed to replace it.")]
    [SerializeField] private float introMessageHoldSeconds = 3f;

    [Header("Step Guide")]
    [Tooltip("Shows the current step's helper object after the player has been on that step for this many seconds.")]
    [SerializeField] private float stuckStepGuideDelaySeconds = 20f;
    [Tooltip("How long the current step's helper object stays visible before hiding again.")]
    [SerializeField] private float stuckStepGuideVisibleSeconds = 5f;

    [Header("Hint Button Request Animation")]
    [Tooltip("Optional. This helper object is shown after the player has gone this long without completing a milestone.")]
    [FormerlySerializedAs("milestoneIdleGuideObject")]
    [SerializeField] private GameObject hintButtonRequestAnimationObject;
    [Tooltip("How many seconds since the last completed milestone before the idle helper object is shown.")]
    [FormerlySerializedAs("milestoneIdleGuideDelaySeconds")]
    [SerializeField] private float hintButtonRequestAnimationDelaySeconds = 20f;
    [Tooltip("How long the hint button request animation stays visible before hiding automatically.")]
    [SerializeField] private float hintButtonRequestAnimationVisibleSeconds = 10f;

    [Header("Step Effects")]
    [SerializeField] private GameObject stepEffectObject;
    [SerializeField] private bool hideStepEffectWhenIdle = true;
    [SerializeField] private bool autoHideUsingAnimatorState = true;
    [SerializeField] private string stepEffectAnimationStateName = "FireWORK ANIMATION";
    [SerializeField] private float stepEffectAutoHideTimeout = 3f;
    [SerializeField] private float stepEffectVisibleDuration = 1.2f;
    [SerializeField] private Animator stepEffectAnimator;
    [SerializeField] private string stepEffectParameterName = "firework";
    [SerializeField] private bool stepEffectUsesBoolParameter = true;
    [SerializeField] private float stepEffectBoolResetDelay = 0.15f;
    [SerializeField] private float stepEffectWaitForEnterTimeout = 0.5f;
    [SerializeField] private float stepEffectMinResetDelayAfterEnter = 0.1f;
    [SerializeField] private float stepEffectWaitForCompletionTimeout = 3f;
    [SerializeField] private AudioSource stepEffectAudioSource;
    [SerializeField] private AudioClip stepCompletedSound;
    [SerializeField] private float stepCompletedSoundDelay = 0.35f;

    [Header("Win Fireworks Effect")]
    [SerializeField] private GameObject winFireworksEffectObject;
    [SerializeField] private bool hideWinFireworksWhenIdle = true;
    [SerializeField] private bool autoHideWinFireworksUsingAnimatorState = true;
    [SerializeField] private string winFireworksAnimationStateName = "FireWORK ANIMATION";
    [SerializeField] private float winFireworksAutoHideTimeout = 3f;
    [SerializeField] private float winFireworksVisibleDuration = 1.5f;
    [SerializeField] private Animator winFireworksAnimator;
    [SerializeField] private string winFireworksParameterName = "firework";
    [SerializeField] private bool winFireworksUsesBoolParameter = true;
    [SerializeField] private float winFireworksBoolResetDelay = 0.15f;
    [SerializeField] private float winFireworksWaitForEnterTimeout = 0.5f;
    [SerializeField] private float winFireworksMinResetDelayAfterEnter = 0.1f;
    [SerializeField] private float winFireworksWaitForCompletionTimeout = 3f;
    [SerializeField] private AudioSource winFireworksAudioSource;
    [SerializeField] private AudioClip winFireworksSound;
    [SerializeField] private float winFireworksSoundDelay = 0.5f;
    [SerializeField] private float winFireworksRiseHeight = 100f;
    [SerializeField] private float winFireworksRiseSpeed = 250f;

    public event Action<string> OnStepCompleted;

    private int _hintCount = 0;
    private int _mistakeStreak = 0;

    [Header("Debug")]
    public bool logProgress = true;

    [Header("Optional")]
    public bool resetProgressOnEnable = true;

    public int CurrentStepIndex => _currentStepIndex;
    public int TotalSteps => steps == null ? 0 : steps.Count;

    public float NormalizedProgress => TotalSteps <= 0 ? 0f : Mathf.Clamp01((float)_currentStepIndex / TotalSteps);
    public float GetProgress01() => NormalizedProgress;

    public string GetCurrentStepActionId()
    {
        if (_currentStepIndex < 0 || _currentStepIndex >= TotalSteps)
            return string.Empty;

        return (steps[_currentStepIndex].actionId ?? string.Empty).Trim();
    }

    public event Action<int, int> OnProgressChanged;

    private int _currentStepIndex = 0;
    private bool _subscribed = false;
    private Coroutine _waitRoutine;
    private Coroutine _transitionRoutine;
    private Coroutine _stepEffectRoutine;
    private Coroutine _stepEffectSoundRoutine;
    private Coroutine _winFireworksRoutine;
    private Coroutine _winFireworksSoundRoutine;
    private Coroutine _winFireworksMoveRoutine;
    private float _lastActionTime;
    private float _lastMilestoneTime;
    private float _stepGuideTimerStartTime;
    private float _introMessageShownTime;
    private float _hintButtonRequestAnimationShownTime;
    private bool _idleHintShown;
    private bool _hintButtonRequestAnimationShown;
    private bool _hintButtonRequestAnimationTriggeredForCurrentMilestone;
    private bool _stepGuideVisible;
    private bool _introMessageUnlocked;
    private bool _hasWinFireworksBaseLocalPosition;
    private Vector3 _winFireworksBaseLocalPosition;

    private void Start()
    {
        if (statsManager == null) statsManager = FindFirstObjectByType<LevelStatsManager>();
    }

    private void OnEnable()
    {
        if (resetProgressOnEnable)
            _currentStepIndex = 0;

        _lastActionTime = Time.unscaledTime;
        _lastMilestoneTime = Time.unscaledTime;
        _stepGuideTimerStartTime = Time.unscaledTime;
        _introMessageShownTime = Time.unscaledTime;
        _hintButtonRequestAnimationShownTime = -1f;
        _idleHintShown = false;
        _hintButtonRequestAnimationShown = false;
        _hintButtonRequestAnimationTriggeredForCurrentMilestone = false;
        _stepGuideVisible = false;
        _introMessageUnlocked = false;

        if (hideStepEffectWhenIdle)
            SetStepEffectObjectVisible(false);

        if (hideWinFireworksWhenIdle)
            SetWinFireworksEffectVisible(false);

        SetHintButtonRequestAnimationVisible(false);
        HideAllStepGuideObjects();

        if (textManager != null)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            textManager.ShowRobotText($"Hi welcome user to {sceneName}");
        }

        if (_waitRoutine != null) StopCoroutine(_waitRoutine);
        _waitRoutine = StartCoroutine(WaitForGameManagerThenSubscribe());
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (_waitRoutine != null)
        {
            StopCoroutine(_waitRoutine);
            _waitRoutine = null;
        }
        if (_transitionRoutine != null)
        {
            StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
        }
        if (_stepEffectRoutine != null)
        {
            StopCoroutine(_stepEffectRoutine);
            _stepEffectRoutine = null;
        }
        if (_stepEffectSoundRoutine != null)
        {
            StopCoroutine(_stepEffectSoundRoutine);
            _stepEffectSoundRoutine = null;
        }
        if (_winFireworksRoutine != null)
        {
            StopCoroutine(_winFireworksRoutine);
            _winFireworksRoutine = null;
        }
        if (_winFireworksSoundRoutine != null)
        {
            StopCoroutine(_winFireworksSoundRoutine);
            _winFireworksSoundRoutine = null;
        }
        if (_winFireworksMoveRoutine != null)
        {
            StopCoroutine(_winFireworksMoveRoutine);
            _winFireworksMoveRoutine = null;
        }

        if (hideStepEffectWhenIdle)
            SetStepEffectObjectVisible(false);

        if (hideWinFireworksWhenIdle)
            SetWinFireworksEffectVisible(false);

        SetHintButtonRequestAnimationVisible(false);
        HideAllStepGuideObjects();
    }

    private void Update()
    {
        UpdateHintButtonRequestAnimation();
        UpdateStuckStepGuide();

        if (textManager == null || _currentStepIndex >= TotalSteps || _idleHintShown || idleHintDelaySeconds <= 0f)
            return;

        if (!CanReplaceIntroMessage(isPlayerAction: false))
            return;

        if (Time.unscaledTime - _lastActionTime < idleHintDelaySeconds)
            return;

        _idleHintShown = true;
        textManager.ShowRobotText(idleHintMessage);
        textManager.PlayJump();
    }

    private void UpdateStuckStepGuide()
    {
        if (_currentStepIndex < 0 || _currentStepIndex >= TotalSteps)
        {
            if (_stepGuideVisible)
                HideCurrentStepGuide();
            return;
        }

        if (stuckStepGuideDelaySeconds <= 0f || stuckStepGuideVisibleSeconds <= 0f)
        {
            if (_stepGuideVisible)
                HideCurrentStepGuide();
            return;
        }

        float elapsed = Time.unscaledTime - _stepGuideTimerStartTime;

        if (!_stepGuideVisible)
        {
            if (elapsed >= stuckStepGuideDelaySeconds)
                ShowCurrentStepGuide();

            return;
        }

        if (elapsed >= stuckStepGuideDelaySeconds + stuckStepGuideVisibleSeconds)
        {
            HideCurrentStepGuide();
            _stepGuideTimerStartTime = Time.unscaledTime;
        }
    }

    private void UpdateHintButtonRequestAnimation()
    {
        if (_currentStepIndex >= TotalSteps)
        {
            SetHintButtonRequestAnimationVisible(false);
            return;
        }

        if (hintButtonRequestAnimationObject == null
            || hintButtonRequestAnimationDelaySeconds <= 0f
            || hintButtonRequestAnimationVisibleSeconds <= 0f)
        {
            SetHintButtonRequestAnimationVisible(false);
            return;
        }

        if (_hintButtonRequestAnimationShown)
        {
            float visibleElapsed = Time.unscaledTime - _hintButtonRequestAnimationShownTime;
            if (visibleElapsed >= hintButtonRequestAnimationVisibleSeconds)
                DismissHintButtonRequestAnimation();

            return;
        }

        if (_hintButtonRequestAnimationTriggeredForCurrentMilestone)
            return;

        float elapsed = Time.unscaledTime - _lastMilestoneTime;
        if (elapsed < hintButtonRequestAnimationDelaySeconds)
            return;

        SetHintButtonRequestAnimationVisible(true);
        _hintButtonRequestAnimationTriggeredForCurrentMilestone = true;
        textManager?.PlayJump();
    }

    private IEnumerator WaitForGameManagerThenSubscribe()
    {
        while (GameManager.Instance == null)
        {
            yield return null;
        }

        Subscribe();

        yield return new WaitForEndOfFrame();
        NotifyProgress();
    }

    private void Subscribe()
    {
        if (_subscribed) return;

        GameManager.Instance.OnActionRecorded -= HandleAction;
        GameManager.Instance.OnActionRecorded += HandleAction;
        _subscribed = true;

        if (logProgress)
            Debug.Log("[LevelObjectiveTracker] Subscribed to GameManager actions.");
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;

        if (GameManager.Instance != null)
            GameManager.Instance.OnActionRecorded -= HandleAction;

        _subscribed = false;
    }

    private static bool IsTelemetryAction(string actionId)
    {
        return !string.IsNullOrEmpty(actionId)
            && actionId.StartsWith(TelemetryPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNonObjectiveAction(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId))
            return true;

        if (IsTelemetryAction(actionId))
            return true;

        return actionId.StartsWith("tab_", StringComparison.OrdinalIgnoreCase)
            || actionId.StartsWith("assistant_", StringComparison.OrdinalIgnoreCase)
            || actionId.StartsWith("user_id_", StringComparison.OrdinalIgnoreCase)
            || actionId.StartsWith("hint_requested", StringComparison.OrdinalIgnoreCase)
            || string.Equals(actionId, "ui_restart_pressed", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsConfiguredStepAction(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId) || steps == null)
            return false;

        string received = actionId.Trim();
        for (int i = 0; i < steps.Count; i++)
        {
            string expected = (steps[i]?.actionId ?? string.Empty).Trim();
            if (string.Equals(received, expected, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void HandleAction(string actionId)
    {
        if (_currentStepIndex >= TotalSteps) return;

        string received = (actionId ?? string.Empty).Trim();
        if (received.Length > 0)
        {
            _lastActionTime = Time.unscaledTime;
            _idleHintShown = false;

            if (!IsTelemetryAction(received))
                CanReplaceIntroMessage(isPlayerAction: true);

            if (string.Equals(received, "telemetry_progress_bar_visible:True", StringComparison.OrdinalIgnoreCase)
                || string.Equals(received, "telemetry_progress_bar_visible:False", StringComparison.OrdinalIgnoreCase))
            {
                DismissHintButtonRequestAnimation();
            }
        }

        if (IsNonObjectiveAction(received) && !IsConfiguredStepAction(received)) return;

        bool isInstantWin = false;
        if (!string.IsNullOrEmpty(winActionId) && string.Equals(received, winActionId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            isInstantWin = true;
        }
        else if (alternativeWinActions != null)
        {
            foreach (var alt in alternativeWinActions)
            {
                if (!string.IsNullOrEmpty(alt) && string.Equals(received, alt.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    isInstantWin = true;
                    break;
                }
            }
        }

        if (isInstantWin)
        {
            _mistakeStreak = 0;
            if (logProgress)
                Debug.Log($"[LevelObjectiveTracker] MASTER WIN TRIGGERED: '{received}'");

            GameManager.Instance?.RecordAction($"telemetry_level_instant_win:{received}");
            SetHintButtonRequestAnimationVisible(false);
            HideAllStepGuideObjects();
            _currentStepIndex = TotalSteps;
            PlayStepCompletedEffect();
            NotifyProgress();

            if (statsManager != null)
                statsManager.FinishLevel();
            else
                Debug.LogWarning("[LevelObjectiveTracker] Cannot log score: LevelStatsManager not found! Please attach it to your Manager object.");

            HandleLevelWin();
            return;
        }

        bool stepFound = false;
        int lastCompletedIndex = -1;

        for (int i = _currentStepIndex; i < TotalSteps; i++)
        {
            string expected = (steps[i].actionId ?? string.Empty).Trim();

            if (string.Equals(received, expected, StringComparison.OrdinalIgnoreCase))
            {
                if (steps[i].requiresPreviousStep && i != _currentStepIndex)
                    break;

                _mistakeStreak = 0;
                lastCompletedIndex = i;
                _currentStepIndex = i + 1;
                stepFound = true;
                break;
            }
        }

        if (stepFound)
        {
            string successMsg = string.Empty;
            if (lastCompletedIndex != -1)
            {
                successMsg = steps[lastCompletedIndex].successText;
                OnStepCompleted?.Invoke(successMsg);

                GameManager.Instance?.RecordAction($"telemetry_level_step_completed:{received}");

                var completedStep = steps[lastCompletedIndex];
                if (completedStep.changeStateOnComplete)
                {
                    if (completedStep.stateToLoad != null)
                    {
                        if (ScreenController.Instance != null)
                        {
                            ScreenController.Instance.LoadState(completedStep.stateToLoad, completedStep.pushLoadedStateToHistory);
                        }
                        else
                        {
                            Debug.LogWarning("[LevelObjectiveTracker] Cannot change state: ScreenController.Instance is null.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[LevelObjectiveTracker] Step '{completedStep.actionId}' has Change State On Complete enabled but no state assigned.");
                    }
                }
            }

            if (textManager != null && !string.IsNullOrEmpty(successMsg))
            {
                textManager.ShowSuccessText(successMsg);
            }

            if (logProgress)
                Debug.Log($"[LevelObjectiveTracker] Progress updated to step {_currentStepIndex}/{TotalSteps}");

            ResetHintButtonRequestAnimationTimer();
            ResetCurrentStepGuideTimer();
            PlayStepCompletedEffect();

            NotifyProgress();

            if (_currentStepIndex >= TotalSteps)
            {
                if (logProgress) Debug.Log("[LevelObjectiveTracker] LEVEL COMPLETE");

                GameManager.Instance?.RecordAction("telemetry_level_objectives_completed");

                if (statsManager != null)
                    statsManager.FinishLevel();
                else
                    Debug.LogWarning("[LevelObjectiveTracker] Cannot log score: LevelStatsManager not found! Please attach it to your Manager object.");

                HandleLevelWin();
            }
        }
        else
        {
            GameManager.Instance?.RecordAction($"telemetry_level_wrong_action:{received}");

            _mistakeStreak++;
            statsManager?.RegisterMistake();

            string warningMsg = "Hmm, that is not quite right...";
            if (_mistakeStreak >= 3)
            {
                warningMsg = idleHintMessage;
                _mistakeStreak = 0;
            }

            if (textManager != null)
                textManager.ShowRobotText(warningMsg);
        }
    }

    private void NotifyProgress()
    {
        OnProgressChanged?.Invoke(_currentStepIndex, TotalSteps);
        GameManager.Instance?.RecordAction($"telemetry_level_progress:{_currentStepIndex}/{TotalSteps}");
    }

    private bool CanReplaceIntroMessage(bool isPlayerAction)
    {
        if (_introMessageUnlocked)
            return true;

        if (isPlayerAction)
        {
            _introMessageUnlocked = true;
            return true;
        }

        float holdTime = Mathf.Max(0f, introMessageHoldSeconds);
        if (Time.unscaledTime - _introMessageShownTime >= holdTime)
        {
            _introMessageUnlocked = true;
            return true;
        }

        return false;
    }

    public string GetCurrentHint()
    {
        _hintCount++;
        if (_currentStepIndex >= TotalSteps) return "Done!";
        return steps[_currentStepIndex].hintText;
    }

    private void HandleLevelWin()
    {
        PlayWinFireworksEffect();
        onWin?.Invoke();

        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);

        _transitionRoutine = StartCoroutine(WaitThenTransition());
    }

    private IEnumerator WaitThenTransition()
    {
        yield return new WaitForSeconds(delayBeforeTransition);

        string sceneToLoad = ResolveNextScene();
        if (!string.IsNullOrWhiteSpace(sceneToLoad))
        {
            GameManager.Instance?.RecordAction($"telemetry_level_transition_scene:{sceneToLoad}");

            if (logProgress)
                Debug.Log($"[LevelObjectiveTracker] Transitioning to scene: {sceneToLoad}");
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            GameManager.Instance?.RecordAction("telemetry_level_transition_scene:none");

            if (logProgress)
                Debug.Log("[LevelObjectiveTracker] No next scene assigned. Staying in current scene.");
        }

        _transitionRoutine = null;
    }

    private void PlayStepCompletedEffect()
    {
        if (stepEffectAnimator == null)
        {
            if (logProgress)
                Debug.LogWarning("[LevelObjectiveTracker] Step effect skipped: Step Effect Animator is not assigned.");
        }

        if (_stepEffectRoutine != null)
            StopCoroutine(_stepEffectRoutine);

        if (_stepEffectSoundRoutine != null)
        {
            StopCoroutine(_stepEffectSoundRoutine);
            _stepEffectSoundRoutine = null;
        }

        _stepEffectRoutine = StartCoroutine(PlayStepCompletedEffectRoutine());
    }

    private void PlayWinFireworksEffect()
    {
        if (winFireworksAnimator == null)
        {
            if (logProgress)
                Debug.LogWarning("[LevelObjectiveTracker] Win fireworks effect skipped: Win Fireworks Animator is not assigned.");
        }

        if (_winFireworksRoutine != null)
            StopCoroutine(_winFireworksRoutine);

        if (_winFireworksSoundRoutine != null)
        {
            StopCoroutine(_winFireworksSoundRoutine);
            _winFireworksSoundRoutine = null;
        }

        if (_winFireworksMoveRoutine != null)
        {
            StopCoroutine(_winFireworksMoveRoutine);
            _winFireworksMoveRoutine = null;
        }

        _winFireworksRoutine = StartCoroutine(PlayWinFireworksEffectRoutine());
    }

    private IEnumerator PlayStepCompletedEffectRoutine()
    {
        if (hideStepEffectWhenIdle)
            SetStepEffectObjectVisible(true);

        if (stepEffectAudioSource != null && stepCompletedSound != null)
            _stepEffectSoundRoutine = StartCoroutine(PlayStepCompletedSoundRoutine());
        else if (logProgress)
            Debug.Log("[LevelObjectiveTracker] Step effect sound skipped: audio source or clip is missing.");

        if (stepEffectAnimator != null && !string.IsNullOrWhiteSpace(stepEffectParameterName))
        {
            if (stepEffectUsesBoolParameter)
            {
                stepEffectAnimator.SetBool(stepEffectParameterName, true);
                StartCoroutine(ResetStepEffectBoolRoutine());
            }
            else
            {
                stepEffectAnimator.SetTrigger(stepEffectParameterName);
            }
        }
        else if (logProgress)
        {
            Debug.LogWarning("[LevelObjectiveTracker] Step effect skipped: animator or parameter name is missing.");
        }

        if (hideStepEffectWhenIdle)
        {
            if (autoHideUsingAnimatorState
                && stepEffectAnimator != null
                && !string.IsNullOrWhiteSpace(stepEffectAnimationStateName))
            {
                yield return WaitForStepEffectAnimationToFinish();
            }
            else
            {
                float visibleDelay = Mathf.Max(0f, stepEffectVisibleDuration);
                if (visibleDelay > 0f)
                    yield return new WaitForSeconds(visibleDelay);
            }

            SetStepEffectObjectVisible(false);
        }

        _stepEffectRoutine = null;
    }

    private IEnumerator PlayWinFireworksEffectRoutine()
    {
        EnsureWinFireworksBasePosition();
        ResetWinFireworksPosition();

        if (hideWinFireworksWhenIdle)
            SetWinFireworksEffectVisible(true);

        if (winFireworksAudioSource != null && winFireworksSound != null)
            _winFireworksSoundRoutine = StartCoroutine(PlayWinFireworksSoundRoutine());
        else if (logProgress)
            Debug.Log("[LevelObjectiveTracker] Win fireworks sound skipped: audio source or clip is missing.");

        if (winFireworksEffectObject != null && winFireworksRiseHeight > 0f && winFireworksRiseSpeed > 0f)
            _winFireworksMoveRoutine = StartCoroutine(MoveWinFireworksUpRoutine());

        if (winFireworksAnimator != null && !string.IsNullOrWhiteSpace(winFireworksParameterName))
        {
            if (winFireworksUsesBoolParameter)
            {
                winFireworksAnimator.SetBool(winFireworksParameterName, true);
                StartCoroutine(ResetWinFireworksBoolRoutine());
            }
            else
            {
                winFireworksAnimator.SetTrigger(winFireworksParameterName);
            }
        }
        else if (logProgress)
        {
            Debug.LogWarning("[LevelObjectiveTracker] Win fireworks effect skipped: animator or parameter name is missing.");
        }

        if (hideWinFireworksWhenIdle)
        {
            if (autoHideWinFireworksUsingAnimatorState
                && winFireworksAnimator != null
                && !string.IsNullOrWhiteSpace(winFireworksAnimationStateName))
            {
                yield return WaitForWinFireworksAnimationToFinish();
            }
            else
            {
                float visibleDelay = Mathf.Max(0f, winFireworksVisibleDuration);
                if (visibleDelay > 0f)
                    yield return new WaitForSeconds(visibleDelay);
            }

            SetWinFireworksEffectVisible(false);
            ResetWinFireworksPosition();
        }

        _winFireworksRoutine = null;
    }

    private IEnumerator MoveWinFireworksUpRoutine()
    {
        if (winFireworksEffectObject == null)
        {
            _winFireworksMoveRoutine = null;
            yield break;
        }

        Vector3 targetLocalPosition = _winFireworksBaseLocalPosition + (Vector3.up * Mathf.Max(0f, winFireworksRiseHeight));

        while (winFireworksEffectObject != null)
        {
            var currentPosition = winFireworksEffectObject.transform.localPosition;
            var nextPosition = Vector3.MoveTowards(
                currentPosition,
                targetLocalPosition,
                Mathf.Max(0f, winFireworksRiseSpeed) * Time.unscaledDeltaTime);

            winFireworksEffectObject.transform.localPosition = nextPosition;

            if (nextPosition == targetLocalPosition)
                break;

            yield return null;
        }

        _winFireworksMoveRoutine = null;
    }

    private IEnumerator ResetStepEffectBoolRoutine()
    {
        if (stepEffectAnimator != null)
        {
            float waitTimeout = Mathf.Max(0.05f, stepEffectWaitForEnterTimeout);
            float elapsed = 0f;
            bool enteredEffectState = false;

            while (elapsed < waitTimeout)
            {
                AnimatorStateInfo stateInfo = stepEffectAnimator.GetCurrentAnimatorStateInfo(0);
                if (IsAnimatorInEffectState(stateInfo))
                {
                    enteredEffectState = true;
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (enteredEffectState)
            {
                float completionTimeout = Mathf.Max(0.2f, stepEffectWaitForCompletionTimeout);
                float completionElapsed = 0f;

                while (completionElapsed < completionTimeout)
                {
                    AnimatorStateInfo stateInfo = stepEffectAnimator.GetCurrentAnimatorStateInfo(0);
                    if (IsAnimatorInEffectState(stateInfo) && !stateInfo.loop && stateInfo.normalizedTime >= 1f)
                        break;

                    completionElapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }

        float resetDelay = Mathf.Max(stepEffectMinResetDelayAfterEnter, stepEffectBoolResetDelay);
        if (resetDelay > 0f)
            yield return new WaitForSeconds(resetDelay);

        if (stepEffectAnimator != null && !string.IsNullOrWhiteSpace(stepEffectParameterName))
            stepEffectAnimator.SetBool(stepEffectParameterName, false);
    }

    private IEnumerator ResetWinFireworksBoolRoutine()
    {
        if (winFireworksAnimator != null)
        {
            float waitTimeout = Mathf.Max(0.05f, winFireworksWaitForEnterTimeout);
            float elapsed = 0f;
            bool enteredEffectState = false;

            while (elapsed < waitTimeout)
            {
                AnimatorStateInfo stateInfo = winFireworksAnimator.GetCurrentAnimatorStateInfo(0);
                if (IsWinFireworksAnimatorInEffectState(stateInfo))
                {
                    enteredEffectState = true;
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (enteredEffectState)
            {
                float completionTimeout = Mathf.Max(0.2f, winFireworksWaitForCompletionTimeout);
                float completionElapsed = 0f;

                while (completionElapsed < completionTimeout)
                {
                    AnimatorStateInfo stateInfo = winFireworksAnimator.GetCurrentAnimatorStateInfo(0);
                    if (IsWinFireworksAnimatorInEffectState(stateInfo) && !stateInfo.loop && stateInfo.normalizedTime >= 1f)
                        break;

                    completionElapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }

        float resetDelay = Mathf.Max(winFireworksMinResetDelayAfterEnter, winFireworksBoolResetDelay);
        if (resetDelay > 0f)
            yield return new WaitForSeconds(resetDelay);

        if (winFireworksAnimator != null && !string.IsNullOrWhiteSpace(winFireworksParameterName))
            winFireworksAnimator.SetBool(winFireworksParameterName, false);
    }

    private IEnumerator PlayStepCompletedSoundRoutine()
    {
        if (stepCompletedSoundDelay > 0f)
            yield return new WaitForSeconds(stepCompletedSoundDelay);

        if (stepEffectAudioSource != null && stepCompletedSound != null)
            stepEffectAudioSource.PlayOneShot(stepCompletedSound);

        _stepEffectSoundRoutine = null;
    }

    private IEnumerator PlayWinFireworksSoundRoutine()
    {
        if (winFireworksSoundDelay > 0f)
            yield return new WaitForSeconds(winFireworksSoundDelay);

        if (winFireworksAudioSource != null && winFireworksSound != null)
            winFireworksAudioSource.PlayOneShot(winFireworksSound);

        _winFireworksSoundRoutine = null;
    }

    private IEnumerator WaitForStepEffectAnimationToFinish()
    {
        float timeout = Mathf.Max(0.2f, stepEffectAutoHideTimeout);
        float elapsed = 0f;
        bool enteredEffectState = false;
        bool animationCompleted = false;

        while (elapsed < timeout)
        {
            if (stepEffectAnimator == null || !stepEffectAnimator.isActiveAndEnabled)
                yield break;

            AnimatorStateInfo stateInfo = stepEffectAnimator.GetCurrentAnimatorStateInfo(0);
            bool inEffectState = IsAnimatorInEffectState(stateInfo);

            if (!enteredEffectState)
            {
                if (inEffectState)
                    enteredEffectState = true;
            }
            else
            {
                if (inEffectState)
                {
                    if (!stateInfo.loop && stateInfo.normalizedTime >= 1f)
                        animationCompleted = true;
                }
                else if (animationCompleted)
                {
                    yield break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitForWinFireworksAnimationToFinish()
    {
        float timeout = Mathf.Max(0.2f, winFireworksAutoHideTimeout);
        float elapsed = 0f;
        bool enteredEffectState = false;
        bool animationCompleted = false;

        while (elapsed < timeout)
        {
            if (winFireworksAnimator == null || !winFireworksAnimator.isActiveAndEnabled)
                yield break;

            AnimatorStateInfo stateInfo = winFireworksAnimator.GetCurrentAnimatorStateInfo(0);
            bool inEffectState = IsWinFireworksAnimatorInEffectState(stateInfo);

            if (!enteredEffectState)
            {
                if (inEffectState)
                    enteredEffectState = true;
            }
            else
            {
                if (inEffectState)
                {
                    if (!stateInfo.loop && stateInfo.normalizedTime >= 1f)
                        animationCompleted = true;
                }
                else if (animationCompleted)
                {
                    yield break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private bool IsAnimatorInEffectState(AnimatorStateInfo stateInfo)
    {
        if (string.IsNullOrWhiteSpace(stepEffectAnimationStateName))
            return false;

        int shortNameHash = Animator.StringToHash(stepEffectAnimationStateName);
        if (stateInfo.shortNameHash == shortNameHash)
            return true;

        return stateInfo.IsName(stepEffectAnimationStateName)
            || stateInfo.IsName($"Base Layer.{stepEffectAnimationStateName}");
    }

    private bool IsWinFireworksAnimatorInEffectState(AnimatorStateInfo stateInfo)
    {
        if (string.IsNullOrWhiteSpace(winFireworksAnimationStateName))
            return false;

        int shortNameHash = Animator.StringToHash(winFireworksAnimationStateName);
        if (stateInfo.shortNameHash == shortNameHash)
            return true;

        return stateInfo.IsName(winFireworksAnimationStateName)
            || stateInfo.IsName($"Base Layer.{winFireworksAnimationStateName}");
    }

    private void SetStepEffectObjectVisible(bool isVisible)
    {
        if (stepEffectObject == null)
            return;

        if (stepEffectObject.activeSelf != isVisible)
            stepEffectObject.SetActive(isVisible);
    }

    private void SetWinFireworksEffectVisible(bool isVisible)
    {
        if (winFireworksEffectObject == null)
            return;

        if (winFireworksEffectObject.activeSelf != isVisible)
            winFireworksEffectObject.SetActive(isVisible);

        if (!isVisible)
            ResetWinFireworksPosition();
    }

    private void EnsureWinFireworksBasePosition()
    {
        if (winFireworksEffectObject == null || _hasWinFireworksBaseLocalPosition)
            return;

        _winFireworksBaseLocalPosition = winFireworksEffectObject.transform.localPosition;
        _hasWinFireworksBaseLocalPosition = true;
    }

    private void ResetWinFireworksPosition()
    {
        if (winFireworksEffectObject == null)
            return;

        EnsureWinFireworksBasePosition();
        winFireworksEffectObject.transform.localPosition = _winFireworksBaseLocalPosition;
    }

    private void ResetHintButtonRequestAnimationTimer()
    {
        _lastMilestoneTime = Time.unscaledTime;
        _hintButtonRequestAnimationShownTime = -1f;
        _hintButtonRequestAnimationTriggeredForCurrentMilestone = false;
        SetHintButtonRequestAnimationVisible(false);
    }

    private void SetHintButtonRequestAnimationVisible(bool isVisible)
    {
        if (hintButtonRequestAnimationObject != null && hintButtonRequestAnimationObject.activeSelf != isVisible)
            hintButtonRequestAnimationObject.SetActive(isVisible);

        _hintButtonRequestAnimationShown = isVisible;

        if (isVisible)
            _hintButtonRequestAnimationShownTime = Time.unscaledTime;
    }

    private void DismissHintButtonRequestAnimation()
    {
        _hintButtonRequestAnimationShownTime = -1f;
        SetHintButtonRequestAnimationVisible(false);
    }

    private void ResetCurrentStepGuideTimer()
    {
        HideAllStepGuideObjects();
        _stepGuideVisible = false;
        _stepGuideTimerStartTime = Time.unscaledTime;
    }

    private void ShowCurrentStepGuide()
    {
        var guideObject = GetCurrentStepGuideObject();
        if (guideObject == null)
        {
            _stepGuideTimerStartTime = Time.unscaledTime;
            return;
        }

        HideAllStepGuideObjects();
        guideObject.SetActive(true);
        _stepGuideVisible = true;
    }

    private void HideCurrentStepGuide()
    {
        var guideObject = GetCurrentStepGuideObject();
        if (guideObject != null && guideObject.activeSelf)
            guideObject.SetActive(false);

        _stepGuideVisible = false;
    }

    private void HideAllStepGuideObjects()
    {
        if (steps == null)
            return;

        for (int i = 0; i < steps.Count; i++)
        {
            var guideObject = steps[i]?.stuckStepGuideObject;
            if (guideObject != null && guideObject.activeSelf)
                guideObject.SetActive(false);
        }
    }

    private GameObject GetCurrentStepGuideObject()
    {
        if (_currentStepIndex < 0 || _currentStepIndex >= TotalSteps)
            return null;

        return steps[_currentStepIndex]?.stuckStepGuideObject;
    }

    private string ResolveNextScene()
    {
#if UNITY_EDITOR
        if (nextSceneAsset != null)
        {
            string scenePath = AssetDatabase.GetAssetPath(nextSceneAsset);
            return System.IO.Path.GetFileNameWithoutExtension(scenePath);
        }
#endif
        return nextSceneName;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (nextSceneAsset == null)
        {
            nextSceneName = string.Empty;
            return;
        }

        string scenePath = AssetDatabase.GetAssetPath(nextSceneAsset);
        nextSceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
    }
#endif
}
