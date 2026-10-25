using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BalloonDogMissionsTests
{
    private object state;
    private Type type;
    private readonly DateTime day = new DateTime(2026,10,25,12,0,0,DateTimeKind.Utc);
    private object Call(string name, params object[] args) => type.GetMethod(name).Invoke(state,args);
    private T Field<T>(string name) => (T)type.GetField(name).GetValue(state);
    private object Lifetime => Field<object>("Lifetime");
    private int Stat(string name) => (int)Lifetime.GetType().GetField(name).GetValue(Lifetime);
    [SetUp]
    public void Setup()
    {
        type = Type.GetType("BalloonDogMissionProgress, Assembly-CSharp",true);
        state = Activator.CreateInstance(type);
        Call("EnsureDay",day,1,false,10);
    }
    private void Run(double seconds, int coins, string skin = "classic")
    {
        Call("BeginRun",skin); Call("ProgressRun",seconds,coins); Call("EndRun");
    }
    [Test]
    public void RelaunchKeepsTheSameDailySelectionAndProgress()
    {
        Run(60d,500);
        int[] ids = (int[])Field<int[]>("DailyIds").Clone();
        string json = JsonUtility.ToJson(state);
        state = JsonUtility.FromJson(json,type);
        Assert.That((int)Call("EnsureDay",day,16,true,10),Is.Zero);
        Assert.That(Field<int[]>("DailyIds"),Is.EqualTo(ids));
        Assert.That(Stat("Coins"),Is.EqualTo(500));
        Assert.That(Stat("Runs"),Is.EqualTo(1));
    }
    [Test]
    public void MidnightUtcIsThreeAmInTurkeyAndRefreshesOnlyDailyProgress()
    {
        Run(70d,700);
        Call("EnsureDay",day.Date.AddDays(1),1,false,10);
        Assert.That(Field<string>("Day"),Is.EqualTo("2026-10-26"));
        Assert.That(Stat("Coins"),Is.EqualTo(700));
        object daily = Field<object>("Daily");
        Assert.That((int)daily.GetType().GetField("Coins").GetValue(daily),Is.Zero);
        Assert.That((DateTime)type.GetMethod("NextReset").Invoke(null,new object[]{day}),Is.EqualTo(day.Date.AddDays(1)));
    }
    [Test]
    public void ChangingTheClockBackDoesNotReopenAnOldDailySet()
    {
        Call("EnsureDay",day.AddDays(2),1,false,10);
        int[] ids = (int[])Field<int[]>("DailyIds").Clone();
        Call("EnsureDay",day,1,false,10);
        Assert.That(Field<string>("Day"),Is.EqualTo("2026-10-27"));
        Assert.That(Field<int[]>("DailyIds"),Is.EqualTo(ids));
    }
    [Test]
    public void RolloverWaitsUntilTheCurrentRunEnds()
    {
        Call("BeginRun","classic");
        Call("EnsureDay",day.AddDays(1),1,false,10);
        Assert.That(Field<string>("Day"),Is.EqualTo("2026-10-25"));
        Call("EndRun"); Call("EnsureDay",day.AddDays(1),1,false,10);
        Assert.That(Field<string>("Day"),Is.EqualTo("2026-10-26"));
    }
    [Test]
    public void SelectionAvoidsYesterdayAndUnavailableSkinOrCaseTasks()
    {
        for (int d=0;d<90;d++)
        {
            int[] previous = (int[])Field<int[]>("DailyIds").Clone();
            Call("EnsureDay",day.AddDays(d+1),1,false,10);
            int[] ids = Field<int[]>("DailyIds");
            Assert.That(Array.IndexOf(ids, 7), Is.EqualTo(-1));
            Assert.That(Array.IndexOf(ids, 8), Is.EqualTo(-1));
            for(int i=0;i<3;i++) Assert.That(ids[i],Is.Not.EqualTo(previous[i]));
        }
    }
    [Test]
    public void EarningsAreIncrementalAndRecoilCannotAwardCoinsTwice()
    {
        Call("BeginRun","classic");
        Call("ProgressRun",10d,100); Call("ProgressRun",10d,100);
        Call("ProgressRun",10d,80); Call("ProgressRun",10d,150);
        Assert.That(Stat("Coins"),Is.EqualTo(150));
        Assert.That(Stat("BestCoins"),Is.EqualTo(150));
        Call("EndRun"); Run(40d,200);
        Assert.That(Stat("Coins"),Is.EqualTo(350));
        Assert.That(Stat("BestCoins"),Is.EqualTo(200));
    }
    [Test]
    public void ReviveDoesNotCountAnotherRunAndLongRunsCountOnce()
    {
        Call("BeginRun","classic"); Call("BeginRun","classic");
        Call("ProgressRun",120d,100); Call("ProgressRun",60d,200);
        Assert.That(Stat("Runs"),Is.EqualTo(1)); Assert.That(Stat("LongRuns"),Is.EqualTo(1));
    }
    [Test]
    public void BalloonCountsTrackTotalAndBestRunIndependently()
    {
        Call("CollectBalloon"); Assert.That(Stat("Balloons"),Is.Zero);
        Call("BeginRun","classic"); Call("CollectBalloon"); Call("CollectBalloon"); Call("EndRun");
        Call("BeginRun","classic"); Call("CollectBalloon"); Call("EndRun");
        Assert.That(Stat("Balloons"),Is.EqualTo(3)); Assert.That(Stat("BestBalloons"),Is.EqualTo(2));
    }
    [Test]
    public void ClaimCannotBeRepeatedAndTheNextTierRetainsProgress()
    {
        Run(60d,5000);
        object[] args = {0,1,10,0,0};
        Assert.That((bool)type.GetMethod("ClaimNormal").Invoke(state,args),Is.True);
        Assert.That((int)args[3],Is.EqualTo(500));
        Assert.That((int)Call("NormalTarget",0,10),Is.EqualTo(15000));
        Assert.That((double)Call("NormalValue",0,1),Is.EqualTo(5000d));
        Assert.That((bool)type.GetMethod("ClaimNormal").Invoke(state,new object[]{0,1,10,0,0}),Is.False);
    }
    [Test]
    public void StageFiveAwardsGemsAndNoCoins()
    {
        Run(60d,150000);
        for (int stage=0;stage<5;stage++)
        {
            object[] args = {0,1,10,0,0};
            Assert.That((bool)type.GetMethod("ClaimNormal").Invoke(state,args),Is.True);
            Assert.That((int)args[3],Is.EqualTo(stage==4 ? 0 : (stage+1)*500));
            Assert.That((int)args[4],Is.EqualTo(stage==4 ? 5 : 0));
        }
    }
    [Test]
    public void ExpiredCompletedRewardsAndBonusAreGrantedOnlyOnce()
    {
        type.GetField("DailyIds").SetValue(state,new[]{0,3,2});
        Run(90d,1000); Run(90d,1000); Run(90d,1000);
        Assert.That((int)Call("EnsureDay",day.AddDays(1),1,false,10),Is.EqualTo(2500));
        Assert.That(Stat("DailyCompleted"),Is.EqualTo(3)); Assert.That(Stat("FullDays"),Is.EqualTo(1));
        Assert.That((int)Call("EnsureDay",day.AddDays(1),1,false,10),Is.Zero);
    }
    [Test]
    public void UnfinishedExpiredTasksDoNotGrantCoins()
    {
        type.GetField("DailyIds").SetValue(state,new[]{0,3,2});
        Run(10d,100);
        Assert.That((int)Call("EnsureDay",day.AddDays(1),1,false,10),Is.Zero);
    }
    [Test]
    public void FiniteCollectionAndCaseSeriesDoNotDemandUnavailableSkins()
    {
        Field<int[]>("Stages")[7] = 5; Field<int[]>("Stages")[8] = 5;
        Assert.That((bool)Call("Finished",7),Is.True); Assert.That((bool)Call("Finished",8),Is.True);
        Assert.That((bool)Call("CanClaimNormal",7,16,10),Is.False);
        Assert.That((bool)Call("CanClaimNormal",8,16,10),Is.False);
        Assert.That((int)Call("NormalTarget",7,10),Is.EqualTo(16));
        Assert.That((int)Call("NormalTarget",8,10),Is.EqualTo(15));
    }
}
