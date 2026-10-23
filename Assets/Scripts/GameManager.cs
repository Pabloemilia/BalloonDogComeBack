using System;
using TMPro;
using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    public const string BestScorePreferenceKey = "BalloonDog.BestScore";
    private const string EndlessBestResetKey = "BalloonDog.BestScore.EndlessEconomy.v1";

    [Header("Optional UI References")]
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TMP_Text endTitleText;
    [SerializeField] private TMP_Text endSummaryText;

    private bool isGameOver;
    private BalloonDogContinuePrompt continuePrompt;
    private BalloonDogDeathAnimation deathAnimation;
    private bool runStarted;
    private bool rewardsSettled;
    private bool doubleRewardClaimed;
    public bool IsContinuePending { get; private set; }
    public bool IsResultFinalized => isGameOver && !IsContinuePending;

    public static GameManager Instance { get; private set; }
    public static int BestScore
    {
        get
        {
            // Reset the prototype record once, never on every launch or replay.
            if (PlayerPrefs.GetInt(EndlessBestResetKey, 0) != 1)
            {
                PlayerPrefs.SetInt(BestScorePreferenceKey, 0);
                PlayerPrefs.SetInt(EndlessBestResetKey, 1);
                PlayerPrefs.Save();
            }
            return Mathf.Max(0, PlayerPrefs.GetInt(BestScorePreferenceKey, 0));
        }
    }
    public bool IsGameOver => isGameOver;
    public bool LastRunCompleted { get; private set; }
    public int LastScore { get; private set; }
    public double LastDistance { get; private set; }
    public int LastCoinsReward { get; private set; }
    public bool IsDoubleRewardClaimed => doubleRewardClaimed;
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
        Time.timeScale = BalloonDogSceneFlow.IsBusy ? 0f : 1f;

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
        SnapshotRun();
        LastRunCompleted = false;
        LastEndReason = reason;
        LastEndTitle = "GAME OVER";
        LastEndSummary = $"{reason}\nScore: {LastScore}\nBest: {BestScore}";
        IsContinuePending = true;
        Time.timeScale = 0f;
        CameraShakeController.ShakeGlobal(0.32f, 0.18f);
        continuePrompt = GetComponent<BalloonDogContinuePrompt>();
        if (continuePrompt == null) continuePrompt = gameObject.AddComponent<BalloonDogContinuePrompt>();
        deathAnimation = GetComponent<BalloonDogDeathAnimation>();
        if (deathAnimation == null) deathAnimation = gameObject.AddComponent<BalloonDogDeathAnimation>();
        PlayerRunner player = FindAnyObjectByType<PlayerRunner>();
        bool animated = false;
        try
        {
            animated = player != null && deathAnimation.Play(player.transform, () =>
            {
                // A restart, early result or revive must never open a stale offer.
                if (IsContinuePending) continuePrompt.Show(this);
            });
        }
        catch (Exception error)
        {
            // A visual-only failure must never leave the frozen run without UI.
            deathAnimation.Restore();
            Debug.LogWarning("Death animation skipped: " + error.Message, this);
        }
        if (!animated) continuePrompt.Show(this);
    }

    // Legacy scene/editor integrations may still call these methods. Endless
    // runs have no completion state or completion UI, and these calls do nothing.
    public void TriggerLevelComplete(int baseScore, float launchDistance, int multiplier, int finalScore) { }
    public void TriggerLevelComplete(int score) { }

    public void ExpireContinueOffer()
    {
        if (!IsContinuePending) return;
        IsContinuePending = false;
        deathAnimation?.Finish();
        if (continuePrompt != null) continuePrompt.Close();
        SaveBestScore(LastScore);
        SettleRunRewards();
        LastEndSummary = $"{LastEndReason}\nScore: {LastScore}\nBest: {BestScore}";
        ShowEndPanel(LastEndTitle, LastEndSummary);
        Time.timeScale = 0f;
        GameEnded?.Invoke();
    }

    // Only the rewarded-ad success callback may resume the current run.
    public void CompleteRewardedContinue()
    {
        if (!IsContinuePending) return;
        AirController air = FindAnyObjectByType<AirController>();
        PlayerRunner runner = FindAnyObjectByType<PlayerRunner>();
        if (air == null || runner == null) { ExpireContinueOffer(); return; }
        IsContinuePending = false;
        isGameOver = false;
        if (continuePrompt != null) continuePrompt.Close();
        if (endPanel != null) endPanel.SetActive(false);
        air.RestoreAfterContinue();
        runner.RestoreAfterContinue();
        runner.GetComponent<BalloonSizeController>()?.SnapToCurrentAir();
        deathAnimation?.Restore();
        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        deathAnimation?.Restore();
        SettleBeforeLeaving();
        BalloonDogSceneFlow.ReloadGame(true);
    }

    public void ReturnToMainMenu()
    {
        deathAnimation?.Restore();
        SettleBeforeLeaving();
        BalloonDogSceneFlow.ReloadGame(false);
    }

    public void BeginRun() => runStarted = true;

    private void SnapshotRun()
    {
        ScoreController scoreController = FindAnyObjectByType<ScoreController>();
        LastScore = scoreController != null ? scoreController.CurrentScore : 0;
        LastDistance = scoreController != null ? scoreController.DistanceTravelled : 0d;
    }

    private void SettleRunRewards()
    {
        if (rewardsSettled) return;
        rewardsSettled = true;
        LastCoinsReward = BalloonDogEconomy.CalculateRunCoins(LastDistance);
        // Gems are reserved for purchases and successful rewarded ads.
        BalloonDogEconomy.AddCoins(LastCoinsReward);
    }

    private void SettleBeforeLeaving()
    {
        if (rewardsSettled || !runStarted && !isGameOver) return;
        if (!isGameOver) SnapshotRun();
        SaveBestScore(LastScore);
        SettleRunRewards();
    }

    public bool TryClaimDoubleRunReward()
    {
        if (!IsResultFinalized || !rewardsSettled || doubleRewardClaimed
            || LastCoinsReward == 0) return false;
        doubleRewardClaimed = true;
        BalloonDogEconomy.AddCoins(LastCoinsReward);
        return true;
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
