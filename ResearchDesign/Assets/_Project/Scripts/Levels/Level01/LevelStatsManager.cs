using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelStatsManager : MonoBehaviour
{
    [Header("UI Display (Optional)")]
    [Tooltip("Optional TextMeshPro element to display the final score summary when the level ends.")]
    public TextMeshProUGUI statsDisplay;

    [Header("Scoring Weights")]
    [Tooltip("Starting score before any penalties are applied.")]
    public int baseScore = 10000;
    [Tooltip("Points deducted per second spent in the level.")]
    public int timePenaltyPerSecond = 10;
    [Tooltip("Points deducted per mistake made.")]
    public int mistakePenalty = 100;
    [Tooltip("Points deducted per hint used. Higher than mistake penalty to encourage exploration.")]
    public int hintPenalty = 500;

    private float _startTime;
    private int _mistakes = 0;
    private int _hintsUsed = 0;
    private bool _isLevelActive = false;

    private void Start()
    {
        StartTracking();
    }

    public void StartTracking()
    {
        _startTime = Time.time;
        _mistakes = 0;
        _hintsUsed = 0;
        _isLevelActive = true;

        GameManager.Instance?.RecordAction($"telemetry_level_tracking_started:{SceneManager.GetActiveScene().name}");
    }

    public void RegisterMistake()
    {
        if (!_isLevelActive) return;
        _mistakes++;
    }

    public void RegisterHint()
    {
        if (!_isLevelActive) return;
        _hintsUsed++;
    }

    public void FinishLevel()
    {
        if (!_isLevelActive) return;
        _isLevelActive = false;

        float totalTime = Time.time - _startTime;
        int minutes = Mathf.FloorToInt(totalTime / 60f);
        int seconds = Mathf.FloorToInt(totalTime % 60f);
        int roundedSeconds = Mathf.RoundToInt(totalTime);

        int finalScore = Mathf.Max(0,
            baseScore
            - (roundedSeconds * timePenaltyPerSecond)
            - (_mistakes * mistakePenalty)
            - (_hintsUsed * hintPenalty));

        string summary = $"<b>SCORE: {finalScore}</b> | Time: {minutes:00}:{seconds:00} | Mistakes: {_mistakes} | Hints: {_hintsUsed}";

        GameManager.Instance?.RecordAction("telemetry_level_completed");
        GameManager.Instance?.RecordAction($"telemetry_level_scene:{SceneManager.GetActiveScene().name}");
        GameManager.Instance?.RecordAction($"telemetry_level_score:{finalScore}");
        GameManager.Instance?.RecordAction($"telemetry_level_time_seconds:{roundedSeconds}");
        GameManager.Instance?.RecordAction($"telemetry_level_mistakes:{_mistakes}");
        GameManager.Instance?.RecordAction($"telemetry_level_hints:{_hintsUsed}");

        if (statsDisplay != null)
            statsDisplay.text = $"Level Complete!\n{summary.Replace(" | ", "\n")}";
    }
}
