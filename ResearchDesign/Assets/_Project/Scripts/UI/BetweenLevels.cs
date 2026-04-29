using System.Collections;
using TMPro;
using UnityEngine;

public class BetweenLevels : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private AudioClip typingSound;

    [Header("Content")]
    [TextArea(2, 8)]
    [SerializeField] private string message;

    [Header("Typewriter")]
    [SerializeField] private float typingSpeed = 0.04f;
    [SerializeField] private bool playOnEnable = true;

    private Coroutine _typingRoutine;

    public bool PlayOnEnable => playOnEnable;

    private void OnEnable()
    {
        if (playOnEnable)
            PlayConfiguredText();
    }

    private void OnDisable()
    {
        StopTyping();
    }

    public void PlayConfiguredText()
    {
        PlayText(message);
    }

    public void PlayText(string textToType)
    {
        if (targetText == null)
        {
            Debug.LogWarning("[BetweenLevels] No TextMeshProUGUI assigned.");
            return;
        }

        if (_typingRoutine != null)
            StopCoroutine(_typingRoutine);

        _typingRoutine = StartCoroutine(TypeRoutine(textToType ?? string.Empty));
    }

    public void StopTyping()
    {
        if (_typingRoutine != null)
        {
            StopCoroutine(_typingRoutine);
            _typingRoutine = null;
        }

        StopTypingSound();
    }

    public void ClearText()
    {
        StopTyping();

        if (targetText != null)
            targetText.text = string.Empty;
    }

    private IEnumerator TypeRoutine(string fullText)
    {
        targetText.text = string.Empty;
        StartTypingSound();

        for (int i = 0; i < fullText.Length; i++)
        {
            targetText.text += fullText[i];
            yield return new WaitForSeconds(typingSpeed);
        }

        StopTypingSound();
        _typingRoutine = null;
    }

    private void StartTypingSound()
    {
        if (typingAudioSource == null || typingSound == null)
            return;

        typingAudioSource.clip = typingSound;
        typingAudioSource.loop = true;
        typingAudioSource.Play();
    }

    private void StopTypingSound()
    {
        if (typingAudioSource == null)
            return;

        if (typingAudioSource.isPlaying)
            typingAudioSource.Stop();

        typingAudioSource.loop = false;
    }
}
