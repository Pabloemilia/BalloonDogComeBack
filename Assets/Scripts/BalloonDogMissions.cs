using System;
using UnityEngine;

/// <summary>Local save adapter. Run earnings alone advance coin missions, never wallet rewards.</summary>
public static class BalloonDogMissions
{
    public const string SaveKey = "BalloonDog.Missions.v1";
    private static BalloonDogMissionProgress progress;
    private static float lastSave;
    public static BalloonDogMissionProgress Progress { get { EnsureDay(); return progress; } }
    public static int Scale => BalloonDogEconomy.CoinScale;
    public static int OwnedSkins
    {
        get
        {
            int count = 0;
            foreach (var skin in BalloonDogEconomy.Skins) if (BalloonDogEconomy.IsOwned(skin.Id)) count++;
            return count;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntime() { progress = null; lastSave = 0f; }

    public static void EnsureDay()
    {
        if (progress == null)
        {
            try { progress = JsonUtility.FromJson<BalloonDogMissionProgress>(PlayerPrefs.GetString(SaveKey, "")); }
            catch (ArgumentException) { progress = null; }
            if (progress == null || progress.Lifetime == null || progress.Daily == null ||
                progress.Stages == null || progress.Stages.Length != 12 || progress.DailyIds == null ||
                progress.DailyIds.Length != 3 || progress.DailyClaimed == null || progress.DailyClaimed.Length != 3)
                progress = new BalloonDogMissionProgress();
            if (progress.Daily.Skins == null) progress.Daily.Skins = new System.Collections.Generic.List<string>();
            progress.SyncCases(BalloonDogEconomy.CasesOpened);
        }
        string previousDay = progress.Day;
        int reward = progress.EnsureDay(DateTime.UtcNow, OwnedSkins,
            !BalloonDogEconomy.AllSkinsOwned && BalloonDogEconomy.Coins >= BalloonDogEconomy.CasePrice, Scale);
        if (previousDay != progress.Day)
        {
            Save(false);
            if (reward > 0) BalloonDogEconomy.AddCoins(reward);
            else PlayerPrefs.Save();
        }
    }
    public static void BeginRun()
    {
        EnsureDay();
        progress.BeginRun(BalloonDogEconomy.EquippedSkinId);
        Save();
    }
    public static void TickRun(float delta, int coins)
    {
        if (progress == null || !progress.RunActive) return;
        progress.ProgressRun(delta,coins);
        if (Time.unscaledTime - lastSave >= 5f) Save();
    }
    public static void CollectBalloon()
    {
        if (progress == null || !progress.RunActive) return;
        progress.CollectBalloon();
    }
    public static void EndRun(int coins)
    {
        if (progress == null || !progress.RunActive) return;
        progress.ProgressRun(0,coins);
        progress.EndRun();
        Save();
        EnsureDay();
    }
    public static void OpenCase()
    {
        EnsureDay();
        progress.OpenCase();
        Save();
    }
    public static bool ClaimDaily(int slot)
    {
        EnsureDay();
        int coins = progress.ClaimDaily(slot,Scale);
        if (coins <= 0) return false;
        Save(false);
        BalloonDogEconomy.AddCoins(coins); // saves the claim and wallet in the same PlayerPrefs flush
        return true;
    }
    public static bool ClaimBonus()
    {
        EnsureDay();
        int coins = progress.ClaimBonus(Scale);
        if (coins <= 0) return false;
        Save(false); BalloonDogEconomy.AddCoins(coins); return true;
    }
    public static bool ClaimNormal(int id)
    {
        EnsureDay();
        if (!progress.ClaimNormal(id,OwnedSkins,Scale,out int coins,out int gems)) return false;
        Save(false); BalloonDogEconomy.AddRewards(coins,gems); return true;
    }
    public static void Save(bool flush = true)
    {
        if (progress == null) return;
        PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(progress));
        if (flush) PlayerPrefs.Save();
        lastSave = Time.unscaledTime;
    }
}
