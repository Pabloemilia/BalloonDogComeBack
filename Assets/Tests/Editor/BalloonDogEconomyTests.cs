using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
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
    private const string CaseReset = "BalloonDog.Skins.CaseEconomy.v1";
    private const string Cases = "BalloonDog.Cases.Opened.v1";
    private const string CoinScaleMigration = "BalloonDog.Coins.Scale10.v1";
    private const string LegacySelection = "BalloonDog.SelectedSkin.V5";
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private readonly Dictionary<string, int> savedInts = new Dictionary<string, int>();
    private readonly HashSet<string> presentKeys = new HashSet<string>();
    private string equipped;
    private string legacySelection;
    private string missionSave;
    private bool hadMissionSave;
    private float previousTimeScale;
    private bool previousAudioPause;
    private GameObject player;
    private GameObject host;
    private GameObject panel;
    private GameObject wallet;
    private Component score;
    private Component manager;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Economy(string method, params object[] args) => Runtime("BalloonDogEconomy").GetMethod(method).Invoke(null, args);
    private static int Balance(string property) => (int)Runtime("BalloonDogEconomy").GetProperty(property).GetValue(null);
    private static Array Skins => (Array)Runtime("BalloonDogEconomy").GetProperty("Skins").GetValue(null);
    private static string SkinId(object skin) => (string)skin.GetType().GetProperty("Id").GetValue(skin);
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
        hadMissionSave = PlayerPrefs.HasKey("BalloonDog.Missions.v1");
        missionSave = PlayerPrefs.GetString("BalloonDog.Missions.v1", "");
        PlayerPrefs.DeleteKey("BalloonDog.Missions.v1");
        Runtime("BalloonDogMissions").GetMethod("ResetRuntime", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,null);
        var keys = new HashSet<string> { Init, Coins, Gems, Best, Reset, CaseReset, Cases, CoinScaleMigration };
        foreach (object skin in Skins) keys.Add("BalloonDog.Skin.Owned." + SkinId(skin));
        foreach (string id in new[] { "mint", "sunset", "royal", "midnight", "candy" })
            keys.Add("BalloonDog.Skin.Owned." + id);
        foreach (string key in keys)
        {
            if (PlayerPrefs.HasKey(key)) presentKeys.Add(key);
            savedInts[key] = PlayerPrefs.GetInt(key, 0);
        }
        if (PlayerPrefs.HasKey("BalloonDog.Skin.Equipped")) presentKeys.Add("BalloonDog.Skin.Equipped");
        equipped = PlayerPrefs.GetString("BalloonDog.Skin.Equipped", "classic");
        if (PlayerPrefs.HasKey(LegacySelection)) presentKeys.Add(LegacySelection);
        legacySelection = PlayerPrefs.GetString(LegacySelection, string.Empty);
        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PlayerPrefs.SetInt(Init, 1);
        PlayerPrefs.SetInt(Coins, 0);
        PlayerPrefs.SetInt(Gems, 0);
        PlayerPrefs.SetInt(Best, 0);
        PlayerPrefs.SetInt(Reset, 1);
        PlayerPrefs.SetInt(CaseReset, 1);
        PlayerPrefs.SetInt(CoinScaleMigration, 1);
        PlayerPrefs.SetInt(Cases, 0);
        foreach (object skin in Skins)
            PlayerPrefs.SetInt("BalloonDog.Skin.Owned." + SkinId(skin), SkinId(skin) == "classic" ? 1 : 0);
        PlayerPrefs.SetString("BalloonDog.Skin.Equipped", "classic");
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
        if (presentKeys.Contains(LegacySelection)) PlayerPrefs.SetString(LegacySelection, legacySelection);
        else PlayerPrefs.DeleteKey(LegacySelection);
        if (hadMissionSave) PlayerPrefs.SetString("BalloonDog.Missions.v1", missionSave);
        else PlayerPrefs.DeleteKey("BalloonDog.Missions.v1");
        Runtime("BalloonDogMissions").GetMethod("ResetRuntime", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,null);
        PlayerPrefs.Save();
        savedInts.Clear();
        presentKeys.Clear();
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
    }

    [TestCase(0d, 0)]
    [TestCase(8.33d, 0)]
    [TestCase(8.34d, 10)]
    [TestCase(9.99d, 10)]
    [TestCase(10d, 10)]
    [TestCase(25d, 30)]
    [TestCase(49.99d, 50)]
    [TestCase(50d, 60)]
    [TestCase(100d, 120)]
    [TestCase(499.99d, 590)]
    [TestCase(500d, 600)]
    [TestCase(1000d, 1200)]
    [TestCase(10000d, 12000)]
    public void DistanceRewardsKeepTheTwentyPercentBoostAndScaleWholeCoinsTenfold(double metres, int coins)
    {
        Assert.That((int)Economy("CalculateRunCoins", metres), Is.EqualTo(coins));
    }

    [Test]
    public void InvalidDistancesDoNotCreateRewardsAndLongRunsSaturateSafely()
    {
        foreach (double distance in new[] { -1d, double.NaN, double.PositiveInfinity })
            Assert.That((int)Economy("CalculateRunCoins", distance), Is.Zero);
        Assert.That((int)Economy("CalculateRunCoins", double.MaxValue), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void CoinScaleMigrationRunsOnceAndPreservesGemsSkinsAndCaseProgress()
    {
        PlayerPrefs.SetInt(Coins, 1999);
        PlayerPrefs.SetInt(Gems, 12);
        PlayerPrefs.SetInt(Cases, 7);
        string skin = SkinId(Skins.GetValue(3));
        PlayerPrefs.SetInt("BalloonDog.Skin.Owned." + skin, 1);
        PlayerPrefs.SetString("BalloonDog.Skin.Equipped", skin);
        PlayerPrefs.DeleteKey(CoinScaleMigration);
        Assert.That(Balance("Coins"), Is.EqualTo(19990));
        Assert.That(Balance("CasePrice"), Is.EqualTo(4000));
        Assert.That(Balance("CasesOpened"), Is.EqualTo(7));
        Assert.That(Balance("Gems"), Is.EqualTo(12));
        Assert.That((bool)Economy("IsOwned", skin), Is.True);
        Assert.That(PlayerPrefs.GetString("BalloonDog.Skin.Equipped"), Is.EqualTo(skin));
        Assert.That(PlayerPrefs.GetInt(CoinScaleMigration), Is.EqualTo(1));
        // Subsequent reads, new rewards and initialization cannot reapply the conversion.
        Economy("AddCoins", 10);
        PlayerPrefs.DeleteKey(Init);
        Assert.That(Balance("Coins"), Is.EqualTo(20000));
        Assert.That(Balance("Coins"), Is.EqualTo(20000));
    }

    [TestCase(0, 0)]
    [TestCase(-1, 0)]
    [TestCase(500, 5000)]
    [TestCase(1500, 15000)]
    [TestCase(3500, 35000)]
    [TestCase(7500, 75000)]
    [TestCase(16000, 160000)]
    [TestCase(int.MaxValue, int.MaxValue)]
    public void CoinScaleConversionAndLegacyWalletsClampSafely(int before, int expected)
    {
        Assert.That((int)Economy("ScaleCoins", before), Is.EqualTo(expected));
        PlayerPrefs.SetInt(Coins, before);
        PlayerPrefs.DeleteKey(CoinScaleMigration);
        Assert.That(Balance("Coins"), Is.EqualTo(expected));
        Assert.That(Balance("Coins"), Is.EqualTo(expected));
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
    public void FinalizedRunPaysCoinsOnceAndSuccessfulDoubleNeverChangesGems()
    {
        Economy("AddGems", 5);
        Snapshot(1000f);
        Set(manager, "isGameOver", true);
        Set(manager, "<IsContinuePending>k__BackingField", true);
        Assert.That(Balance("Coins"), Is.Zero, "The continue offer must not pay early.");
        Call(manager, "ExpireContinueOffer");
        Assert.That(Balance("Coins"), Is.EqualTo(1200));
        Assert.That(Balance("Gems"), Is.EqualTo(5));
        Call(manager, "ExpireContinueOffer");
        Call(manager, "SettleRunRewards");
        Assert.That(Balance("Coins"), Is.EqualTo(1200));
        Assert.That((bool)Call(manager, "TryClaimDoubleRunReward"), Is.True);
        Assert.That((bool)Call(manager, "TryClaimDoubleRunReward"), Is.False);
        Assert.That(Balance("Coins"), Is.EqualTo(2400));
        Assert.That(Balance("Gems"), Is.EqualTo(5));
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
        Assert.That(Balance("Coins"), Is.EqualTo(1200));
        Assert.That(Balance("Gems"), Is.Zero);
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
        Assert.That(Balance("Coins"), Is.EqualTo(1200));
        Assert.That(Balance("Gems"), Is.Zero);
    }

    [TestCase(-1, 500)]
    [TestCase(0, 500)]
    [TestCase(1, 1000)]
    [TestCase(2, 1500)]
    [TestCase(10, 5500)]
    [TestCase(11, 6000)]
    [TestCase(12, 6000)]
    [TestCase(int.MaxValue, 6000)]
    public void CasePriceIncreasesByFiveHundredAndNeverExceedsSixThousand(int opened, int expected)
    {
        Assert.That((int)Economy("CalculateCasePrice", opened), Is.EqualTo(expected));
    }

    [Test]
    public void CasesUnlockAllFifteenModelSkinsWithoutDuplicatesAndSaveTheNextPrice()
    {
        Economy("AddRewards", 100000, 5);
        var seen = new HashSet<string>();
        int spent = 0;
        Assert.That(Skins.Length, Is.EqualTo(16), "One starter plus the fifteen scene model skins.");
        for (int count = 0; count < Skins.Length - 1; count++)
        {
            int price = Math.Min(6000, (count + 1) * 500);
            Assert.That(Balance("CasePrice"), Is.EqualTo(price));
            object[] args = { price, null };
            Assert.That((bool)Economy("TryUnlockNextSkin", args), Is.True);
            string id = SkinId(args[1]);
            Assert.That(seen.Add(id), Is.True, "Each case must unlock a new model.");
            Assert.That((bool)Economy("IsOwned", id), Is.True);
            Assert.That(PlayerPrefs.GetString("BalloonDog.Skin.Equipped"), Is.EqualTo(id));
            spent += price;
            Assert.That(Balance("Coins"), Is.EqualTo(100000 - spent));
            Assert.That(Balance("CasesOpened"), Is.EqualTo(count + 1));
        }
        Assert.That(spent, Is.EqualTo(57000));
        Assert.That(Balance("Gems"), Is.EqualTo(5));
        Assert.That(Balance("CasePrice"), Is.EqualTo(6000));
        object[] completed = { 6000, null };
        Assert.That((bool)Economy("TryUnlockNextSkin", completed), Is.False);
        Assert.That(Balance("Coins"), Is.EqualTo(43000));
        Assert.That(Balance("CasesOpened"), Is.EqualTo(15));
    }

    [Test]
    public void InsufficientBalanceOrStalePriceNeverSpendsOrAdvancesTheCase()
    {
        Economy("AddCoins", 499);
        object[] insufficient = { 500, null };
        Assert.That((bool)Economy("TryUnlockNextSkin", insufficient), Is.False);
        Economy("AddCoins", 9501);
        foreach (int quote in new[] { -1, 0, 499, 1000 })
        {
            object[] invalid = { quote, null };
            Assert.That((bool)Economy("TryUnlockNextSkin", invalid), Is.False);
        }
        Assert.That(Balance("Coins"), Is.EqualTo(10000));
        Assert.That(Balance("CasesOpened"), Is.Zero);
        object[] first = { 500, null };
        Assert.That((bool)Economy("TryUnlockNextSkin", first), Is.True);
        object[] stale = { 500, null };
        Assert.That((bool)Economy("TryUnlockNextSkin", stale), Is.False);
        Assert.That(Balance("Coins"), Is.EqualTo(9500));
        Assert.That(Balance("CasePrice"), Is.EqualTo(1000));
        Assert.That((bool)Economy("Equip", SkinId(Skins.GetValue(2))), Is.False, "Locked model selection stays blocked.");
    }

    [Test]
    public void CollectionResetRunsOncePreservesWalletAndBestAndKeepsFutureUnlocks()
    {
        foreach (object skin in Skins) PlayerPrefs.SetInt("BalloonDog.Skin.Owned." + SkinId(skin), 1);
        PlayerPrefs.SetInt("BalloonDog.Skin.Owned.mint", 1);
        PlayerPrefs.SetInt(Coins, 1000);
        PlayerPrefs.SetInt(Gems, 7);
        PlayerPrefs.SetInt(Best, 1234);
        PlayerPrefs.SetInt(Cases, 9);
        PlayerPrefs.SetString("BalloonDog.Skin.Equipped", SkinId(Skins.GetValue(15)));
        PlayerPrefs.SetString(LegacySelection, SkinId(Skins.GetValue(15)));
        PlayerPrefs.DeleteKey(CaseReset);
        Assert.That(Balance("CasePrice"), Is.EqualTo(500));
        foreach (object skin in Skins)
            Assert.That((bool)Economy("IsOwned", SkinId(skin)), Is.EqualTo(SkinId(skin) == "classic"));
        Assert.That(PlayerPrefs.HasKey("BalloonDog.Skin.Owned.mint"), Is.False);
        Assert.That(PlayerPrefs.HasKey(LegacySelection), Is.False);
        Assert.That(PlayerPrefs.GetString("BalloonDog.Skin.Equipped"), Is.EqualTo("classic"));
        Assert.That(Balance("Coins"), Is.EqualTo(1000));
        Assert.That(Balance("Gems"), Is.EqualTo(7));
        Assert.That(PlayerPrefs.GetInt(Best), Is.EqualTo(1234));
        object[] opening = { 500, null };
        Assert.That((bool)Economy("TryUnlockNextSkin", opening), Is.True);
        Assert.That(Balance("CasePrice"), Is.EqualTo(1000));
        Assert.That((bool)Economy("IsOwned", SkinId(opening[1])), Is.True);
        Assert.That(PlayerPrefs.GetString("BalloonDog.Skin.Equipped"), Is.EqualTo(SkinId(opening[1])));
    }

    [Test]
    public void ModelSkinCatalogMatchesExistingPrefabsAndLockedSelectionCannotBypassCases()
    {
        Component switcher = player.AddComponent(Runtime("BalloonDogSkinSwitcher"));
        GameObject starter = new GameObject("default");
        starter.transform.SetParent(player.transform, false);
        Set(switcher, "starterVisual", starter);
        var prefabs = new List<GameObject>();
        foreach (object skin in Skins)
        {
            string id = SkinId(skin);
            if (id == "classic") continue;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Skins/" + id + ".prefab");
            Assert.That(prefab, Is.Not.Null, id);
            prefabs.Add(prefab);
        }
        Set(switcher, "skinPrefabs", prefabs);
        for (int i = 0; i < Skins.Length; i++)
            Assert.That((string)Call(switcher, "EconomySkinId", i), Is.EqualTo(SkinId(Skins.GetValue(i))));
        Call(switcher, "SelectSkin", 1);
        Assert.That(PlayerPrefs.GetString("BalloonDog.Skin.Equipped"), Is.EqualTo("classic"));
        Assert.That((int)switcher.GetType().GetProperty("CurrentSkinIndex").GetValue(switcher), Is.EqualTo(-1));
        Assert.That(Balance("CasesOpened"), Is.Zero);
    }

    [Test]
    public void CaseButtonShowsCoinLogoAndLivePriceOnTheMovingOrangeFace()
    {
        wallet = new GameObject("CasePriceUITest", typeof(RectTransform));
        Component machine = wallet.AddComponent(Runtime("BalloonDogCaseOpening"));
        Call(machine, "Build");
        Call(machine, "OnEnable");
        Transform price = wallet.transform.Find("CaseMachine/OpenCase/OrangeFace/CasePrice");
        Assert.That(price, Is.Not.Null);
        Assert.That(price.Find("CoinIcon").GetComponent<Image>().sprite,
            Is.SameAs(Resources.Load<Sprite>("MarketUI/Extras/Icon_Coins")));
        Type textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
        Component text = price.Find("Amount").GetComponent(textType);
        Assert.That((string)textType.GetProperty("text").GetValue(text), Is.EqualTo("500"));
        Economy("AddCoins", 1000);
        object[] first = { 500, null };
        Assert.That((bool)Economy("TryUnlockNextSkin", first), Is.True);
        Assert.That((string)textType.GetProperty("text").GetValue(text), Is.EqualTo(1000.ToString("N0")));
    }

    [TestCase("skin_caramel", "COMMON")]
    [TestCase("skin_clown", "UNCOMMON")]
    [TestCase("skin_dino", "RARE")]
    [TestCase("skin_honey", "EPIC")]
    [TestCase("skin_traxex", "LEGENDARY")]
    public void CapsuleAndRewardBackgroundMatchTheAwardedSkinsRarityInsteadOfItsBodyColor(string skinId, string rarity)
    {
        wallet = new GameObject("DispensedCapsuleColorTest", typeof(RectTransform));
        Component machine = wallet.AddComponent(Runtime("BalloonDogCaseOpening"));
        Call(machine, "Build");
        Image capsule = wallet.transform.Find("CaseMachine/MachineVisual/DispensedCapsule").GetComponent<Image>();
        Material reusedMaterial = capsule.material;
        // First use a different tier, verifying successive openings update the existing material.
        Call(machine, "PrepareDispensedCapsule", Economy("FindSkin", "skin_cyber"));
        Call(machine, "ApplyRewardTheme", Economy("FindSkin", "skin_cyber"));
        object skin = Economy("FindSkin", skinId);
        Call(machine, "PrepareDispensedCapsule", skin);
        Call(machine, "ApplyRewardTheme", skin);
        Color expected = wallet.transform.Find("CaseMachine/" + rarity + "Swatch")
            .GetComponent<Image>().material.GetColor("_CapsuleColor");
        Assert.That(capsule.material, Is.SameAs(reusedMaterial));
        Assert.That(capsule.material.GetColor("_CapsuleColor"), Is.EqualTo(expected));
        Transform rewardPage = (Transform)machine.GetType().GetField("rewardPanel", Private).GetValue(machine);
        Image background = rewardPage.Find("Backdrop").GetComponent<Image>();
        Assert.That(background.color, Is.EqualTo(Color.white));
        Assert.That(background.material.shader.name, Is.EqualTo("BalloonDog/UI/CapsuleTint"));
        Assert.That(background.material.GetColor("_CapsuleColor"), Is.EqualTo(expected * .72f));
        Assert.That(skin.GetType().GetProperty("Rarity").GetValue(skin).ToString().ToUpperInvariant(), Is.EqualTo(rarity));
        Assert.That(Balance("CasesOpened"), Is.Zero);
        Assert.That(Balance("Coins"), Is.Zero);
    }

    [Test]
    public void CapsuleMachineSeparatesMovingPartsAndUsesTheEquippedModelForTheReward()
    {
        wallet = new GameObject("CapsuleMachineUITest", typeof(RectTransform));
        Component machine = wallet.AddComponent(Runtime("BalloonDogCaseOpening"));
        Call(machine, "Build");
        Transform visual = wallet.transform.Find("CaseMachine/MachineVisual");
        Assert.That(visual, Is.Not.Null);
        Assert.That(visual.Find("BodyAndGlass").GetComponent<Image>().sprite, Is.Not.Null);
        Assert.That(((RectTransform)visual.Find("Crank")).pivot, Is.EqualTo(new Vector2(.255f, .395f)));
        Assert.That(visual.Find("DispensedCapsule").gameObject.activeSelf, Is.False);
        int uncommon = 0, legendary = 0;
        var positions = new HashSet<Vector2>();
        for (int i = 0; i < 18; i++)
        {
            Image capsule = visual.Find("Capsule" + i).GetComponent<Image>();
            Assert.That(capsule.sprite, Is.Not.Null);
            Assert.That(capsule.material.shader.name, Is.EqualTo("BalloonDog/UI/CapsuleTint"));
            Assert.That(capsule.raycastTarget, Is.False);
            Color tint = capsule.material.GetColor("_CapsuleColor");
            if (tint.g > .9f && tint.r < .3f) uncommon++;
            if (tint.r > .9f && tint.g > .5f) legendary++;
            positions.Add(capsule.rectTransform.anchoredPosition);
            Assert.That(capsule.transform.GetSiblingIndex(), Is.LessThan(visual.Find("BodyAndGlass").GetSiblingIndex()));
        }
        Assert.That(positions.Count, Is.EqualTo(18));
        Assert.That(uncommon, Is.GreaterThan(legendary));
        Assert.That(legendary, Is.EqualTo(1));
        Transform reward = (Transform)machine.GetType().GetField("rewardPanel", Private).GetValue(machine);
        Assert.That(reward.parent, Is.Null);
        Assert.That(reward.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        Assert.That(reward.Find("Celebration").GetComponent(Runtime("BalloonDogSkinRewardEffects")), Is.Not.Null);
        Assert.That(reward.gameObject.activeSelf, Is.False);
        Component preview = reward.Find("RewardContent/EquippedSkinPreview").GetComponent(Runtime("BalloonDogMenuShowcase"));
        Assert.That((bool)preview.GetType().GetProperty("CompactPreview").GetValue(preview), Is.True);
        Assert.That(wallet.transform.Find("CaseMachine/ReelViewport"), Is.Null);
    }

    [TestCase(0f, 0f)]
    [TestCase(.32f, -55f)]
    [TestCase(.64f, 0f)]
    [TestCase(1f, 0f)]
    public void CapsuleCrankMakesOneShortStrokeThenStaysStill(float progress, float angle)
    {
        float actual = (float)Runtime("BalloonDogCaseOpening").GetMethod("CrankAngle").Invoke(null, new object[] { progress });
        Assert.That(actual, Is.EqualTo(angle).Within(.001f));
    }

    [Test]
    public void SeparateRewardPageLocksMarketAndContinueRestoresItWithoutSpending()
    {
        wallet = new GameObject("ModernMarketScreen", typeof(RectTransform));
        Component machine = wallet.AddComponent(Runtime("BalloonDogCaseOpening"));
        Call(machine, "Build");
        Transform reward = (Transform)machine.GetType().GetField("rewardPanel", Private).GetValue(machine);
        // Exercise dismissal without starting a live model-rendering stage in this editor test.
        ((Behaviour)reward.Find("RewardContent/EquippedSkinPreview").GetComponent(Runtime("BalloonDogMenuShowcase"))).enabled = false;
        Economy("AddCoins", 1000);
        Call(machine, "SetMarketInputLocked", true);
        reward.gameObject.SetActive(true);
        Assert.That(wallet.GetComponent<CanvasGroup>().interactable, Is.False);
        Assert.That(reward.GetComponentInParent<CanvasGroup>(), Is.Null);
        Call(machine, "DismissReward");
        Assert.That(reward.gameObject.activeSelf, Is.False);
        Assert.That(wallet.GetComponent<CanvasGroup>().interactable, Is.True);
        Assert.That(Balance("Coins"), Is.EqualTo(1000));
        Assert.That(Balance("CasesOpened"), Is.Zero);
    }

    [Test]
    public void CapsuleMachineRejectsAnUnaffordableOpeningAndResetsWhenClosed()
    {
        wallet = new GameObject("CapsuleCancellationTest", typeof(RectTransform));
        Component machine = wallet.AddComponent(Runtime("BalloonDogCaseOpening"));
        Call(machine, "Build");
        Call(machine, "OpenCase");
        Assert.That((bool)machine.GetType().GetField("spinning", Private).GetValue(machine), Is.False);
        Assert.That(Balance("CasesOpened"), Is.Zero);
        Economy("AddCoins", 1000);
        // Simulate leaving while the visual animation is in progress, before the purchase commits.
        Set(machine, "spinning", true);
        Transform crank = wallet.transform.Find("CaseMachine/MachineVisual/Crank");
        crank.localRotation = Quaternion.Euler(0f, 0f, 150f);
        wallet.transform.Find("CaseMachine/MachineVisual/DispensedCapsule").gameObject.SetActive(true);
        wallet.SetActive(false);
        Assert.That(Balance("Coins"), Is.EqualTo(1000));
        Assert.That(Balance("CasesOpened"), Is.Zero);
        Assert.That(Balance("CasePrice"), Is.EqualTo(500));
        wallet.SetActive(true);
        Assert.That(Quaternion.Angle(crank.localRotation, Quaternion.identity), Is.LessThan(.01f));
        Assert.That(wallet.transform.Find("CaseMachine/MachineVisual/DispensedCapsule").gameObject.activeSelf, Is.False);
        Assert.That(wallet.transform.Find("CaseMachine/OpenCase").GetComponent<Button>().interactable, Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ApprovedCurrencyShopShowsFiveCorrectPacksAndSixIndependentButtons(bool gems)
    {
        wallet = new GameObject("ApprovedShopTest", typeof(RectTransform));
        wallet.GetComponent<RectTransform>().sizeDelta = new Vector2(1080f, 1920f);
        Component ui = wallet.AddComponent(Runtime("BalloonDogModernUI"));
        Set(ui, "safeRoot", wallet.GetComponent<RectTransform>());
        GameObject page = (GameObject)Call(ui, "BuildCurrencyPacksScreen", gems);
        Transform layout = page.transform.Find("CurrencyLayout");
        Image art = layout.Find("ApprovedShopArtwork").GetComponent<Image>();
        Assert.That(art.sprite, Is.Not.Null);
        Assert.That(art.raycastTarget, Is.False);
        Assert.That(page.GetComponentsInChildren<ScrollRect>(true), Is.Empty);
        Assert.That(page.GetComponentsInChildren<Button>(true).Length, Is.EqualTo(6));
        Type textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
        int[] expected = gems ? new[] { 50, 150, 350, 750, 1600 } : new[] { 5000, 15000, 35000, 75000, 160000 };
        string[] prices = { "$0.49", "$0.99", "$1.99", "$3.99", "$7.99" };
        string currency = gems ? "GEMS" : "COINS";
        for (int i = 0; i < 5; i++)
        {
            Component quantity = layout.Find(currency + "PackAmount" + (i + 1)).GetComponent(textType);
            Assert.That((string)textType.GetProperty("text").GetValue(quantity),
                Is.EqualTo(expected[i].ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"))));
            Transform button = layout.Find(currency + "Pack" + (i + 1));
            Component price = button.Find("PressContent/PackPrice").GetComponent(textType);
            Assert.That((string)textType.GetProperty("text").GetValue(price), Is.EqualTo(prices[i]));
            Graphic surface = button.Find("PressContent").GetComponent<Graphic>();
            Assert.That(surface.GetComponent<Image>(), Is.Null,
                "Feedback must not draw another copy of the painted price button.");
            Assert.That(surface.GetType(), Is.EqualTo(Runtime("BalloonDogShopPriceFace")));
            Component press = button.GetComponent(Runtime("BalloonDogDirectPressButton"));
            Assert.That(press, Is.Not.Null);
            object font = textType.GetProperty("font").GetValue(price);
            MethodInfo hasCharacter = font.GetType().GetMethod("HasCharacter",
                new[] { typeof(char), typeof(bool), typeof(bool) });
            Assert.That((bool)hasCharacter.Invoke(font, new object[] { '$', true, true }), Is.True,
                "Every USD price must resolve the dollar glyph in the bundled fonts.");
            RectTransform labelRect = (RectTransform)price.transform;
            Vector2 labelPosition = labelRect.anchoredPosition;
            Vector2 facePosition = surface.rectTransform.anchoredPosition;
            Assert.That(surface.color.a, Is.Zero);
            Set(press, "depth", 1f);
            Call(press, "Apply");
            Assert.That(surface.color.a, Is.EqualTo(.18f).Within(.001f));
            Assert.That(((Graphic)price).color, Is.EqualTo(Color.white), "Prices must stay white during a press.");
            Assert.That(surface.rectTransform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(surface.rectTransform.anchoredPosition, Is.EqualTo(facePosition));
            Assert.That(labelRect.anchoredPosition, Is.EqualTo(labelPosition + Vector2.down * 2f));
            using (var vertices = new UnityEngine.UI.VertexHelper())
            {
                surface.GetType().GetMethod("OnPopulateMesh", Private).Invoke(surface, new object[] { vertices });
                var mesh = new Mesh();
                try
                {
                    vertices.FillMesh(mesh);
                    Rect face = (Rect)surface.GetType().GetProperty("FaceBounds").GetValue(surface);
                    Rect full = surface.rectTransform.rect;
                    Assert.That(face.width, Is.LessThan(full.width));
                    Assert.That(face.height, Is.LessThan(full.height));
                    Assert.That(mesh.vertexCount, Is.EqualTo(133));
                    Color32[] colors = mesh.colors32;
                    float radius = face.height * .5f;
                    Vector3[] points = mesh.vertices;
                    for (int v = 0; v < points.Length; v++)
                    {
                        Vector3 vertex = points[v];
                        float x = Mathf.Max(0f, Mathf.Abs(vertex.x - face.center.x) - (face.width * .5f - radius));
                        float y = Mathf.Abs(vertex.y - face.center.y);
                        Assert.That(x * x + y * y, Is.LessThanOrEqualTo(radius * radius + .1f),
                            "The tinted mesh must stay inside the orange capsule, excluding atlas corners.");
                        if (v > 0 && v % 2 == 0)
                            Assert.That(colors[v].a, Is.Zero, "The outer edge must feather to transparent.");
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(mesh); }
            }
            Set(press, "depth", 0f);
            Call(press, "Apply");
            Assert.That(surface.color.a, Is.Zero);
            Assert.That(labelRect.anchoredPosition, Is.EqualTo(labelPosition));
            Set(press, "depth", 1f);
            Call(press, "Apply");
            Call(press, "OnDisable");
            Assert.That(surface.color.a, Is.Zero, "Closing the page must clear all press feedback.");
            Assert.That(labelRect.anchoredPosition, Is.EqualTo(labelPosition));
        }
        Image back = layout.Find("BackToExtras").GetComponent<Image>();
        Sprite originalBlue = (Sprite)Runtime("BalloonDogMenuArtwork").GetMethod("Get")
            .Invoke(null, new object[] { "butonmavi" });
        Assert.That(back.sprite, Is.SameAs(originalBlue));
        Assert.That(back.GetComponent(Runtime("BalloonDogDirectPressButton")), Is.Null);
        RectTransform left = (RectTransform)layout.Find(currency + "Pack1");
        RectTransform right = (RectTransform)layout.Find(currency + "Pack2");
        Assert.That(left.anchoredPosition.x, Is.LessThan(0f));
        Assert.That(right.anchoredPosition.x, Is.GreaterThan(0f));
        Assert.That(left.anchoredPosition.y, Is.EqualTo(right.anchoredPosition.y));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void CollectionPagesShowRealOwnedModelsAndLockedSilhouettesWithoutChangingThePlayer(int pageIndex)
    {
        Component switcher = player.AddComponent(Runtime("BalloonDogSkinSwitcher"));
        GameObject starter = GameObject.CreatePrimitive(PrimitiveType.Cube);
        starter.name = "StarterPreviewSource";
        starter.transform.SetParent(player.transform, false);
        starter.transform.localScale = new Vector3(1f, 2f, 3f);
        Set(switcher, "starterVisual", starter);
        var prefabs = new List<GameObject>();
        foreach (object skin in Skins)
        {
            string id = SkinId(skin);
            if (id == "classic") continue;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Skins/" + id + ".prefab");
            Assert.That(prefab, Is.Not.Null, id);
            prefabs.Add(prefab);
        }
        Set(switcher, "skinPrefabs", prefabs);
        int first = pageIndex * 6;
        int last = Mathf.Min(first + 6, Skins.Length);
        string revealedId = SkinId(Skins.GetValue(first == 0 ? 1 : first));
        PlayerPrefs.SetInt("BalloonDog.Skin.Owned." + revealedId, 1);
        string equippedBefore = (string)Runtime("BalloonDogEconomy").GetProperty("EquippedSkinId").GetValue(null);
        Vector3 sourceScale = starter.transform.localScale;
        Material sourceMaterial = starter.GetComponent<Renderer>().sharedMaterial;

        wallet = new GameObject("CollectionPreviewTest", typeof(RectTransform));
        wallet.GetComponent<RectTransform>().sizeDelta = new Vector2(1080f, 1920f);
        Component ui = wallet.AddComponent(Runtime("BalloonDogModernUI"));
        Set(ui, "safeRoot", wallet.GetComponent<RectTransform>());
        Call(ui, "BuildSkinsScreen");
        Transform content = wallet.transform.Find("ModernSkinsScreen/CollectionContent");
        Component studio = content.GetComponent(Runtime("BalloonDogCollectionStudio"));
        Set(studio, "switcher", switcher);
        Call(ui, "SetCollectionPage", pageIndex);
        Assert.That((bool)Call(studio, "BuildPage"), Is.True);
        GameObject models = (GameObject)studio.GetType().GetField("models", Private).GetValue(studio);
        Assert.That(models.transform.childCount, Is.EqualTo(last - first));
        Assert.That(models.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(models.GetComponentsInChildren<Animator>(true), Is.Empty);
        Assert.That(models.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
        RenderTexture atlas = (RenderTexture)studio.GetType().GetField("target", Private).GetValue(studio);
        GameObject stage = (GameObject)studio.GetType().GetField("stage", Private).GetValue(studio);
        Assert.That(stage.GetComponentsInChildren<Camera>().Length, Is.EqualTo(1));

        for (int i = 0; i < Skins.Length; i++)
        {
            string id = SkinId(Skins.GetValue(i));
            Transform card = content.Find("Collection_" + id);
            Assert.That(card.gameObject.activeSelf, Is.EqualTo(i >= first && i < last));
            Assert.That(card.Find("SkinSwatch"), Is.Null);
            RawImage preview = card.Find("SkinPreview").GetComponent<RawImage>();
            Assert.That(preview.texture, Is.SameAs(atlas));
            Assert.That(preview.raycastTarget, Is.False);
            if (i < first || i >= last) continue;
            Assert.That(preview.color, Is.EqualTo(Color.white));
            Transform model = models.transform.Find(id);
            Assert.That(model.GetComponentsInChildren<MeshFilter>().Length, Is.GreaterThan(0));
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
                foreach (Material material in renderer.sharedMaterials)
                    Assert.That(material.GetFloat("_Silhouette"),
                        Is.EqualTo(id == "classic" || id == revealedId ? 0f : 1f), id);
        }
        Image previous = content.Find("PreviousPage").GetComponent<Image>();
        Image next = content.Find("NextPage").GetComponent<Image>();
        Assert.That(previous.sprite.texture, Is.SameAs(Resources.Load<Sprite>("CollectionUI/PageArrow").texture));
        Assert.That(next.sprite, Is.SameAs(previous.sprite));
        Assert.That(previous.rectTransform.localScale.x, Is.LessThan(0f));
        Assert.That(next.rectTransform.localScale.x, Is.GreaterThan(0f));
        Assert.That(previous.GetComponent<Button>().interactable, Is.EqualTo(pageIndex > 0));
        Assert.That(next.GetComponent<Button>().interactable, Is.EqualTo(pageIndex < 2));
        Assert.That(previous.rectTransform.anchoredPosition.y, Is.EqualTo(next.rectTransform.anchoredPosition.y));
        Assert.That(previous.rectTransform.anchoredPosition.x, Is.EqualTo(-next.rectTransform.anchoredPosition.x));

        // Earning a skin while the collection is open reveals its real materials.
        string newlyOwned = SkinId(Skins.GetValue(first + 2));
        PlayerPrefs.SetInt("BalloonDog.Skin.Owned." + newlyOwned, 1);
        Call(studio, "MarkDirty");
        Call(studio, "LateUpdate");
        models = (GameObject)studio.GetType().GetField("models", Private).GetValue(studio);
        foreach (Renderer renderer in models.transform.Find(newlyOwned).GetComponentsInChildren<Renderer>())
            foreach (Material material in renderer.sharedMaterials)
                Assert.That(material.GetFloat("_Silhouette"), Is.Zero);
        Camera camera = stage.GetComponentInChildren<Camera>();
        Call(studio, "AfterBuiltInCamera", camera);
        Assert.That(camera.enabled, Is.False, "Static thumbnails must stop rendering after the atlas is ready.");
        Assert.That(starter.transform.localScale, Is.EqualTo(sourceScale));
        Assert.That(starter.GetComponent<Renderer>().sharedMaterial, Is.SameAs(sourceMaterial));
        Assert.That(starter.GetComponent<Collider>().enabled, Is.True);
        Assert.That((string)Runtime("BalloonDogEconomy").GetProperty("EquippedSkinId").GetValue(null), Is.EqualTo(equippedBefore));
        Assert.That(Balance("Coins"), Is.Zero);
        content.gameObject.SetActive(false);
        Assert.That(atlas == null, Is.True);
        Assert.That(stage == null, Is.True);
        content.gameObject.SetActive(true);
        Call(studio, "LateUpdate");
        Assert.That(studio.GetType().GetField("target", Private).GetValue(studio), Is.Not.Null);
    }

    [TestCase(1080f,1920f)]
    [TestCase(1080f,2348f)]
    [TestCase(750f,1334f)]
    [TestCase(1125f,2436f)]
    public void MissionsPopupFitsAllDailyRowsAndSwitchesTabsWithoutGapsOrClipping(float width, float height)
    {
        wallet = new GameObject("MissionsUiTest", typeof(RectTransform));
        wallet.GetComponent<RectTransform>().sizeDelta = new Vector2(width,height);
        Component ui = wallet.AddComponent(Runtime("BalloonDogModernUI"));
        Set(ui,"safeRoot",wallet.GetComponent<RectTransform>());
        GameObject main = new GameObject("Main",typeof(RectTransform));
        main.transform.SetParent(wallet.transform,false);
        Set(ui,"mainScreen",main);
        Call(ui,"BuildMissionsPopup");
        Call(ui,"ShowMissionsPopup");
        Canvas.ForceUpdateCanvases();
        Transform popup = wallet.transform.Find("MissionsPopup");
        Assert.That(main.activeSelf,Is.True);
        Assert.That(popup.gameObject.activeSelf,Is.True);
        Assert.That(main.GetComponent<CanvasGroup>().interactable,Is.False);
        Transform window = popup.Find("MissionsWindow");
        Assert.That(window.GetComponent<Button>(),Is.Not.Null,"Empty panel taps must not bubble to outside-close.");
        ScrollRect scroll = window.Find("MissionViewport").GetComponent<ScrollRect>();
        RectTransform list = (RectTransform)window.Find("MissionViewport/MissionList");
        RectTransform viewport = (RectTransform)window.Find("MissionViewport");
        Assert.That(list.childCount,Is.EqualTo(3));
        Assert.That(list.anchoredPosition,Is.EqualTo(Vector2.zero));
        Assert.That(scroll.vertical,Is.False,"The daily page must fit without scrolling.");
        Rect timerBounds = MissionBounds(window,window.Find("ResetTimer"));
        Rect firstTask = MissionBounds(window,list.Find("Mission_0/Task"));
        Assert.That(timerBounds.yMin-firstTask.yMax,Is.InRange(0f,65f),"No half-row gap below the timer.");
        Rect viewBounds = MissionBounds(window,viewport);
        foreach (Transform row in list)
        {
            Rect task = MissionBounds(window,row.Find("Task"));
            Rect bar = MissionBounds(window,row.Find("ProgressTrack"));
            Assert.That(task.yMax,Is.LessThanOrEqualTo(viewBounds.yMax));
            Assert.That(bar.yMin,Is.GreaterThanOrEqualTo(viewBounds.yMin));
            Assert.That(bar.yMax,Is.LessThan(task.yMin),"Progress must not overlap a two-line title.");
        }
        Rect bonus = MissionBounds(window,window.Find("DailyBonus"));
        Assert.That(bonus.yMax,Is.LessThan(viewBounds.yMin),"The bonus cannot cover the final mission.");
        Rect panelBounds = MissionBounds(wallet.transform,window);
        Assert.That(panelBounds.xMin,Is.GreaterThan(-width*.5f));
        Assert.That(panelBounds.xMax,Is.LessThan(width*.5f));
        Assert.That(panelBounds.yMin,Is.GreaterThan(-height*.5f));
        Assert.That(panelBounds.yMax,Is.LessThan(height*.5f));
        Image unselected = window.Find("NormalTab").GetComponent<Image>();
        Assert.That(unselected.sprite.border.x,Is.GreaterThan(0f));
        Assert.That(window.Find("NormalTab/IdleFace").gameObject.activeSelf,Is.True);
        window.Find("NormalTab").GetComponent<Button>().onClick.Invoke();
        Assert.That(list.childCount,Is.EqualTo(12));
        Assert.That(scroll.vertical,Is.True);
        Assert.That(list.rect.height,Is.GreaterThan(2500f));
        Assert.That(list.anchoredPosition,Is.EqualTo(Vector2.zero));
        Assert.That(window.Find("PermanentHint").gameObject.activeSelf,Is.True);
        list.anchoredPosition = new Vector2(0f,500f);
        Call(ui,"RebuildMissionRows");
        Assert.That(list.anchoredPosition.y,Is.EqualTo(500f),"Claiming rewards must not jump the permanent list.");
        window.Find("DailyTab").GetComponent<Button>().onClick.Invoke();
        Assert.That(list.anchoredPosition,Is.EqualTo(Vector2.zero));
        Assert.That(scroll.vertical,Is.False);
        window.Find("MissionsClose").GetComponent<Button>().onClick.Invoke();
        Assert.That(main.activeSelf,Is.True);
        Assert.That(popup.gameObject.activeSelf,Is.False);
        Assert.That(main.GetComponent<CanvasGroup>().interactable,Is.True);
    }

    private static Rect MissionBounds(Transform root, Transform child)
    {
        Vector3[] corners = new Vector3[4];
        ((RectTransform)child).GetWorldCorners(corners);
        Vector3 first = root.InverseTransformPoint(corners[0]);
        Vector3 last = root.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(first.x,first.y,last.x,last.y);
    }

    [Test]
    public void OpeningACaseInitializesMissionsWithoutCountingTheCaseTwice()
    {
        Economy("AddCoins",500);
        object[] args = {500,null};
        Assert.That((bool)Economy("TryUnlockNextSkin",args),Is.True);
        object progress = Runtime("BalloonDogMissions").GetProperty("Progress").GetValue(null);
        object lifetime = progress.GetType().GetField("Lifetime").GetValue(progress);
        Assert.That((int)lifetime.GetType().GetField("Cases").GetValue(lifetime),Is.EqualTo(1));
        object daily = progress.GetType().GetField("Daily").GetValue(progress);
        Assert.That((int)daily.GetType().GetField("Cases").GetValue(daily),Is.EqualTo(1));
    }

    [Test]
    public void MissionRewardsDoNotProgressCoinTasksAndClaimsSurviveReload()
    {
        Type missions = Runtime("BalloonDogMissions");
        missions.GetMethod("BeginRun").Invoke(null,null);
        missions.GetMethod("TickRun").Invoke(null,new object[]{60f,5000});
        missions.GetMethod("EndRun").Invoke(null,new object[]{5000});
        Assert.That((bool)missions.GetMethod("ClaimNormal").Invoke(null,new object[]{0}),Is.True);
        Assert.That(Balance("Coins"),Is.EqualTo(500));
        missions.GetMethod("ResetRuntime",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        object progress = missions.GetProperty("Progress").GetValue(null);
        object lifetime = progress.GetType().GetField("Lifetime").GetValue(progress);
        Assert.That((int)lifetime.GetType().GetField("Coins").GetValue(lifetime),Is.EqualTo(5000));
        Assert.That((bool)missions.GetMethod("ClaimNormal").Invoke(null,new object[]{0}),Is.False);
        Assert.That(Balance("Coins"),Is.EqualTo(500));
    }

    private Component CurrencyUi(string originField, out GameObject origin, out GameObject coinsPage, out GameObject gemsPage, out GameObject gameplay)
    {
        wallet = new GameObject("CurrencyNavigationTest", typeof(RectTransform));
        Component ui = wallet.AddComponent(Runtime("BalloonDogModernUI"));
        origin = new GameObject("Origin", typeof(RectTransform));
        coinsPage = new GameObject("Coins", typeof(RectTransform));
        gemsPage = new GameObject("Gems", typeof(RectTransform));
        gameplay = new GameObject("Gameplay", typeof(RectTransform));
        foreach (GameObject page in new[] { origin, coinsPage, gemsPage, gameplay })
        {
            page.transform.SetParent(wallet.transform, false);
            page.SetActive(false);
        }
        Set(ui, originField, origin);
        Set(ui, "coinPacksScreen", coinsPage);
        Set(ui, "gemPacksScreen", gemsPage);
        Set(ui, "gameplayOverlay", gameplay);
        return ui;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CurrencyCounterClicksOpenTheCorrectStoreAndSurviveVisualRestyling(bool gems)
    {
        Component ui = CurrencyUi("mainScreen", out GameObject origin, out GameObject coinsPage,
            out GameObject gemsPage, out GameObject gameplay);
        origin.SetActive(true);
        GameObject card = new GameObject("WalletCounter", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(origin.transform, false);
        Type textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
        Component label = new GameObject("Label", typeof(RectTransform), textType).GetComponent(textType);
        label.transform.SetParent(card.transform, false);
        Call(ui, "BindCurrencyCounter", label, gems);
        Call(ui, "BindCurrencyCounter", label, gems);
        Assert.That(card.transform.childCount, Is.EqualTo(2), "Only one hit surface may be attached.");
        Runtime("BalloonDogBalloonThemeRuntime").GetMethod("StyleWalletCounter")
            .Invoke(null, new object[] { card.GetComponent<RectTransform>(), label, gems });
        Button hit = card.transform.Find("CurrencyHitTarget").GetComponent<Button>();
        Assert.That(hit.GetComponent<Image>().raycastTarget, Is.True);
        hit.onClick.Invoke();
        Assert.That(gems ? gemsPage.activeSelf : coinsPage.activeSelf, Is.True);
        Assert.That(gems ? coinsPage.activeSelf : gemsPage.activeSelf, Is.False);
        Call(ui, "ReturnFromCurrencyPacks");
        Assert.That(origin.activeSelf, Is.True);
    }

    [TestCase("marketScreen")]
    [TestCase("resultScreen")]
    [TestCase("pauseScreen")]
    public void StoreBackReturnsToTheOriginScreenAndPreservesPausedTime(string originField)
    {
        Component ui = CurrencyUi(originField, out GameObject origin, out GameObject coinsPage,
            out GameObject gemsPage, out GameObject gameplay);
        origin.SetActive(true);
        Time.timeScale = 0f;
        Call(ui, "ShowCurrencyPacks", false);
        Assert.That(origin.activeSelf, Is.False);
        Call(ui, "ShowCurrencyPacks", true);
        Assert.That(coinsPage.activeSelf, Is.False);
        Assert.That(gemsPage.activeSelf, Is.True);
        Call(ui, "ReturnFromCurrencyPacks");
        Assert.That(origin.activeSelf, Is.True);
        Assert.That(gameplay.activeSelf, Is.False);
        Assert.That(Time.timeScale, Is.Zero);
    }

    [Test]
    public void GameplayStorePausesTheRunAndReturnsWithoutSettlingOrGrantingCoins()
    {
        Component ui = CurrencyUi("mainScreen", out GameObject origin, out GameObject coinsPage,
            out GameObject gemsPage, out GameObject gameplay);
        gameplay.SetActive(true);
        Time.timeScale = 1f;
        Call(ui, "ShowCurrencyPacks", false);
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(gameplay.activeSelf, Is.False);
        Call(ui, "ShowCurrencyPacks", true);
        Call(ui, "ReturnFromCurrencyPacks");
        Assert.That(gameplay.activeSelf, Is.True);
        Assert.That(origin.activeSelf, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(Balance("Coins"), Is.Zero);
        Assert.That(Balance("CasesOpened"), Is.Zero);
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
