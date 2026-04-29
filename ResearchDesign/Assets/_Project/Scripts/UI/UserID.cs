using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_InputField))]
public class UserID : MonoBehaviour
{
    [Serializable]
    private class UserIdPayload
    {
        public string id;
        public string participant_id;
        public string current_level;
    }

    [Header("Input")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Local Save")]
    [SerializeField] private string playerPrefsKey = "agentic_browser_user_id";
    [SerializeField] private bool loadSavedUserIdOnStart = true;
    [SerializeField] private bool saveUserIdLocally = true;
    [SerializeField] private bool clearInputAfterSuccessfulSubmit;

    [Header("Firebase Bridge")]
    [SerializeField] private bool sendToFirebaseWhenAvailable = true;
    [SerializeField] private FirebaseManager firebaseManager;

    private bool isSubmitting;
    private string pendingUploadId = string.Empty;

    private void Reset()
    {
        inputField = GetComponent<TMP_InputField>();
        firebaseManager = FindFirstObjectByType<FirebaseManager>();
    }

    private void Awake()
    {
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();

        ResolveFirebaseManager();
    }

    private void Start()
    {
        if (!loadSavedUserIdOnStart || inputField == null)
            return;

        var rememberedUserId = GetRememberedUserId();
        if (rememberedUserId.Length == 0)
            return;

        inputField.text = rememberedUserId;
        ApplyRememberedUserIdToRuntime(rememberedUserId);
    }

    private void OnEnable()
    {
        ResolveFirebaseManager();

        if (inputField != null)
            inputField.onSubmit.AddListener(OnSubmitted);

        if (submitButton != null)
            submitButton.onClick.AddListener(SubmitCurrentInput);

        if (firebaseManager != null)
            firebaseManager.OnBridgeResponseReceived += OnFirebaseBridgeResponseReceived;
    }

    private void OnDisable()
    {
        if (inputField != null)
            inputField.onSubmit.RemoveListener(OnSubmitted);

        if (submitButton != null)
            submitButton.onClick.RemoveListener(SubmitCurrentInput);

        if (firebaseManager != null)
            firebaseManager.OnBridgeResponseReceived -= OnFirebaseBridgeResponseReceived;
    }

    private void ResolveFirebaseManager()
    {
        if (firebaseManager != null)
            return;

        firebaseManager = FirebaseManager.Instance;
        if (firebaseManager == null)
            firebaseManager = FindFirstObjectByType<FirebaseManager>();
    }

    private void OnSubmitted(string submittedText)
    {
        TrySubmit(submittedText);
    }

    public void SubmitCurrentInput()
    {
        var currentText = inputField != null ? inputField.text : string.Empty;
        TrySubmit(currentText);
    }

    public string GetSavedUserId()
    {
        return GetRememberedUserId();
    }

    private void TrySubmit(string rawValue)
    {
        if (isSubmitting)
            return;

        var userId = (rawValue ?? string.Empty).Trim();
        if (userId.Length == 0)
        {
            UpdateStatus("Enter a user ID first.");
            return;
        }

        var savedLocally = SaveLocally(userId);
        ApplyRememberedUserIdToRuntime(userId);

        GameManager.Instance?.RecordAction($"user_id_submitted:{userId}");
        StartCoroutine(SubmitRoutine(userId, savedLocally));
    }

    private IEnumerator SubmitRoutine(string userId, bool savedLocally)
    {
        isSubmitting = true;
        SetInteractable(false);

        if (!sendToFirebaseWhenAvailable)
        {
            GameManager.Instance?.RecordAction("user_id_saved_local_only");
            UpdateStatus(savedLocally
                ? "Saved locally. Firebase upload is off."
                : "Firebase upload is off. Local save is off.");
            FinishSubmit(success: true);
            yield break;
        }

        ResolveFirebaseManager();
        if (firebaseManager != null)
        {
            pendingUploadId = userId;
            GameManager.Instance?.RecordAction("user_id_upload_requested");
            UpdateStatus(savedLocally
                ? "Saved locally. Sending to Firebase..."
                : "Sending to Firebase...");
            firebaseManager.UploadData(BuildPayloadJson(userId));
            FinishSubmit(success: true);
            yield break;
        }

        GameManager.Instance?.RecordAction("user_id_firebase_not_enabled");
        UpdateStatus(savedLocally
            ? "Saved locally. Firebase bridge not found yet."
            : "Firebase bridge not found and local save is off.");
        FinishSubmit(success: true);
    }

    private void OnFirebaseBridgeResponseReceived(FirebaseManager.BridgeResponse response)
    {
        if (response == null)
            return;

        if (!string.Equals(response.type, "save", StringComparison.OrdinalIgnoreCase))
            return;

        if (!string.IsNullOrEmpty(pendingUploadId)
            && !string.IsNullOrEmpty(response.documentId)
            && !string.Equals(pendingUploadId, response.documentId, StringComparison.Ordinal))
            return;

        if (string.Equals(response.status, "success", StringComparison.OrdinalIgnoreCase))
        {
            pendingUploadId = string.Empty;
            GameManager.Instance?.RecordAction("user_id_uploaded_firebase");
            UpdateStatus("User ID saved to Firebase.");
            return;
        }

        if (string.Equals(response.status, "error", StringComparison.OrdinalIgnoreCase))
        {
            pendingUploadId = string.Empty;
            GameManager.Instance?.RecordAction("user_id_upload_failed");
            UpdateStatus(saveUserIdLocally
                ? "Saved locally. Firebase upload failed. Check browser console."
                : "Firebase upload failed. Check browser console.");
        }
    }

    private string BuildPayloadJson(string userId)
    {
        var payload = new UserIdPayload
        {
            id = userId,
            participant_id = userId,
            current_level = SceneManager.GetActiveScene().name
        };

        return JsonUtility.ToJson(payload);
    }

    private bool SaveLocally(string userId)
    {
        if (!saveUserIdLocally)
            return false;

        PlayerPrefs.SetString(playerPrefsKey, userId);
        PlayerPrefs.Save();
        return true;
    }

    private string GetRememberedUserId()
    {
        var sessionData = GameSessionData.Instance;
        if (sessionData != null && !string.IsNullOrWhiteSpace(sessionData.CurrentParticipantId))
            return sessionData.CurrentParticipantId.Trim();

        var telemetryManager = FirebaseTelemetryManager.Instance;
        var telemetryUserId = telemetryManager != null ? telemetryManager.CurrentParticipantID : string.Empty;
        if (!string.IsNullOrWhiteSpace(telemetryUserId))
            return telemetryUserId.Trim();

        return string.Empty;
    }

    private void ApplyRememberedUserIdToRuntime(string userId)
    {
        var normalizedUserId = (userId ?? string.Empty).Trim();
        if (normalizedUserId.Length == 0)
            return;

        GameSessionData.Instance?.SetParticipantId(normalizedUserId);
        FirebaseTelemetryManager.Instance?.SetCurrentParticipantID(normalizedUserId);
    }

    private void FinishSubmit(bool success)
    {
        if (success && clearInputAfterSuccessfulSubmit && inputField != null)
            inputField.text = string.Empty;

        SetInteractable(true);

        if (inputField != null)
            inputField.ActivateInputField();

        isSubmitting = false;
    }

    private void SetInteractable(bool isInteractable)
    {
        if (inputField != null)
            inputField.interactable = isInteractable;

        if (submitButton != null)
            submitButton.interactable = isInteractable;
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
