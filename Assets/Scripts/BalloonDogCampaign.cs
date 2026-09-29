using System;
using UnityEngine;

/// <summary>
/// Small, local-only campaign save. A single gameplay scene is reused while
/// BalloonDogLevelDirector builds the selected level from deterministic rules.
/// </summary>
public static class BalloonDogCampaign
{
    public const int LevelCount = 1;

    private const string SelectedLevelKey = "BalloonDog.Campaign.SelectedLevel";
    private const string UnlockedLevelKey = "BalloonDog.Campaign.UnlockedLevel";
    private const string CompletedLevelKey = "BalloonDog.Campaign.CompletedLevel";

    public static event Action Changed;

    public static int CurrentLevel
    {
        get
        {
            EnsureInitialized();
            return Mathf.Clamp(
                PlayerPrefs.GetInt(SelectedLevelKey, 1),
                1,
                UnlockedLevel);
        }
    }

    public static int UnlockedLevel
    {
        get
        {
            EnsureInitialized();
            return Mathf.Clamp(
                PlayerPrefs.GetInt(UnlockedLevelKey, 1),
                1,
                LevelCount);
        }
    }

    public static int HighestCompletedLevel
    {
        get
        {
            EnsureInitialized();
            return Mathf.Clamp(
                PlayerPrefs.GetInt(CompletedLevelKey, 0),
                0,
                LevelCount);
        }
    }

    public static bool IsCampaignComplete => HighestCompletedLevel >= LevelCount;

    public static void SelectLevel(int level)
    {
        EnsureInitialized();
        int safeLevel = Mathf.Clamp(level, 1, UnlockedLevel);
        PlayerPrefs.SetInt(SelectedLevelKey, safeLevel);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void MarkCurrentLevelComplete()
    {
        EnsureInitialized();
        int completed = Mathf.Max(HighestCompletedLevel, CurrentLevel);
        int unlocked = Mathf.Clamp(Mathf.Max(UnlockedLevel, CurrentLevel + 1), 1, LevelCount);
        PlayerPrefs.SetInt(CompletedLevelKey, completed);
        PlayerPrefs.SetInt(UnlockedLevelKey, unlocked);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void SelectNextLevel()
    {
        EnsureInitialized();
        SelectLevel(Mathf.Min(CurrentLevel + 1, UnlockedLevel));
    }

    public static string GetLevelName(int level)
    {
        return "OBSTACLE RUN";
    }

    private static void EnsureInitialized()
    {
        if (!PlayerPrefs.HasKey(UnlockedLevelKey))
        {
            PlayerPrefs.SetInt(UnlockedLevelKey, 1);
            PlayerPrefs.SetInt(SelectedLevelKey, 1);
            PlayerPrefs.SetInt(CompletedLevelKey, 0);
            PlayerPrefs.Save();
        }
    }
}
