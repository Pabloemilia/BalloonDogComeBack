using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour
{
    [Header("Optional UI References")]
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TMP_Text endTitleText;
    [SerializeField] private TMP_Text endSummaryText;

    private bool isGameOver;

    public static GameManager Instance { get; private set; }
    public bool IsGameOver => isGameOver;

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
        ShowEndPanel("ÖLDÜN", reason);
        Time.timeScale = 0f;
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
        ShowEndPanel(
            $"x{multiplier} ÇARPAN!",
            $"Fırlatma: {launchDistance:0.0} m\n" +
            $"Skor: {baseScore} → {finalScore}");
        Time.timeScale = 0f;
    }

    // Eski FinishLine sürümüyle uyumluluk.
    public void TriggerLevelComplete(int score)
    {
        TriggerLevelComplete(score, 0f, 1, score);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        isGameOver = false;

        Scene currentScene = SceneManager.GetActiveScene();

        if (currentScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(currentScene.buildIndex, LoadSceneMode.Single);
            return;
        }

        SceneManager.LoadScene(currentScene.name, LoadSceneMode.Single);
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
