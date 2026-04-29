using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelUnlock : MonoBehaviour
{
    private enum UnlockCompletionMode
    {
        DelayOnly,
        AnimatorState,
        AnimationEvent,
        LegacyAnimation
    }

    [Header("Unlock Visual")]
    [SerializeField] private GameObject unlockParent;
    [SerializeField] private GameObject unlockAnimationChild;
    [SerializeField] private Image unlockImage;
    [SerializeField] private Sprite startingSprite;
    [SerializeField] private Sprite completedSprite;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private float delayAfterCompletedSprite = 0.75f;

    [Header("Animation")]
    [SerializeField] private Animator unlockAnimator;
    [SerializeField] private Animation legacyUnlockAnimation;
    [SerializeField] private UnlockCompletionMode completionMode = UnlockCompletionMode.DelayOnly;
    [SerializeField] private bool triggerAnimatorOnStart = false;
    [SerializeField] private string animatorTriggerName = "Play";
    [SerializeField] private bool setAnimatorBoolOnStart = true;
    [SerializeField] private string animatorBoolName = "unlocked";
    [SerializeField] private bool animatorBoolValueOnStart = true;
    [SerializeField] private bool playAnimatorStateDirectlyOnStart = true;
    [SerializeField] private bool disableAnimatorBeforeCompletedSpriteSwap = true;
    [SerializeField] private string unlockAnimationStateName = "level 2 unlock";
    [SerializeField] private float delayBeforeHide = 1.1f;
    [SerializeField] private float animationTimeout = 5f;

    [Header("Audio")]
    [SerializeField] private AudioSource unlockAudioSource;
    [SerializeField] private AudioClip unlockMusic;
    [SerializeField] private bool loopUnlockMusic = true;

    [Header("Between Levels")]
    [SerializeField] private GameObject betweenLevelsRoot;
    [SerializeField] private BetweenLevels betweenLevels;
    [SerializeField] private bool hideBetweenLevelsRootUntilUnlock = true;
    [SerializeField] private bool disableBetweenLevelsUntilUnlock = true;
    [SerializeField] private bool showBetweenLevelsRootAfterUnlock = true;
    [SerializeField] private bool playBetweenLevelsTextAfterUnlock = true;

    private Coroutine _sequenceRoutine;
    private bool _unlockAnimationFinished;
    private bool _startedAnimatorDirectly;
    private string _resolvedUnlockStateName;

    private void Reset()
    {
        if (unlockParent == null)
            unlockParent = gameObject;

        if (unlockAnimationChild == null && transform.childCount > 0)
            unlockAnimationChild = transform.GetChild(0).gameObject;

        ResolveAnimationReferences();

        if (betweenLevels == null)
            betweenLevels = FindFirstObjectByType<BetweenLevels>();
    }

    private void Awake()
    {
        if (unlockParent == null)
            unlockParent = gameObject;

        ResolveAnimationReferences();
        PrepareBetweenLevelsForUnlock();
    }

    private void OnEnable()
    {
        if (playOnEnable)
            PlayUnlockSequence();
    }

    private void OnDisable()
    {
        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }

        StopUnlockMusic();
    }

    public void PlayUnlockSequence()
    {
        if (_sequenceRoutine != null)
            StopCoroutine(_sequenceRoutine);

        ResolveAnimationReferences();
        PrepareBetweenLevelsForUnlock();
        _sequenceRoutine = StartCoroutine(PlayUnlockSequenceRoutine());
    }

    public void NotifyUnlockAnimationFinished()
    {
        _unlockAnimationFinished = true;
    }

    public void FinishUnlockImmediately()
    {
        _unlockAnimationFinished = true;
    }

    private IEnumerator PlayUnlockSequenceRoutine()
    {
        _unlockAnimationFinished = false;
        _startedAnimatorDirectly = false;

        ApplyStartingSprite();
        SetUnlockVisualVisible(true);
        yield return WaitForStableAnimationStart();
        TriggerUnlockAnimation();
        StartUnlockMusic();
        yield return HoldFirstAnimatorFrameIfNeeded();

        yield return WaitForUnlockToFinish();
        StopUnlockMusic();
        ApplyCompletedSprite();
        yield return WaitForDuration(delayAfterCompletedSprite);

        StartBetweenLevelsSequence();
        _sequenceRoutine = null;
        SetUnlockVisualVisible(false);
    }

    private void PrepareBetweenLevelsForUnlock()
    {
        if (betweenLevels != null)
            betweenLevels.ClearText();

        if (hideBetweenLevelsRootUntilUnlock
            && betweenLevelsRoot != null
            && betweenLevelsRoot != gameObject
            && betweenLevelsRoot.activeSelf)
        {
            betweenLevelsRoot.SetActive(false);
        }

        if (disableBetweenLevelsUntilUnlock
            && betweenLevels != null
            && betweenLevels.enabled)
        {
            betweenLevels.enabled = false;
        }
    }

    private void TriggerUnlockAnimation()
    {
        ResolveAnimationReferences();
        _startedAnimatorDirectly = false;

        if (unlockAnimator != null && !unlockAnimator.enabled)
            unlockAnimator.enabled = true;

        if (completionMode == UnlockCompletionMode.AnimatorState
            && playAnimatorStateDirectlyOnStart
            && unlockAnimator != null
            && TryResolveUnlockStateName(out _resolvedUnlockStateName))
        {
            unlockAnimator.Rebind();
            unlockAnimator.Play(_resolvedUnlockStateName, 0, 0f);
            unlockAnimator.Update(0f);
            _startedAnimatorDirectly = true;
            return;
        }

        if (setAnimatorBoolOnStart
            && unlockAnimator != null
            && HasAnimatorParameter(animatorBoolName, AnimatorControllerParameterType.Bool))
        {
            unlockAnimator.SetBool(animatorBoolName, animatorBoolValueOnStart);
        }

        if (triggerAnimatorOnStart
            && unlockAnimator != null
            && HasAnimatorParameter(animatorTriggerName, AnimatorControllerParameterType.Trigger))
        {
            unlockAnimator.ResetTrigger(animatorTriggerName);
            unlockAnimator.SetTrigger(animatorTriggerName);
        }

        if (completionMode == UnlockCompletionMode.LegacyAnimation
            && legacyUnlockAnimation != null
            && !legacyUnlockAnimation.isPlaying)
        {
            legacyUnlockAnimation.Play();
        }
    }

    private IEnumerator WaitForUnlockToFinish()
    {
        switch (completionMode)
        {
            case UnlockCompletionMode.AnimatorState:
                yield return WaitForAnimatorStateOrTimeout();
                yield break;

            case UnlockCompletionMode.AnimationEvent:
                yield return WaitForAnimationEventOrTimeout();
                yield break;

            case UnlockCompletionMode.LegacyAnimation:
                yield return WaitForLegacyAnimationOrTimeout();
                yield break;

            default:
                yield return WaitForDelayOrSignal(delayBeforeHide);
                yield break;
        }
    }

    private IEnumerator WaitForAnimatorStateOrTimeout()
    {
        if (unlockAnimator == null)
        {
            yield return WaitForDelayOrSignal(delayBeforeHide);
            yield break;
        }

        if (TryGetUnlockAnimationDuration(out float animationDuration))
        {
            float waitDuration = Mathf.Min(Mathf.Max(0.01f, animationDuration), Mathf.Max(0.05f, animationTimeout));
            yield return WaitForDuration(waitDuration);
            yield break;
        }

        if (!TryResolveUnlockStateName(out _resolvedUnlockStateName))
        {
            yield return WaitForDelayOrSignal(delayBeforeHide);
            yield break;
        }

        float timeout = Mathf.Max(0.05f, animationTimeout);
        float elapsed = 0f;
        bool enteredState = false;
        int resolvedStateHash = Animator.StringToHash(_resolvedUnlockStateName);

        while (elapsed < timeout)
        {
            if (_unlockAnimationFinished)
                yield break;

            AnimatorStateInfo stateInfo = unlockAnimator.GetCurrentAnimatorStateInfo(0);
            bool inUnlockState = IsResolvedUnlockState(stateInfo, resolvedStateHash);

            if (!enteredState)
            {
                if (inUnlockState)
                    enteredState = true;
            }
            else if (!inUnlockState)
            {
                yield break;
            }
            else if (inUnlockState && !stateInfo.loop && stateInfo.normalizedTime >= 1f)
            {
                yield break;
            }

            elapsed += GetDeltaTime();
            yield return null;
        }
    }

    private IEnumerator WaitForAnimationEventOrTimeout()
    {
        float timeout = Mathf.Max(0.05f, animationTimeout);
        float elapsed = 0f;

        while (!_unlockAnimationFinished && elapsed < timeout)
        {
            elapsed += GetDeltaTime();
            yield return null;
        }
    }

    private IEnumerator WaitForLegacyAnimationOrTimeout()
    {
        if (legacyUnlockAnimation == null)
        {
            yield return WaitForDelayOrSignal(delayBeforeHide);
            yield break;
        }

        float timeout = Mathf.Max(0.05f, animationTimeout);
        float elapsed = 0f;
        bool animationStarted = false;

        while (elapsed < timeout)
        {
            if (_unlockAnimationFinished)
                yield break;

            if (legacyUnlockAnimation.isPlaying)
            {
                animationStarted = true;
            }
            else if (animationStarted)
            {
                yield break;
            }

            elapsed += GetDeltaTime();
            yield return null;
        }
    }

    private IEnumerator WaitForDelayOrSignal(float delay)
    {
        float elapsed = 0f;
        float targetDelay = Mathf.Max(0f, delay);

        while (!_unlockAnimationFinished && elapsed < targetDelay)
        {
            elapsed += GetDeltaTime();
            yield return null;
        }
    }

    private IEnumerator WaitForDuration(float duration)
    {
        float elapsed = 0f;
        float targetDuration = Mathf.Max(0f, duration);

        while (elapsed < targetDuration)
        {
            elapsed += GetDeltaTime();
            yield return null;
        }
    }

    private IEnumerator WaitForStableAnimationStart()
    {
        yield return null;

        const float maxStartDelta = 0.1f;
        const int maxExtraFrames = 4;
        int extraFrames = 0;

        while (GetDeltaTime() > maxStartDelta && extraFrames < maxExtraFrames)
        {
            extraFrames++;
            yield return null;
        }
    }

    private IEnumerator HoldFirstAnimatorFrameIfNeeded()
    {
        if (!_startedAnimatorDirectly || unlockAnimator == null)
            yield break;

        float originalSpeed = unlockAnimator.speed;
        unlockAnimator.speed = 0f;
        yield return null;

        if (unlockAnimator != null)
            unlockAnimator.speed = originalSpeed > 0f ? originalSpeed : 1f;
    }

    private void StartBetweenLevelsSequence()
    {
        bool autoPlaybackWillRun = false;

        if (showBetweenLevelsRootAfterUnlock
            && betweenLevelsRoot != null
            && betweenLevelsRoot != gameObject
            && !betweenLevelsRoot.activeSelf)
        {
            betweenLevelsRoot.SetActive(true);

            if (betweenLevels != null && betweenLevels.PlayOnEnable)
                autoPlaybackWillRun = true;
        }

        if (disableBetweenLevelsUntilUnlock
            && betweenLevels != null
            && !betweenLevels.enabled)
        {
            betweenLevels.enabled = true;

            if (betweenLevels.PlayOnEnable)
                autoPlaybackWillRun = true;
        }

        if (playBetweenLevelsTextAfterUnlock
            && betweenLevels != null
            && !autoPlaybackWillRun)
        {
            betweenLevels.PlayConfiguredText();
        }
    }

    private bool IsUnlockState(AnimatorStateInfo stateInfo)
    {
        if (string.IsNullOrWhiteSpace(unlockAnimationStateName))
            return false;

        int shortNameHash = Animator.StringToHash(unlockAnimationStateName);
        if (stateInfo.shortNameHash == shortNameHash)
            return true;

        return stateInfo.IsName(unlockAnimationStateName)
            || stateInfo.IsName($"Base Layer.{unlockAnimationStateName}");
    }

    private bool IsResolvedUnlockState(AnimatorStateInfo stateInfo, int resolvedStateHash)
    {
        if (stateInfo.shortNameHash == resolvedStateHash || stateInfo.fullPathHash == resolvedStateHash)
            return true;

        if (string.IsNullOrWhiteSpace(_resolvedUnlockStateName))
            return IsUnlockState(stateInfo);

        return stateInfo.IsName(_resolvedUnlockStateName);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (unlockAnimator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        AnimatorControllerParameter[] parameters = unlockAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.type == parameterType && parameter.name == parameterName)
                return true;
        }

        return false;
    }

    private bool TryResolveUnlockStateName(out string resolvedStateName)
    {
        resolvedStateName = string.Empty;
        if (unlockAnimator == null || string.IsNullOrWhiteSpace(unlockAnimationStateName))
            return false;

        string raw = unlockAnimationStateName;
        string trimmed = raw.Trim();
        string[] candidates = new[]
        {
            raw,
            trimmed,
            raw + " ",
            trimmed + " ",
            "Base Layer." + raw,
            "Base Layer." + trimmed,
            "Base Layer." + raw + " ",
            "Base Layer." + trimmed + " "
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string candidate = candidates[i];
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            int stateHash = Animator.StringToHash(candidate);
            if (unlockAnimator.HasState(0, stateHash))
            {
                resolvedStateName = candidate;
                return true;
            }
        }

        return false;
    }

    private bool TryGetUnlockAnimationDuration(out float duration)
    {
        duration = 0f;
        if (unlockAnimator == null || unlockAnimator.runtimeAnimatorController == null)
            return false;

        string raw = unlockAnimationStateName ?? string.Empty;
        string trimmed = raw.Trim();
        AnimationClip[] clips = unlockAnimator.runtimeAnimatorController.animationClips;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
                continue;

            string clipName = clip.name ?? string.Empty;
            if (clipName == raw || clipName == trimmed || clipName.Trim() == trimmed)
            {
                duration = clip.length;
                return duration > 0f;
            }
        }

        return false;
    }

    private void SetUnlockVisualVisible(bool isVisible)
    {
        if (unlockParent != null)
        {
            if (unlockParent.activeSelf != isVisible)
                unlockParent.SetActive(isVisible);

            return;
        }

        if (unlockAnimationChild != null && unlockAnimationChild.activeSelf != isVisible)
            unlockAnimationChild.SetActive(isVisible);
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private void ApplyStartingSprite()
    {
        if (unlockImage != null && startingSprite != null)
            unlockImage.sprite = startingSprite;
    }

    private void ApplyCompletedSprite()
    {
        if (unlockImage == null || completedSprite == null)
            return;

        if (unlockAnimator != null)
            unlockAnimator.speed = 1f;

        if (unlockAnimator != null && disableAnimatorBeforeCompletedSpriteSwap)
            unlockAnimator.enabled = false;

        unlockImage.sprite = completedSprite;
    }

    private void StartUnlockMusic()
    {
        if (unlockAudioSource == null || unlockMusic == null)
            return;

        unlockAudioSource.clip = unlockMusic;
        unlockAudioSource.loop = loopUnlockMusic;
        unlockAudioSource.Play();
    }

    private void StopUnlockMusic()
    {
        if (unlockAudioSource == null)
            return;

        if (unlockAudioSource.isPlaying)
            unlockAudioSource.Stop();

        unlockAudioSource.loop = false;
    }

    private void ResolveAnimationReferences()
    {
        if (unlockAnimationChild != null)
        {
            Animator childAnimator = unlockAnimationChild.GetComponent<Animator>();
            if (childAnimator != null)
                unlockAnimator = childAnimator;

            Image childImage = unlockAnimationChild.GetComponent<Image>();
            if (childImage != null)
                unlockImage = childImage;

            if (legacyUnlockAnimation == null)
                legacyUnlockAnimation = unlockAnimationChild.GetComponent<Animation>();
        }

        if (unlockImage == null)
            unlockImage = GetComponentInChildren<Image>(true);

        if (unlockAnimator != null)
            unlockAnimator.updateMode = useUnscaledTime ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
    }
}
