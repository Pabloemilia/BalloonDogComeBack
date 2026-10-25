using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>Serializable mission rules. Coins are supplied in the wallet's current units.</summary>
[Serializable]
public sealed class BalloonDogMissionProgress
{
    [Serializable]
    public sealed class Stats
    {
        public int Coins, BestCoins, Runs, Balloons, BestBalloons, Cases, DailyCompleted, FullDays, LongRuns;
        public double Seconds, BestSeconds;
        public List<string> Skins = new List<string>();
    }

    public string Day = "";
    public Stats Lifetime = new Stats();
    public Stats Daily = new Stats();
    public int[] DailyIds = new int[3];
    public bool[] DailyClaimed = new bool[3];
    public bool BonusClaimed;
    public int[] Stages = new int[12];
    [NonSerialized] private double runSeconds;
    [NonSerialized] private int runCoins, runBalloons;
    [NonSerialized] private bool longRunCounted;
    [NonSerialized] public bool RunActive;

    // Daily IDs: G1,G2,G3,G4,G5,G8,G9,G10,G11. Shrink tasks are intentionally absent.
    private static readonly string[] DailyFormats = {
        "Play {0} runs", "Collect {0} coins", "Collect {0} coins in one run",
        "Survive {0} seconds in one run", "Survive {0} seconds in total",
        "Collect {0} air balloons", "Collect {0} air balloons in one run",
        "Run with {0} different skins", "Open {0} case"
    };
    private static readonly int[][] Targets = {
        new[] {500,1500,3000,7500,15000}, new[] {50,100,200,350,500},
        new[] {10,25,50,100,200}, new[] {60,120,180,300,450},
        new[] {600,1800,3600,7200,14400}, new[] {50,150,400,1000,2000},
        new[] {5,10,20,35,50}, new[] {2,4,6,10,16},
        new[] {3,5,8,12,15}, new[] {5,15,40,100,200},
        new[] {1,3,7,15,30}, new[] {3,10,25,50,100}
    };
    private static readonly string[] NormalFormats = {
        "Collect {0} coins in total", "Collect {0} coins in one run", "Play {0} runs",
        "Survive {0} seconds in one run", "Survive {0} minutes in total",
        "Collect {0} air balloons", "Collect {0} air balloons in one run",
        "Own {0} different skins", "Open {0} cases", "Complete {0} daily missions",
        "Complete all dailies on {0} days", "Finish {0} runs lasting 120 seconds"
    };

    public static string DayKey(DateTime utc) => utc.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public static DateTime NextReset(DateTime utc) => utc.ToUniversalTime().Date.AddDays(1); // 03:00 Turkey
    private static int Add(int current, int value) => (int)Math.Min(int.MaxValue, (long)current + Math.Max(0, value));

    /// <summary>Returns unclaimed, completed coin rewards from the expired day.</summary>
    public int EnsureDay(DateTime utc, int ownedSkins, bool canOpenCase, int coinScale)
    {
        string key = DayKey(utc);
        // Moving the device clock backwards cannot reopen an earlier day's rewards.
        if (string.CompareOrdinal(key, Day) <= 0 || RunActive) return 0;
        int automaticCoins = 0;
        if (!string.IsNullOrEmpty(Day))
        {
            for (int slot = 0; slot < 3; slot++)
                if (CanClaimDaily(slot, coinScale)) automaticCoins = Add(automaticCoins, ClaimDaily(slot, coinScale));
            if (CanClaimBonus) automaticCoins = Add(automaticCoins, ClaimBonus(coinScale));
        }
        int[] previous = DailyIds;
        // Stable date seed: reopening the app never rerolls today's missions.
        int seed = 0;
        foreach (char c in key) seed = unchecked(seed * 31 + c);
        var random = new Random(seed);
        var groups = new[] { new List<int> {0,1,5}, new List<int> {3,4}, new List<int> {2,6} };
        if (ownedSkins >= 2) groups[1].Add(7);
        if (canOpenCase) groups[2].Add(8);
        DailyIds = new int[3];
        for (int slot = 0; slot < 3; slot++)
        {
            var choices = new List<int>(groups[slot]);
            if (!string.IsNullOrEmpty(Day)) choices.Remove(previous[slot]);
            DailyIds[slot] = choices[random.Next(choices.Count)];
        }
        Day = key;
        Daily = new Stats();
        DailyClaimed = new bool[3];
        BonusClaimed = false;
        return automaticCoins;
    }

    public void BeginRun(string skin)
    {
        if (RunActive) return;
        RunActive = true;
        runSeconds = 0; runCoins = runBalloons = 0; longRunCounted = false;
        Lifetime.Runs = Add(Lifetime.Runs, 1); Daily.Runs = Add(Daily.Runs, 1);
        if (!Daily.Skins.Contains(skin)) Daily.Skins.Add(skin);
    }

    public void ProgressRun(double secondsDelta, int earnedCoins)
    {
        if (!RunActive || double.IsNaN(secondsDelta) || double.IsInfinity(secondsDelta)) return;
        double delta = Math.Max(0, secondsDelta);
        int coinsDelta = Math.Max(0, earnedCoins - runCoins);
        runCoins = Math.Max(runCoins, earnedCoins);
        runSeconds += delta;
        ApplyProgress(Lifetime,delta,coinsDelta);
        ApplyProgress(Daily,delta,coinsDelta);
        if (!longRunCounted && runSeconds >= 120d)
        {
            Lifetime.LongRuns = Add(Lifetime.LongRuns, 1);
            Daily.LongRuns = Add(Daily.LongRuns, 1);
            longRunCounted = true;
        }
    }

