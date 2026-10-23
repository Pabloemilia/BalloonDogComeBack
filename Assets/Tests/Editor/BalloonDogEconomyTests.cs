using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public sealed class BalloonDogEconomyTests
{
    private const string Init = "BalloonDog.Economy.Initialized";
    private const string Coins = "BalloonDog.Coins";
    private const string Gems = "BalloonDog.Gems";
    private const string Best = "BalloonDog.BestScore";
    private const string Reset = "BalloonDog.BestScore.EndlessEconomy.v1";
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private readonly Dictionary<string, int> savedInts = new Dictionary<string, int>();
    private readonly HashSet<string> presentKeys = new HashSet<string>();
    private string equipped;
    private float previousTimeScale;
    private GameObject player;
    private GameObject host;
    private GameObject panel;
    private GameObject wallet;
    private Component score;
    private Component manager;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Economy(string method, params object[] args) => Runtime("BalloonDogEconomy").GetMethod(method).Invoke(null, args);
    private static int Balance(string property) => (int)Runtime("BalloonDogEconomy").GetProperty(property).GetValue(null);
    private static object Call(Component target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Static | Private).Invoke(target, args);
    private static void Set(Component target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    private void Snapshot(float metres)
    {
        player.transform.position = Vector3.forward * metres;
        Call(manager, "SnapshotRun");
    }

    [SetUp]
    public void SetUp()
    {
        foreach (string key in new[] { Init, Coins, Gems, Best, Reset, "BalloonDog.Skin.Owned.classic" })
        {
            if (PlayerPrefs.HasKey(key)) presentKeys.Add(key);
            savedInts[key] = PlayerPrefs.GetInt(key, 0);
        }
        if (PlayerPrefs.HasKey("BalloonDog.Skin.Equipped")) presentKeys.Add("BalloonDog.Skin.Equipped");
        equipped = PlayerPrefs.GetString("BalloonDog.Skin.Equipped", "classic");
        previousTimeScale = Time.timeScale;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PlayerPrefs.SetInt(Init, 1);
        PlayerPrefs.SetInt(Coins, 0);
        PlayerPrefs.SetInt(Gems, 0);
        PlayerPrefs.SetInt(Best, 0);
        PlayerPrefs.SetInt(Reset, 1);
        player = new GameObject("EconomyRunPlayer");
        score = player.AddComponent(Runtime("ScoreController"));
        Call(score, "Awake");
        host = new GameObject("EconomyRunManager");
        manager = host.AddComponent(Runtime("GameManager"));
        Call(manager, "Awake");
        panel = new GameObject("EconomyResultPanel");
        panel.SetActive(false);
        Call(manager, "ConfigureEndPanel", panel, null, null);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(panel);
        if (wallet != null) UnityEngine.Object.DestroyImmediate(wallet);
        UnityEngine.Object.DestroyImmediate(host);
        UnityEngine.Object.DestroyImmediate(player);
        foreach (var pair in savedInts)
            if (presentKeys.Contains(pair.Key)) PlayerPrefs.SetInt(pair.Key, pair.Value);
            else PlayerPrefs.DeleteKey(pair.Key);
        if (presentKeys.Contains("BalloonDog.Skin.Equipped")) PlayerPrefs.SetString("BalloonDog.Skin.Equipped", equipped);
        else PlayerPrefs.DeleteKey("BalloonDog.Skin.Equipped");
        PlayerPrefs.Save();
        savedInts.Clear();
        presentKeys.Clear();
        Time.timeScale = previousTimeScale;
    }

    [TestCase(0d, 0, 0)]
    [TestCase(9.99d, 0, 0)]
    [TestCase(10d, 1, 0)]
    [TestCase(100d, 10, 0)]
    [TestCase(499.99d, 49, 0)]
    [TestCase(500d, 50, 1)]
    [TestCase(1000d, 100, 2)]
    [TestCase(10000d, 1000, 20)]
    public void DistanceRewardsUseWholeMetreThresholdsWithoutAFarmingMinimum(double metres, int coins, int gems)
    {
        Assert.That((int)Economy("CalculateRunCoins", metres), Is.EqualTo(coins));
        Assert.That((int)Economy("CalculateRunGems", metres), Is.EqualTo(gems));
    }

    [Test]
    public void InvalidDistancesDoNotCreateRewardsAndLongRunsSaturateSafely()
    {
        foreach (double distance in new[] { -1d, double.NaN, double.PositiveInfinity })
            Assert.That((int)Economy("CalculateRunCoins", distance), Is.Zero);
        Assert.That((int)Economy("CalculateRunCoins", double.MaxValue), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void NewWalletStartsAtZeroAndExistingCoinsArePreservedWhenGemsArrive()
    {
        PlayerPrefs.DeleteKey(Init);
        PlayerPrefs.DeleteKey(Coins);
        PlayerPrefs.DeleteKey(Gems);
        Assert.That(Balance("Coins"), Is.Zero);
        Assert.That(Balance("Gems"), Is.Zero);
        PlayerPrefs.SetInt(Coins, 7752);
        PlayerPrefs.DeleteKey(Init);
        PlayerPrefs.DeleteKey(Gems);
        Assert.That(Balance("Coins"), Is.EqualTo(7752));
        Assert.That(Balance("Gems"), Is.Zero);
        Economy("AddGems", 5);
        Assert.That(PlayerPrefs.GetInt(Gems), Is.EqualTo(5));
    }

    [Test]
    public void BothBalancesPersistSpendAndRejectOverspendingNegativeAmountsAndOverflow()
    {
        Economy("AddRewards", 100, 5);
        Assert.That((bool)Economy("TrySpendCoins", 30), Is.True);
        Assert.That((bool)Economy("TrySpendGems", 2), Is.True);
        Assert.That((bool)Economy("TrySpendGems", 4), Is.False);
        Assert.That((bool)Economy("TrySpendCoins", -5), Is.False);
        Economy("AddRewards", -100, -5);
        Assert.That(PlayerPrefs.GetInt(Coins), Is.EqualTo(70));
        Assert.That(PlayerPrefs.GetInt(Gems), Is.EqualTo(3));
        Economy("AddRewards", int.MaxValue, int.MaxValue);
        Assert.That(Balance("Coins"), Is.EqualTo(int.MaxValue));
        Assert.That(Balance("Gems"), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void BestIsResetOnceThenNewRecordsSurviveFurtherReads()
    {
        PlayerPrefs.SetInt(Best, 99999);
        PlayerPrefs.DeleteKey(Reset);
        Assert.That((int)Runtime("GameManager").GetProperty("BestScore").GetValue(null), Is.Zero);
        Call(manager, "SaveBestScore", 1000);
        Assert.That((int)Runtime("GameManager").GetProperty("BestScore").GetValue(null), Is.EqualTo(1000));
        Call(manager, "SaveBestScore", 500);
        Assert.That((int)Runtime("GameManager").GetProperty("BestScore").GetValue(null), Is.EqualTo(1000));
    }

    [Test]
    public void FinalizedRunPaysOnceAndOneSuccessfulDoubleAddsBothCurrenciesOnce()
    {
        Snapshot(1000f);
        Set(manager, "isGameOver", true);
        Set(manager, "<IsContinuePending>k__BackingField", true);
        Assert.That(Balance("Coins"), Is.Zero, "The continue offer must not pay early.");
        Call(manager, "ExpireContinueOffer");
        Assert.That(Balance("Coins"), Is.EqualTo(100));
        Assert.That(Balance("Gems"), Is.EqualTo(2));
        Call(manager, "ExpireContinueOffer");
        Call(manager, "SettleRunRewards");
        Assert.That(Balance("Coins"), Is.EqualTo(100));
        Assert.That((bool)Call(manager, "TryClaimDoubleRunReward"), Is.True);
        Assert.That((bool)Call(manager, "TryClaimDoubleRunReward"), Is.False);
        Assert.That(Balance("Coins"), Is.EqualTo(200));
        Assert.That(Balance("Gems"), Is.EqualTo(4));
        Assert.That(PlayerPrefs.GetInt(Best), Is.EqualTo(10000));
    }

    [Test]
    public void ContinueKeepsOneRunAndRecoilDoesNotFarmCoins()
    {
        Component air = player.AddComponent(Runtime("AirController"));
        Call(air, "Awake");
        Component runner = player.AddComponent(Runtime("PlayerRunner"));
        Call(runner, "Awake");
        Snapshot(500f);
        Set(manager, "isGameOver", true);
        Set(manager, "<IsContinuePending>k__BackingField", true);
        Call(manager, "CompleteRewardedContinue");
        Assert.That(Balance("Coins"), Is.Zero);
        Assert.That(Balance("Gems"), Is.Zero);
        Snapshot(498f);
        Snapshot(500f);
        Snapshot(1000f);
        Set(manager, "isGameOver", true);
        Set(manager, "<IsContinuePending>k__BackingField", true);
        Call(manager, "ExpireContinueOffer");
        Assert.That(Balance("Coins"), Is.EqualTo(100));
        Assert.That(Balance("Gems"), Is.EqualTo(2));
    }

    [Test]
    public void LeavingAnActiveRunSavesEarnedDistanceButOpeningMenusDoesNotGrantFreeCoins()
    {
        Call(manager, "SettleBeforeLeaving");
        Assert.That(Balance("Coins"), Is.Zero);
        Call(manager, "BeginRun");
        Snapshot(1000f);
        Call(manager, "SettleBeforeLeaving");
        Call(manager, "SettleBeforeLeaving");
        Assert.That(Balance("Coins"), Is.EqualTo(100));
        Assert.That(Balance("Gems"), Is.EqualTo(2));
    }

    [Test]
    public void MainAndMarketCountersShareTheSameLargeIconAndFontAndGemsSitOnTheRight()
    {
        wallet = new GameObject("WalletHeaderTest", typeof(RectTransform));
        var cards = new List<RectTransform>();
        var labels = new List<RectTransform>();
        Type textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
        for (int i = 0; i < 3; i++)
        {
            RectTransform card = new GameObject("Counter" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            card.SetParent(wallet.transform, false);
            Component label = new GameObject("Label", typeof(RectTransform), textType).GetComponent(textType);
            label.transform.SetParent(card, false);
            // Reproduce the old market autosize cap; styling must replace it.
            textType.GetProperty("enableAutoSizing").SetValue(label, true);
            textType.GetProperty("fontSizeMax").SetValue(label, 30f);
            Runtime("BalloonDogBalloonThemeRuntime").GetMethod("StyleWalletCounter")
                .Invoke(null, new object[] { card, label, i == 2 });
            cards.Add(card);
            labels.Add((RectTransform)label.transform);
            Assert.That(card.sizeDelta, Is.EqualTo(new Vector2(290f, 96f)));
            Assert.That((float)textType.GetProperty("fontSizeMax").GetValue(label), Is.EqualTo(46f));
            Assert.That(((RectTransform)card.Find("ThemePillIcon")).sizeDelta, Is.EqualTo(new Vector2(62f, 62f)));
        }
        Assert.That(cards[0].anchoredPosition, Is.EqualTo(cards[1].anchoredPosition));
        Assert.That(labels[0].offsetMin, Is.EqualTo(labels[1].offsetMin));
        Assert.That(cards[2].anchoredPosition.x, Is.GreaterThan(cards[0].anchoredPosition.x + cards[0].sizeDelta.x));
        Assert.That(cards[2].Find("ThemePillIcon").GetComponent<Image>().sprite,
            Is.SameAs(Resources.Load<Sprite>("MarketUI/Extras/Icon_Gems")));
    }
}
