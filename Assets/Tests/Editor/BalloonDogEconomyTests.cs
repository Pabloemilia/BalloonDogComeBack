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
    private const string LegacySelection = "BalloonDog.SelectedSkin.V5";
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private readonly Dictionary<string, int> savedInts = new Dictionary<string, int>();
    private readonly HashSet<string> presentKeys = new HashSet<string>();
    private string equipped;
    private string legacySelection;
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
        var keys = new HashSet<string> { Init, Coins, Gems, Best, Reset, CaseReset, Cases };
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
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PlayerPrefs.SetInt(Init, 1);
        PlayerPrefs.SetInt(Coins, 0);
        PlayerPrefs.SetInt(Gems, 0);
        PlayerPrefs.SetInt(Best, 0);
        PlayerPrefs.SetInt(Reset, 1);
        PlayerPrefs.SetInt(CaseReset, 1);
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
        PlayerPrefs.Save();
        savedInts.Clear();
        presentKeys.Clear();
        Time.timeScale = previousTimeScale;
    }

    [TestCase(0d, 0)]
    [TestCase(8.33d, 0)]
    [TestCase(8.34d, 1)]
    [TestCase(9.99d, 1)]
    [TestCase(10d, 1)]
    [TestCase(25d, 3)]
    [TestCase(49.99d, 5)]
    [TestCase(50d, 6)]
    [TestCase(100d, 12)]
    [TestCase(499.99d, 59)]
    [TestCase(500d, 60)]
    [TestCase(1000d, 120)]
    [TestCase(10000d, 1200)]
    public void DistanceRewardsApplyTheTwentyPercentBoostBeforeRounding(double metres, int coins)
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
        Assert.That(Balance("Coins"), Is.EqualTo(120));
        Assert.That(Balance("Gems"), Is.EqualTo(5));
        Call(manager, "ExpireContinueOffer");
        Call(manager, "SettleRunRewards");
        Assert.That(Balance("Coins"), Is.EqualTo(120));
        Assert.That((bool)Call(manager, "TryClaimDoubleRunReward"), Is.True);
        Assert.That((bool)Call(manager, "TryClaimDoubleRunReward"), Is.False);
        Assert.That(Balance("Coins"), Is.EqualTo(240));
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
        Assert.That(Balance("Coins"), Is.EqualTo(120));
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
        Assert.That(Balance("Coins"), Is.EqualTo(120));
        Assert.That(Balance("Gems"), Is.Zero);
    }

    [TestCase(-1, 50)]
    [TestCase(0, 50)]
    [TestCase(1, 100)]
    [TestCase(2, 150)]
    [TestCase(10, 550)]
    [TestCase(11, 600)]
    [TestCase(12, 600)]
    [TestCase(int.MaxValue, 600)]
    public void CasePriceIncreasesByFiftyAndNeverExceedsSixHundred(int opened, int expected)
    {
        Assert.That((int)Economy("CalculateCasePrice", opened), Is.EqualTo(expected));
    }

    [Test]
    public void CasesUnlockAllFifteenModelSkinsWithoutDuplicatesAndSaveTheNextPrice()
    {
        Economy("AddRewards", 10000, 5);
        var seen = new HashSet<string>();
        int spent = 0;
        Assert.That(Skins.Length, Is.EqualTo(16), "One starter plus the fifteen scene model skins.");
        for (int count = 0; count < Skins.Length - 1; count++)
        {
            int price = Math.Min(600, (count + 1) * 50);
            Assert.That(Balance("CasePrice"), Is.EqualTo(price));
            object[] args = { price, null };
            Assert.That((bool)Economy("TryUnlockNextSkin", args), Is.True);
            string id = SkinId(args[1]);
            Assert.That(seen.Add(id), Is.True, "Each case must unlock a new model.");
            Assert.That((bool)Economy("IsOwned", id), Is.True);
            Assert.That(PlayerPrefs.GetString("BalloonDog.Skin.Equipped"), Is.EqualTo(id));
            spent += price;
            Assert.That(Balance("Coins"), Is.EqualTo(10000 - spent));
            Assert.That(Balance("CasesOpened"), Is.EqualTo(count + 1));
        }
        Assert.That(spent, Is.EqualTo(5700));
        Assert.That(Balance("Gems"), Is.EqualTo(5));
        Assert.That(Balance("CasePrice"), Is.EqualTo(600));
        object[] completed = { 600, null };
        Assert.That((bool)Economy("TryUnlockNextSkin", completed), Is.False);
        Assert.That(Balance("Coins"), Is.EqualTo(4300));
        Assert.That(Balance("CasesOpened"), Is.EqualTo(15));
    }

    [Test]
    public void InsufficientBalanceOrStalePriceNeverSpendsOrAdvancesTheCase()
    {
        Economy("AddCoins", 49);
        object[] insufficient = { 50, null };
        Assert.That((bool)Economy("TryUnlockNextSkin", insufficient), Is.False);
        Economy("AddCoins", 951);
        foreach (int quote in new[] { -1, 0, 49, 100 })
        {
            object[] invalid = { quote, null };
            Assert.That((bool)Economy("TryUnlockNextSkin", invalid), Is.False);
        }
        Assert.That(Balance("Coins"), Is.EqualTo(1000));
        Assert.That(Balance("CasesOpened"), Is.Zero);
        object[] first = { 50, null };
        Assert.That((bool)Economy("TryUnlockNextSkin", first), Is.True);
        object[] stale = { 50, null };
        Assert.That((bool)Economy("TryUnlockNextSkin", stale), Is.False);
        Assert.That(Balance("Coins"), Is.EqualTo(950));
        Assert.That(Balance("CasePrice"), Is.EqualTo(100));
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
        Assert.That(Balance("CasePrice"), Is.EqualTo(50));
        foreach (object skin in Skins)
            Assert.That((bool)Economy("IsOwned", SkinId(skin)), Is.EqualTo(SkinId(skin) == "classic"));
        Assert.That(PlayerPrefs.HasKey("BalloonDog.Skin.Owned.mint"), Is.False);
        Assert.That(PlayerPrefs.HasKey(LegacySelection), Is.False);
        Assert.That(PlayerPrefs.GetString("BalloonDog.Skin.Equipped"), Is.EqualTo("classic"));
        Assert.That(Balance("Coins"), Is.EqualTo(1000));
        Assert.That(Balance("Gems"), Is.EqualTo(7));
        Assert.That(PlayerPrefs.GetInt(Best), Is.EqualTo(1234));
        object[] opening = { 50, null };
        Assert.That((bool)Economy("TryUnlockNextSkin", opening), Is.True);
        Assert.That(Balance("CasePrice"), Is.EqualTo(100));
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
        Assert.That((string)textType.GetProperty("text").GetValue(text), Is.EqualTo("50"));
        Economy("AddCoins", 1000);
        object[] first = { 50, null };
        Assert.That((bool)Economy("TryUnlockNextSkin", first), Is.True);
        Assert.That((string)textType.GetProperty("text").GetValue(text), Is.EqualTo("100"));
    }

    [TestCase("skin_caramel", "COMMON")]
    [TestCase("skin_clown", "UNCOMMON")]
    [TestCase("skin_dino", "RARE")]
    [TestCase("skin_honey", "EPIC")]
    [TestCase("skin_traxex", "LEGENDARY")]
    public void DispensedCapsuleMatchesTheAwardedSkinsRarityInsteadOfItsBodyColor(string skinId, string rarity)
    {
        wallet = new GameObject("DispensedCapsuleColorTest", typeof(RectTransform));
        Component machine = wallet.AddComponent(Runtime("BalloonDogCaseOpening"));
        Call(machine, "Build");
        Image capsule = wallet.transform.Find("CaseMachine/MachineVisual/DispensedCapsule").GetComponent<Image>();
        Material reusedMaterial = capsule.material;
        // First use a different tier, verifying successive openings update the existing material.
        Call(machine, "PrepareDispensedCapsule", Economy("FindSkin", "skin_cyber"));
        object skin = Economy("FindSkin", skinId);
        Call(machine, "PrepareDispensedCapsule", skin);
        Color expected = wallet.transform.Find("CaseMachine/" + rarity + "Swatch")
            .GetComponent<Image>().material.GetColor("_CapsuleColor");
        Assert.That(capsule.material, Is.SameAs(reusedMaterial));
        Assert.That(capsule.material.GetColor("_CapsuleColor"), Is.EqualTo(expected));
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
        Assert.That(Balance("CasePrice"), Is.EqualTo(50));
        wallet.SetActive(true);
        Assert.That(Quaternion.Angle(crank.localRotation, Quaternion.identity), Is.LessThan(.01f));
        Assert.That(wallet.transform.Find("CaseMachine/MachineVisual/DispensedCapsule").gameObject.activeSelf, Is.False);
        Assert.That(wallet.transform.Find("CaseMachine/OpenCase").GetComponent<Button>().interactable, Is.True);
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