    public void CollectBalloon()
    {
        if (!RunActive) return;
        runBalloons = Add(runBalloons, 1);
        ApplyBalloon(Lifetime);
        ApplyBalloon(Daily);
    }
    private void ApplyProgress(Stats stats, double delta, int coinsDelta)
    {
        stats.Coins = Add(stats.Coins, coinsDelta);
        stats.Seconds += delta;
        stats.BestCoins = Math.Max(stats.BestCoins,runCoins);
        stats.BestSeconds = Math.Max(stats.BestSeconds,runSeconds);
    }
    private void ApplyBalloon(Stats stats)
    {
        stats.Balloons = Add(stats.Balloons,1);
        stats.BestBalloons = Math.Max(stats.BestBalloons,runBalloons);
    }
    public void EndRun() => RunActive = false;
    public void OpenCase() { Lifetime.Cases = Add(Lifetime.Cases,1); Daily.Cases = Add(Daily.Cases,1); }
    public void SyncCases(int cases) => Lifetime.Cases = Math.Max(Lifetime.Cases, cases);

    public int DailyTarget(int slot, int scale)
    {
        switch (DailyIds[slot])
        {
            case 0: return 3; case 1: return 300 * scale; case 2: return 100 * scale;
            case 3: return 90; case 4: return 300; case 5: return 20;
            case 6: return 8; case 7: return 2; default: return 1;
        }
    }
    public double DailyValue(int slot)
    {
        switch (DailyIds[slot])
        {
            case 0: return Daily.Runs; case 1: return Daily.Coins; case 2: return Daily.BestCoins;
            case 3: return Daily.BestSeconds; case 4: return Daily.Seconds; case 5: return Daily.Balloons;
            case 6: return Daily.BestBalloons; case 7: return Daily.Skins.Count; default: return Daily.Cases;
        }
    }
    public string DailyTitle(int slot, int scale) => string.Format(CultureInfo.InvariantCulture, DailyFormats[DailyIds[slot]], DailyTarget(slot, scale).ToString("N0", CultureInfo.InvariantCulture));
    public int DailyReward(int slot, int scale) => new[] {30,60,100}[slot] * scale;
    public bool CanClaimDaily(int slot, int scale) => slot >= 0 && slot < 3 && !DailyClaimed[slot] && DailyValue(slot) >= DailyTarget(slot, scale);
    public int ClaimDaily(int slot, int scale)
    {
        if (!CanClaimDaily(slot, scale)) return 0;
        DailyClaimed[slot] = true;
        Lifetime.DailyCompleted = Add(Lifetime.DailyCompleted, 1);
        return DailyReward(slot, scale);
    }
    public int CompletedDailies(int scale)
    {
        int count = 0;
        for (int i=0; i<3; i++) if (DailyClaimed[i] || DailyValue(i) >= DailyTarget(i,scale)) count++;
        return count;
    }
    public bool CanClaimBonus => !BonusClaimed && DailyClaimed[0] && DailyClaimed[1] && DailyClaimed[2];
    public int ClaimBonus(int scale)
    {
        if (!CanClaimBonus) return 0;
        BonusClaimed = true;
        Lifetime.FullDays = Add(Lifetime.FullDays,1);
        return 60 * scale;
    }
    public bool Finished(int id) => (id == 7 || id == 8) && Stages[id] >= 5;
    public int NormalTarget(int id, int scale)
    {
        int stage = Finished(id) ? 4 : Stages[id];
        double target = Targets[id][Math.Min(stage,4)];
        if (stage > 4) target *= Math.Pow(1.5d, stage - 4);
        if (id == 0 || id == 1) target *= scale;
        return (int)Math.Min(int.MaxValue, Math.Ceiling(target));
    }
    public double NormalValue(int id, int ownedSkins)
    {
        switch (id)
        {
            case 0: return Lifetime.Coins; case 1: return Lifetime.BestCoins; case 2: return Lifetime.Runs;
            case 3: return Lifetime.BestSeconds; case 4: return Lifetime.Seconds; case 5: return Lifetime.Balloons;
            case 6: return Lifetime.BestBalloons; case 7: return ownedSkins; case 8: return Lifetime.Cases;
            case 9: return Lifetime.DailyCompleted; case 10: return Lifetime.FullDays; default: return Lifetime.LongRuns;
        }
    }
    public string NormalTitle(int id, int scale)
    {
        int target = NormalTarget(id,scale);
        if (id == 4) target /= 60;
        return string.Format(CultureInfo.InvariantCulture, NormalFormats[id], target.ToString("N0", CultureInfo.InvariantCulture));
    }
    public bool CanClaimNormal(int id, int ownedSkins, int scale) => id >= 0 && id < 12 && !Finished(id) && NormalValue(id,ownedSkins) >= NormalTarget(id,scale);
    public int NormalCoinReward(int id, int scale) => Stages[id] == 4 || Finished(id) ? 0 : Math.Min(300, (Stages[id]+1)*50)*scale;
    public int NormalGemReward(int id) => Stages[id] == 4 ? 5 : 0;
    public bool ClaimNormal(int id, int ownedSkins, int scale, out int coins, out int gems)
    {
        coins = gems = 0;
        if (!CanClaimNormal(id,ownedSkins,scale)) return false;
        coins = NormalCoinReward(id,scale); gems = NormalGemReward(id);
        Stages[id]++;
        return true;
    }
}
