using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour
{
    public const string BestScorePreferenceKey = "BalloonDog.BestScore";

    [Header("Optional UI References")]
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TMP_Text endTitleText;
    [SerializeField] private TMP_Text endSummaryText;

    private bool isGameOver;

    public static GameManager Instance { get; private set; }
    public static int BestScore => PlayerPrefs.GetInt(BestScorePreferenceKey, 0);
    public bool IsGameOver => isGameOver;
    public bool LastRunCompleted { get; private set; }
    public int LastScore { get; private set; }
    public string LastEndReason { get; private set; } = string.Empty;
    public string LastEndTitle { get; private set; } = string.Empty;
    public string LastEndSummary { get; private set; } = string.Empty;

    public event Action GameEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;

        if (endPanel != null)
        {
            endPanel.SetActive(false);
        }
    }

    public void ConfigureEndPanel(
        GameObject panel,
        TMP_Text titleText,
        TMP_Text summaryText)
    {
        endPanel = panel;
        endTitleText = titleText;
        endSummaryText = summaryText;

        if (!isGameOver && endPanel != null)
        {
            endPanel.SetActive(false);
        }
    }

    public void TriggerGameOver(string reason = "HAVA BİTTİ")
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        int score = ResolveCurrentScore();
        SaveBestScore(score);
        LastRunCompleted = false;
        LastScore = score;
        LastEndReason = reason;
        LastEndTitle = "RUN OVER";
        LastEndSummary = $"{reason}\nSkor: {score}\nEn iyi: {BestScore}";
        ShowEndPanel(LastEndTitle, LastEndSummary);
        CameraShakeController.ShakeGlobal(0.32f, 0.18f);
        Time.timeScale = 0f;
        GameEnded?.Invoke();
    }

    public void TriggerLevelComplete(
        int baseScore,
        float launchDistance,
        int multiplier,
        int finalScore)
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        SaveBestScore(finalScore);
        LastRunCompleted = true;
        LastScore = finalScore;
        LastEndReason = "FINISH REACHED";
        LastEndTitle = "LEVEL COMPLETE";
        LastEndSummary =
            $"x{multiplier} MULTIPLIER\n" +
            $"Launch: {launchDistance:0.0} m\n" +
            $"Score: {baseScore} → {finalScore}\n" +
            $"Best: {BestScore}";
        ShowEndPanel(LastEndTitle, LastEndSummary);
        Time.timeScale = 0f;
        GameEnded?.Invoke();
    }

    public void TriggerLevelComplete(int score)
    {
        TriggerLevelComplete(score, 0f, 1, score);
    }

    public void RestartGame()
    {
        MainMenuController.StartImmediatelyOnNextSceneLoad = true;
        ReloadCurrentScene();
    }

    public void ReturnToMainMenu()
    {
        MainMenuController.StartImmediatelyOnNextSceneLoad = false;
        ReloadCurrentScene();
    }

    private int ResolveCurrentScore()
    {
        ScoreController scoreController = FindAnyObjectByType<ScoreController>();
        return scoreController != null ? scoreController.CurrentScore : 0;
    }

    private static void SaveBestScore(int score)
    {
        if (score <= BestScore)
        {
            return;
        }

        PlayerPrefs.SetInt(BestScorePreferenceKey, score);
        PlayerPrefs.Save();
    }

    private void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        isGameOver = false;

        Scene currentScene = SceneManager.GetActiveScene();

        if (!string.IsNullOrWhiteSpace(currentScene.name))
        {
            SceneManager.LoadScene(currentScene.name, LoadSceneMode.Single);
            return;
        }

        if (currentScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(currentScene.buildIndex, LoadSceneMode.Single);
        }
    }

    private void ShowEndPanel(string title, string summary)
    {
        if (endTitleText != null)
        {
            endTitleText.text = title;
        }

        if (endSummaryText != null)
        {
            endSummaryText.text = summary;
        }

        if (endPanel != null)
        {
            endPanel.transform.SetAsLastSibling();
            endPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"{title} - {summary}", this);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
