using UnityEngine;
using TMPro;

public class RobotTextManager : MonoBehaviour
{
    public static RobotTextManager Instance { get; private set; }

    [Header("Text Boxes")]
    [SerializeField] private TextMeshProUGUI hintDisplay;
    [SerializeField] private TextMeshProUGUI robotDisplay;

    [Header("Typewriter Settings")]
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("Hint Box Audio")]
    [SerializeField] private AudioSource hintAudioSource;
    [SerializeField] private AudioClip hintBoxGoodSound;
    [SerializeField] private AudioClip hintBoxHintSound;

    [Header("Robot Box Audio")]
    [SerializeField] private AudioSource robotAudioSource;
    [SerializeField] private AudioClip robotGoodSound;
    [SerializeField] private AudioClip robotBadSound;
    [SerializeField] private AudioClip robotHintPromptSound;

    [Header("Robot Animation")]
    [SerializeField] private Animator robotAnimator;
    [SerializeField] private string jumpBoolParameter = "IsJumping";
    [SerializeField] private float jumpDurationSeconds = 1f;
    [SerializeField] private Transform robotJumpTarget;
    [SerializeField] private float jumpHeightUnits = 25f;

    private Coroutine _hintTypingCoroutine;
    private string _hintCurrentTarget = "";

    private Coroutine _robotTypingCoroutine;
    private string _robotCurrentTarget = "";
    private Coroutine _jumpRoutine;
    private Transform _activeJumpTarget;
    private Vector3 _activeJumpBaseLocalPosition;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        ResetJumpVisualState();

        if (Instance == this)
            Instance = null;
    }

    public void ShowHintText(string message)
    {
        RunTypewriter(ref _hintTypingCoroutine, ref _hintCurrentTarget,
            hintDisplay, hintAudioSource, message, isRobotBox: false);
    }

    public void ShowSuccessText(string message)
    {
        RunTypewriter(ref _hintTypingCoroutine, ref _hintCurrentTarget,
            hintDisplay, hintAudioSource, message, isRobotBox: false);
        RunTypewriter(ref _robotTypingCoroutine, ref _robotCurrentTarget,
            robotDisplay, robotAudioSource, message, isRobotBox: true);
    }

    public void ShowRobotText(string message)
    {
        RunTypewriter(ref _robotTypingCoroutine, ref _robotCurrentTarget,
            robotDisplay, robotAudioSource, message, isRobotBox: true);
    }

    public void PlayJump()
    {
        if (robotAnimator == null)
        {
            Debug.LogWarning("[RobotTextManager] No robot Animator assigned for jump animation.");
            return;
        }

        if (string.IsNullOrWhiteSpace(jumpBoolParameter))
        {
            Debug.LogWarning("[RobotTextManager] Jump bool parameter name is empty.");
            return;
        }

        var jumpTarget = GetJumpTarget();
        if (jumpTarget == null)
        {
            Debug.LogWarning("[RobotTextManager] No jump target assigned for vertical jump motion.");
            return;
        }

        if (_jumpRoutine != null)
        {
            StopCoroutine(_jumpRoutine);
            ResetJumpVisualState();
        }

        _jumpRoutine = StartCoroutine(PlayJumpRoutine(jumpTarget));
    }

    private void RunTypewriter(ref Coroutine slot, ref string currentTarget,
        TextMeshProUGUI display, AudioSource audioSource, string message, bool isRobotBox)
    {
        if (display == null) return;
        if (message == currentTarget && slot != null) return;

        if (slot != null) StopCoroutine(slot);
        currentTarget = message;
        slot = StartCoroutine(TypeTextOnDisplay(display, audioSource, message, isRobotBox));
    }

    private System.Collections.IEnumerator ShakeDisplay(TextMeshProUGUI display)
    {
        Vector3 originalPos = display.transform.localPosition;
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            float x = Random.Range(-5f, 5f);
            display.transform.localPosition =
                new Vector3(originalPos.x + x, originalPos.y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        display.transform.localPosition = originalPos;
    }

    private System.Collections.IEnumerator TypeTextOnDisplay(
        TextMeshProUGUI display, AudioSource audioSource, string fullText, bool isRobotBox)
    {
        display.text = "";

        string lower = fullText.ToLower();
        AudioClip clipToPlay = null;

        if (isRobotBox)
        {
            if (lower.Contains("not right") || lower.Contains("not quite right"))
            {
                clipToPlay = robotBadSound;
                StartCoroutine(ShakeDisplay(display));
            }
            else if (lower.Contains("stuck") || lower.Contains("careful"))
            {
                clipToPlay = robotHintPromptSound;
                StartCoroutine(ShakeDisplay(display));
            }
            else
            {
                clipToPlay = robotGoodSound;
            }
        }
        else
        {
            clipToPlay = lower.Contains("hint") || lower.Contains("try")
                ? hintBoxHintSound
                : hintBoxGoodSound;
        }

        try
        {
            if (audioSource != null && clipToPlay != null)
            {
                audioSource.clip = clipToPlay;
                audioSource.Play();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[RobotTextManager] Audio failed to play: " + e.Message);
        }

        foreach (char letter in fullText.ToCharArray())
        {
            display.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        try
        {
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[RobotTextManager] Audio failed to stop: " + e.Message);
        }
    }

    private System.Collections.IEnumerator PlayJumpRoutine(Transform jumpTarget)
    {
        _activeJumpTarget = jumpTarget;
        _activeJumpBaseLocalPosition = jumpTarget.localPosition;

        robotAnimator.SetBool(jumpBoolParameter, true);

        float duration = Mathf.Max(0.01f, jumpDurationSeconds);
        float height = Mathf.Max(0f, jumpHeightUnits);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float yOffset = height * (4f * t * (1f - t));

            if (_activeJumpTarget != null)
                _activeJumpTarget.localPosition = _activeJumpBaseLocalPosition + (Vector3.up * yOffset);

            yield return null;
        }

        ResetJumpVisualState();

        _jumpRoutine = null;
    }

    private Transform GetJumpTarget()
    {
        if (robotJumpTarget != null)
            return robotJumpTarget;

        if (robotAnimator != null)
            return robotAnimator.transform;

        return null;
    }

    private void ResetJumpVisualState()
    {
        if (robotAnimator != null && !string.IsNullOrWhiteSpace(jumpBoolParameter))
            robotAnimator.SetBool(jumpBoolParameter, false);

        if (_activeJumpTarget != null)
            _activeJumpTarget.localPosition = _activeJumpBaseLocalPosition;

        _activeJumpTarget = null;
    }
}
