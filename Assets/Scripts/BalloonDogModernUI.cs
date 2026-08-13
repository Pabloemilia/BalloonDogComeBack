using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runtime-built, mobile-safe front end for the Balloon Dog prototype.
/// It replaces the legacy prototype panels while continuing to use the
/// existing gameplay, countdown, scoring and restart systems.
/// </summary>
public sealed class BalloonDogModernUI : MonoBehaviour
{
    private const string RuntimeObjectName = "__BalloonDogModernUI";
    private const string MasterVolumeKey = "BalloonDog.MasterVolume";
    private const string SoundPreferenceKey = "BalloonDog.SoundEnabled";
    private const int WheelTokenCost = 300;

    private static readonly Color Ink = new Color(0.035f, 0.025f, 0.085f, 1f);
    private static readonly Color InkSoft = new Color(0.075f, 0.055f, 0.15f, 1f);
    private static readonly Color Purple = new Color(0.20f, 0.035f, 0.60f, 1f);
    private static readonly Color PurpleBright = new Color(0.36f, 0.08f, 0.88f, 1f);
    private static readonly Color Orange = new Color(1f, 0.47f, 0.035f, 1f);
    private static readonly Color Gold = new Color(1f, 0.68f, 0.12f, 1f);
    private static readonly Color Lime = new Color(0.35f, 0.95f, 0.025f, 1f);
    private static readonly Color Cyan = new Color(0.04f, 0.75f, 0.98f, 1f);
    private static readonly Color Muted = new Color(0.62f, 0.62f, 0.72f, 1f);
    private static readonly Color MenuSkyTop = new Color(0.20f, 0.49f, 0.64f, 0.98f);
    private static readonly Color MenuSkyBottom = new Color(0.075f, 0.20f, 0.29f, 0.98f);
    private static readonly Color MenuBlue = new Color(0.18f, 0.54f, 0.96f, 1f);
    private static readonly Color MenuBlueDark = new Color(0.055f, 0.27f, 0.43f, 0.98f);
    private static readonly Color MenuBlueCard = new Color(0.075f, 0.35f, 0.52f, 0.96f);
    private static readonly Color MenuGreen = new Color(0.35f, 0.84f, 0.23f, 1f);
    private static readonly Color MenuCream = new Color(0.91f, 0.86f, 0.63f, 0.98f);

    private enum SettingsReturnTarget
    {
        Main,
        Pause
    }

    private sealed class SkinCardView
    {
        public BalloonDogSkinDefinition Skin;
        public bool MarketMode;
        public Image Swatch;
        public TMP_Text Status;
        public TMP_Text ActionLabel;
        public Button ActionButton;
    }

    private sealed class ToggleRowView
    {
        public TMP_Text StateLabel;
        public Image StateBackground;
    }

    private MainMenuController menuController;
    private GameManager gameManager;
    private ScoreController scoreController;

    private Canvas canvas;
    private RectTransform safeRoot;
    private GameObject mainScreen;
    private GameObject marketScreen;
    private GameObject skinsScreen;
    private GameObject settingsScreen;
    private GameObject privacyScreen;
    private GameObject resultScreen;
    private GameObject pauseScreen;
    private GameObject gameplayOverlay;

    private TMP_Text coinText;
    private TMP_Text mainBestText;
    private TMP_Text equippedNameText;
    private TMP_Text resultTitleText;
    private TMP_Text resultReasonText;
    private TMP_Text resultScoreText;
    private TMP_Text resultBestText;
    private TMP_Text resultRewardText;
    private TMP_Text gameplaySkinText;
    private TMP_Text wheelNextText;
    private TMP_Text wheelButtonLabel;
    private TMP_Text toastText;
    private CanvasGroup toastGroup;
    private float toastTimer;

    private Slider volumeSlider;
    private ToggleRowView soundToggle;
    private ToggleRowView vibrationToggle;
    private SettingsReturnTarget settingsReturnTarget;
    private bool privacyReturnToSettings;

    private readonly List<SkinCardView> skinCards = new List<SkinCardView>();
    private readonly List<Image> menuPreviewParts = new List<Image>();
    private int lastKnownCoins = -1;
    private string lastKnownSkin = string.Empty;
    private bool resultRewardGranted;
    private int lastRunReward;
    private RectTransform wheelTransform;
    private Button wheelButton;
    private bool wheelSpinning;
    private Coroutine rebuildRoutine;
    private bool economySubscribed;

    private static Sprite roundedSprite;
    private static Sprite circleSprite;
    private static Sprite patternSprite;
    private static readonly Dictionary<string, Sprite> ResourceSprites =
        new Dictionary<string, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeInterface()
    {
        GameObject previous = GameObject.Find(RuntimeObjectName);
        if (previous != null)
        {
            return;
        }

        GameObject runtimeObject = new GameObject(RuntimeObjectName);
        DontDestroyOnLoad(runtimeObject);
        runtimeObject.AddComponent<BalloonDogModernUI>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private IEnumerator Start()
    {
        if (!economySubscribed)
        {
            BalloonDogEconomy.Changed += RefreshPersistentViews;
            economySubscribed = true;
        }

        yield return RebuildForCurrentScene();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (rebuildRoutine != null)
        {
            StopCoroutine(rebuildRoutine);
        }

        rebuildRoutine = StartCoroutine(RebuildForCurrentScene());
    }

    private IEnumerator RebuildForCurrentScene()
    {
        if (gameManager != null)
        {
            gameManager.GameEnded -= HandleGameEnded;
        }

        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }

        yield return null;

        skinCards.Clear();
        menuPreviewParts.Clear();
        ResetRuntimeReferences();
        ResolveReferences();
        bool startInGameplay = DetectGameplayState();
        DisableLegacyPanels();
        DisableWorldLabels();
        EnsureEventSystem();
        BuildInterface();

        if (gameManager != null)
        {
            gameManager.GameEnded -= HandleGameEnded;
            gameManager.GameEnded += HandleGameEnded;
        }

        ApplySavedAudioSettings();
        RefreshPersistentViews();

        // The legacy controller consumes the restart flag in Start(). Its public
        // state therefore tells this layer whether this load is a fresh menu or
        // an immediate replay.
        if (startInGameplay)
        {
            ShowGameplayOverlay();
        }
        else
        {
            ShowMainScreen();
        }

        rebuildRoutine = null;
    }

    private void ResetRuntimeReferences()
    {
        canvas = null;
        safeRoot = null;
        mainScreen = null;
        marketScreen = null;
        skinsScreen = null;
        settingsScreen = null;
        privacyScreen = null;
        resultScreen = null;
        pauseScreen = null;
        gameplayOverlay = null;
        coinText = null;
        mainBestText = null;
        equippedNameText = null;
        resultTitleText = null;
        resultReasonText = null;
        resultScoreText = null;
        resultBestText = null;
        resultRewardText = null;
        gameplaySkinText = null;
        wheelNextText = null;
        wheelButtonLabel = null;
        wheelTransform = null;
        wheelButton = null;
        volumeSlider = null;
        soundToggle = null;
        vibrationToggle = null;
        toastText = null;
        toastGroup = null;
        wheelSpinning = false;
        toastTimer = 0f;
        lastKnownCoins = -1;
        lastKnownSkin = string.Empty;
        resultRewardGranted = false;
        lastRunReward = 0;
    }

    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.GameEnded -= HandleGameEnded;
                gameManager.GameEnded += HandleGameEnded;
            }
        }

        if (scoreController == null)
        {
            scoreController = FindAnyObjectByType<ScoreController>();
        }

        if (gameManager != null && gameManager.IsGameOver &&
            resultScreen != null && !resultScreen.activeSelf)
        {
            HandleGameEnded();
        }

        int coins = BalloonDogEconomy.Coins;
        string skinId = BalloonDogEconomy.EquippedSkinId;
        if (coins != lastKnownCoins || skinId != lastKnownSkin)
        {
            RefreshPersistentViews();
        }

        UpdateToast();
    }

    private void ResolveReferences()
    {
        menuController = FindAnyObjectByType<MainMenuController>();
        gameManager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
        scoreController = FindAnyObjectByType<ScoreController>();
    }

    private void DisableLegacyPanels()
    {
        string[] panelNames =
        {
            "MainMenuPanel",
            "SettingsPanel",
            "GameOverPanel",
            "EndPanel",
            "PausePanel",
            "PauseButton"
        };

        foreach (string panelName in panelNames)
        {
            GameObject legacyObject = FindNamedSceneObject(panelName);
            if (legacyObject != null)
            {
                legacyObject.SetActive(false);
            }
        }
    }

    private static void DisableWorldLabels()
    {
        SetActive(FindNamedSceneObject("FLOW_WorldLabel"), false);
        SetActive(FindNamedSceneObject("FINISH_WorldLabel"), false);
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        eventSystem.transform.SetAsLastSibling();
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject(
            "BalloonDogModernCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2348f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        safeRoot = CreateRect("SafeArea", canvasObject.transform);
        Stretch(safeRoot);
        safeRoot.gameObject.AddComponent<BalloonDogSafeArea>();

        BuildMainScreen();
        BuildMarketScreen();
        BuildSkinsScreen();
        BuildSettingsScreen();
        BuildPrivacyScreen();
        BuildResultScreen();
        BuildPauseScreen();
        BuildGameplayOverlay();
        BuildToast();

        HideAllScreens();
    }

    private void BuildMainScreen()
    {
        mainScreen = CreateScreen(
            "ModernMainScreen",
            new Color(0.39f, 0.07f, 0.98f, 1f),
            new Color(0.32f, 0.05f, 0.91f, 1f));

        coinText = CreatePillText(
            mainScreen.transform,
            "MainTokens",
            "◉  0",
            new Vector2(-365f, 1030f),
            new Vector2(290f, 92f),
            new Color(0.25f, 0.03f, 0.67f, 0.95f),
            Gold);

        CreateButton(
            mainScreen.transform,
            "MainSettingsButton",
            "⚙",
            new Vector2(430f, 1030f),
            new Vector2(105f, 105f),
            new Color(0.25f, 0.03f, 0.67f, 0.95f),
            Color.white,
            () => ShowSettingsScreen(SettingsReturnTarget.Main),
            48f);

        TMP_Text title = CreateText(
            mainScreen.transform,
            "MainTitle",
            "BALLOON DOG",
            new Vector2(0f, 790f),
            new Vector2(920f, 160f),
            82f,
            Color.white,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        AddTextShadow(title, new Color(0.12f, 0.01f, 0.30f, 0.9f), new Vector2(5f, -6f));

        mainBestText = CreatePillText(
            mainScreen.transform,
            "MainBest",
            "BEST  0",
            new Vector2(0f, 610f),
            new Vector2(340f, 76f),
            new Color(0.22f, 0.02f, 0.60f, 0.72f),
            new Color(0.89f, 0.83f, 1f, 1f));

        CreateResourceImage(
            mainScreen.transform,
            "FigmaMainButtons",
            "ModernUI/MainButtons",
            new Vector2(0f, -760f),
            new Vector2(760f, 207f));
        CreateTransparentButton(
            mainScreen.transform,
            "ModernPlayButton",
            new Vector2(-100f, -760f),
            new Vector2(545f, 190f),
            StartGame);
        CreateTransparentButton(
            mainScreen.transform,
            "QuickMarketButton",
            new Vector2(290f, -760f),
            new Vector2(180f, 190f),
            ShowMarketScreen);

        CreateResourceImage(
            mainScreen.transform,
            "FigmaMiscButtons",
            "ModernUI/MiscButtons",
            new Vector2(0f, -1010f),
            new Vector2(440f, 133f));
        CreateTransparentButton(
            mainScreen.transform,
            "MarketNavButton",
            new Vector2(-80f, -1010f),
            new Vector2(170f, 125f),
            ShowMarketScreen);
        CreateTransparentButton(
            mainScreen.transform,
            "SkinsNavButton",
            new Vector2(85f, -1010f),
            new Vector2(170f, 125f),
            ShowSkinsScreen);

        Button privacyButton = CreateButton(
            mainScreen.transform,
            "PrivacyLinkButton",
            "PRIVACY",
            new Vector2(0f, -1125f),
            new Vector2(280f, 58f),
            new Color(1f, 1f, 1f, 0.04f),
            new Color(0.84f, 0.78f, 1f, 1f),
            ShowPrivacyFromMain,
            22f);
        RemoveButtonShadow(privacyButton);
    }

    private void CreateBalloonDogPreview(Transform parent)
    {
        RectTransform card = CreateCard(
            parent,
            "EquippedSkinPreview",
            new Vector2(0f, 45f),
            new Vector2(650f, 465f),
            new Color(0.055f, 0.035f, 0.14f, 0.92f),
            new Color(0.50f, 0.22f, 1f, 0.60f));

        CreateImage(
            card,
            "PreviewGlow",
            new Vector2(0f, 40f),
            new Vector2(330f, 330f),
            new Color(0.46f, 0.18f, 1f, 0.18f),
            true);

        Color bodyColor = BalloonDogEconomy.EquippedSkin.PrimaryColor;
        Image body = CreateImage(card, "DogBody", new Vector2(-20f, 45f), new Vector2(245f, 125f), bodyColor, false);
        Image head = CreateImage(card, "DogHead", new Vector2(135f, 85f), new Vector2(125f, 125f), bodyColor, true);
        Image frontLeg = CreateImage(card, "DogFrontLeg", new Vector2(60f, -45f), new Vector2(58f, 135f), bodyColor, false);
        Image backLeg = CreateImage(card, "DogBackLeg", new Vector2(-105f, -45f), new Vector2(58f, 135f), bodyColor, false);
        Image tail = CreateImage(card, "DogTail", new Vector2(-160f, 95f), new Vector2(42f, 140f), bodyColor, false);
        tail.rectTransform.localEulerAngles = new Vector3(0f, 0f, -42f);
        Image nose = CreateImage(card, "DogNose", new Vector2(210f, 90f), new Vector2(44f, 44f), bodyColor, true);

        menuPreviewParts.Add(body);
        menuPreviewParts.Add(head);
        menuPreviewParts.Add(frontLeg);
        menuPreviewParts.Add(backLeg);
        menuPreviewParts.Add(tail);
        menuPreviewParts.Add(nose);

        equippedNameText = CreateText(
            card,
            "EquippedSkinName",
            "EQUIPPED  •  CLASSIC",
            new Vector2(0f, -175f),
            new Vector2(560f, 55f),
            28f,
            Gold,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
    }

    private void BuildMarketScreen()
    {
        marketScreen = CreateScreen(
            "ModernMarketScreen",
            MenuSkyTop,
            MenuSkyBottom);

        CreateImage(
            marketScreen.transform,
            "MarketTreeBubbleLeft",
            new Vector2(-490f, 520f),
            new Vector2(220f, 220f),
            new Color(MenuGreen.r, MenuGreen.g, MenuGreen.b, 0.34f),
            true);
        CreateImage(
            marketScreen.transform,
            "MarketTreeBubbleRight",
            new Vector2(500f, 180f),
            new Vector2(260f, 260f),
            new Color(MenuGreen.r, MenuGreen.g, MenuGreen.b, 0.24f),
            true);

        CreateTopBar(marketScreen.transform, "Market", () => ShowSettingsScreen(SettingsReturnTarget.Main));
        CreateRibbon(marketScreen.transform, "MARKET", new Vector2(0f, 800f));

        RectTransform shelves = CreateCard(
            marketScreen.transform,
            "MarketShelvesCard",
            new Vector2(0f, -45f),
            new Vector2(910f, 1390f),
            MenuBlueDark,
            new Color(0.43f, 0.76f, 1f, 0.58f));

        CreateText(
            shelves,
            "MarketSubtitle",
            "ITEMS",
            new Vector2(0f, 605f),
            new Vector2(760f, 64f),
            31f,
            Color.white,
            FontStyles.Bold,
            TextAlignmentOptions.Center);

        for (int index = 0; index < 6; index++)
        {
            int column = index % 2;
            int row = index / 2;
            Vector2 position = new Vector2(
                column == 0 ? -215f : 215f,
                355f - row * 365f);

            RectTransform slot = CreateCard(
                shelves,
                "MarketEmptySlot_" + index,
                position,
                new Vector2(390f, 310f),
                new Color(0.16f, 0.52f, 0.72f, 0.36f),
                new Color(0.72f, 0.90f, 1f, 0.62f));

            Image slotImage = slot.GetComponent<Image>();
            if (slotImage != null)
            {
                slotImage.raycastTarget = false;
            }

            Image innerHighlight = CreateImage(
                slot,
                "EmptySlotHighlight",
                new Vector2(0f, 22f),
                new Vector2(330f, 220f),
                new Color(1f, 1f, 1f, 0.055f),
                false);
            innerHighlight.raycastTarget = false;
        }

        CreateRoundNavButton(marketScreen.transform, "MarketClose", "×", new Vector2(-90f, -1035f), ShowMainScreen);
        CreateRoundNavButton(marketScreen.transform, "MarketSkins", "♛", new Vector2(90f, -1035f), ShowSkinsScreen);
    }

    private void BuildSkinsScreen()
    {
        skinsScreen = CreateScreen(
            "ModernSkinsScreen",
            MenuSkyTop,
            MenuSkyBottom);

        CreateImage(
            skinsScreen.transform,
            "SkinsTreeBubbleLeft",
            new Vector2(-500f, 260f),
            new Vector2(250f, 250f),
            new Color(MenuGreen.r, MenuGreen.g, MenuGreen.b, 0.28f),
            true);
        CreateImage(
            skinsScreen.transform,
            "SkinsTreeBubbleRight",
            new Vector2(500f, 570f),
            new Vector2(210f, 210f),
            new Color(MenuGreen.r, MenuGreen.g, MenuGreen.b, 0.30f),
            true);

        CreateTopBar(skinsScreen.transform, "Skins", () => ShowSettingsScreen(SettingsReturnTarget.Main));
        CreateRibbon(skinsScreen.transform, "SKINS", new Vector2(0f, 800f));

        RectTransform collection = CreateCard(
            skinsScreen.transform,
            "SkinCollectionCard",
            new Vector2(0f, -45f),
            new Vector2(910f, 1390f),
            MenuBlueDark,
            new Color(0.43f, 0.76f, 1f, 0.58f));
        CreateText(
            collection,
            "SkinsSubtitle",
            "YOUR COLLECTION",
            new Vector2(0f, 610f),
            new Vector2(760f, 64f),
            31f,
            Color.white,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        BuildSkinGrid(collection, false);

        CreateRoundNavButton(skinsScreen.transform, "SkinsMarket", "▣", new Vector2(-90f, -1035f), ShowMarketScreen);
        CreateRoundNavButton(skinsScreen.transform, "SkinsClose", "×", new Vector2(90f, -1035f), ShowMainScreen);
    }

    private void BuildSkinGrid(Transform parent, bool marketMode)
    {
        BalloonDogSkinDefinition[] skins = BalloonDogEconomy.Skins;
        for (int index = 0; index < skins.Length; index++)
        {
            BalloonDogSkinDefinition skin = skins[index];
            int column = index % 2;
            int row = index / 2;
            Vector2 position = new Vector2(
                column == 0 ? -215f : 215f,
                415f - row * 370f);

            RectTransform card = CreateCard(
                parent,
                (marketMode ? "Market_" : "Skins_") + skin.Id,
                position,
                new Vector2(390f, 320f),
                MenuBlueCard,
                new Color(skin.AccentColor.r, skin.AccentColor.g, skin.AccentColor.b, 0.72f));

            Image swatch = CreateImage(
                card,
                "SkinSwatch",
                new Vector2(0f, 78f),
                new Vector2(130f, 130f),
                skin.PrimaryColor,
                true);
            AddGraphicShadow(swatch, new Color(0f, 0f, 0f, 0.34f), new Vector2(5f, -7f));

            CreateText(
                card,
                "SkinName",
                skin.DisplayName,
                new Vector2(0f, -12f),
                new Vector2(360f, 50f),
                29f,
                Color.white,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

            TMP_Text status = CreateText(
                card,
                "SkinStatus",
                string.Empty,
                new Vector2(0f, -58f),
                new Vector2(350f, 42f),
                21f,
                new Color(0.78f, 0.90f, 0.98f, 1f),
                FontStyles.Bold,
                TextAlignmentOptions.Center);

            Button action = CreateButton(
                card,
                "SkinAction",
                "BUY",
                new Vector2(0f, -118f),
                new Vector2(310f, 76f),
                MenuGreen,
                Color.white,
                null,
                25f);

            TMP_Text actionLabel = action.GetComponentInChildren<TMP_Text>(true);
            SkinCardView view = new SkinCardView
            {
                Skin = skin,
                MarketMode = marketMode,
                Swatch = swatch,
                Status = status,
                ActionLabel = actionLabel,
                ActionButton = action
            };
            skinCards.Add(view);

            string capturedSkinId = skin.Id;
            action.onClick.AddListener(() => HandleSkinAction(capturedSkinId, marketMode));
        }
    }

    private void BuildSettingsScreen()
    {
        settingsScreen = CreateScreen(
            "ModernSettingsScreen",
            MenuSkyTop,
            MenuSkyBottom);

        CreateImage(
            settingsScreen.transform,
            "SettingsTreeBubbleLeft",
            new Vector2(-500f, 450f),
            new Vector2(230f, 230f),
            new Color(MenuGreen.r, MenuGreen.g, MenuGreen.b, 0.27f),
            true);
        CreateImage(
            settingsScreen.transform,
            "SettingsTreeBubbleRight",
            new Vector2(505f, -50f),
            new Vector2(280f, 280f),
            new Color(MenuGreen.r, MenuGreen.g, MenuGreen.b, 0.22f),
            true);

        CreateTopBar(settingsScreen.transform, "Settings", CloseSettings);
        CreateRibbon(settingsScreen.transform, "SETTINGS", new Vector2(0f, 800f));

        RectTransform card = CreateCard(
            settingsScreen.transform,
            "SettingsCard",
            new Vector2(0f, -55f),
            new Vector2(910f, 1390f),
            MenuBlueDark,
            new Color(0.43f, 0.76f, 1f, 0.58f));

        CreateText(
            card,
            "AudioSection",
            "SOUND EFFECTS",
            new Vector2(0f, 585f),
            new Vector2(700f, 58f),
            24f,
            new Color(0.76f, 0.90f, 1f, 1f),
            FontStyles.Bold,
            TextAlignmentOptions.Center);

        CreateText(
            card,
            "VolumeLabel",
            "MUSIC",
            new Vector2(-275f, 450f),
            new Vector2(260f, 54f),
            25f,
            Color.white,
            FontStyles.Bold,
            TextAlignmentOptions.Left);

        volumeSlider = CreateSlider(card, new Vector2(150f, 450f), new Vector2(420f, 48f));
        volumeSlider.value = PlayerPrefs.GetFloat(MasterVolumeKey, 0.85f);
        volumeSlider.onValueChanged.AddListener(SetMasterVolume);

        soundToggle = CreateToggleRow(card, "SFX", new Vector2(0f, 300f), ToggleSound);

        CreateText(
            card,
            "AboutSection",
            "PRIVACY & SECURITY",
            new Vector2(0f, 140f),
            new Vector2(700f, 58f),
            24f,
            new Color(0.76f, 0.90f, 1f, 1f),
            FontStyles.Bold,
            TextAlignmentOptions.Center);

        vibrationToggle = CreateToggleRow(card, "VIBRATION", new Vector2(0f, 0f), ToggleVibration);
        CreateInfoRow(card, "SAVE DATA", "LOCAL DEVICE", new Vector2(0f, -150f));

        CreateButton(
            card,
            "SettingsPrivacyButton",
            "PRIVACY NOTICE",
            new Vector2(0f, -320f),
            new Vector2(700f, 100f),
            MenuGreen,
            Color.white,
            ShowPrivacyScreen,
            28f);

        CreateInfoRow(card, "CONTROL", "DRAG LEFT / RIGHT", new Vector2(0f, -480f));
        CreateRoundNavButton(settingsScreen.transform, "SettingsClose", "×", new Vector2(0f, -1035f), CloseSettings);
    }

    private void BuildPrivacyScreen()
    {
        privacyScreen = CreateScreen(
            "ModernPrivacyScreen",
            new Color(0.39f, 0.07f, 0.98f, 1f),
            new Color(0.32f, 0.05f, 0.91f, 1f));
        CreateTopBar(privacyScreen.transform, "Privacy", ShowSettingsFromPrivacy);
        CreateRibbon(privacyScreen.transform, "PRIVACY", new Vector2(0f, 800f));

        RectTransform card = CreateCard(
            privacyScreen.transform,
            "PrivacyCard",
            new Vector2(0f, -70f),
            new Vector2(910f, 1420f),
            new Color(0.13f, 0.015f, 0.43f, 0.98f),
            new Color(0.34f, 0.05f, 0.75f, 0.92f));

        CreateText(
            card,
            "PrivacyHeading",
            "YOUR DATA STAYS ON THIS DEVICE",
            new Vector2(0f, 575f),
            new Vector2(760f, 100f),
            34f,
            Gold,
            FontStyles.Bold,
            TextAlignmentOptions.Center);

        string privacyCopy =
            "Balloon Dog stores your high score, token balance, owned skins, " +
            "selected skin and settings locally on your device.\n\n" +
            "This prototype does not create an account and does not send " +
            "personal information to a server. Deleting the app or clearing " +
            "its local data may remove your progress.\n\n" +
            "If analytics, advertising, cloud saves or online services are " +
            "added before release, this notice must be updated and a public " +
            "privacy-policy link must be provided in the store listing.\n\n" +
            "Prototype notice • Updated August 7, 2026";

        TMP_Text copy = CreateText(
            card,
            "PrivacyCopy",
            privacyCopy,
            new Vector2(0f, 70f),
            new Vector2(750f, 890f),
            27f,
            new Color(0.91f, 0.89f, 0.96f, 1f),
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft);
        copy.enableWordWrapping = true;
        copy.lineSpacing = 8f;

        CreateButton(
            card,
            "PrivacyBackButton",
            "GOT IT",
            new Vector2(0f, -560f),
            new Vector2(560f, 105f),
            Lime,
            new Color(0.10f, 0.28f, 0.02f, 1f),
            ShowSettingsFromPrivacy,
            31f);
    }

    private void BuildResultScreen()
    {
        resultScreen = CreateScreen(
            "ModernResultScreen",
            new Color(0.39f, 0.07f, 0.98f, 1f),
            new Color(0.45f, 0.05f, 0.83f, 1f));

        resultTitleText = CreateText(
            resultScreen.transform,
            "ResultTitle",
            "GAME OVER",
            new Vector2(0f, 850f),
            new Vector2(760f, 150f),
            76f,
            Color.white,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        AddTextShadow(resultTitleText, new Color(0f, 0f, 0f, 0.65f), new Vector2(5f, -6f));

        resultReasonText = CreateText(
            resultScreen.transform,
            "ResultReason",
            "KEEP GOING",
            new Vector2(0f, 705f),
            new Vector2(680f, 58f),
            25f,
            new Color(0.82f, 0.76f, 0.94f, 1f),
            FontStyles.Bold,
            TextAlignmentOptions.Center);

        CreateText(
            resultScreen.transform,
            "ScoreCaption",
            "SCORE",
            new Vector2(0f, 520f),
            new Vector2(400f, 55f),
            27f,
            Gold,
            FontStyles.Bold,
            TextAlignmentOptions.Center);

        resultScoreText = CreateText(
            resultScreen.transform,
            "ResultScore",
            "0",
            new Vector2(0f, 420f),
            new Vector2(700f, 130f),
            76f,
            Color.white,
            FontStyles.Bold,
            TextAlignmentOptions.Center);

        CreateText(
            resultScreen.transform,
            "TokenCaption",
            "TOKENS",
            new Vector2(0f, 175f),
            new Vector2(400f, 55f),
            27f,
            Gold,
            FontStyles.Bold,
            TextAlignmentOptions.Center);

        resultRewardText = CreateText(
            resultScreen.transform,
            "ResultReward",
            "+0",
            new Vector2(0f, 75f),
            new Vector2(700f, 120f),
            68f,
            Color.white,
            FontStyles.Bold,
            TextAlignmentOptions.Center);

        CreateText(
            resultScreen.transform,
            "BestCaption",
            "BEST",
            new Vector2(0f, -160f),
            new Vector2(400f, 55f),
            27f,
            Gold,
            FontStyles.Bold,
            TextAlignmentOptions.Center);

        resultBestText = CreateText(
            resultScreen.transform,
            "ResultBest",
            "0",
            new Vector2(0f, -260f),
            new Vector2(700f, 120f),
            68f,
            Color.white,
            FontStyles.Bold,
            TextAlignmentOptions.Center);

        CreateButton(
            resultScreen.transform,
            "ModernRestartButton",
            "REVIVE",
            new Vector2(0f, -665f),
            new Vector2(790f, 150f),
            Lime,
            new Color(0.10f, 0.28f, 0.02f, 1f),
            RestartGame,
            40f);

        CreateButton(
            resultScreen.transform,
            "ModernResultMenuButton",
            "NO THANKS",
            new Vector2(0f, -855f),
            new Vector2(420f, 70f),
            new Color(1f, 1f, 1f, 0.05f),
            Color.white,
            ReturnToMainMenu,
            24f);

        CreateText(
            resultScreen.transform,
            "ResultHint",
            "TOKENS ARE SAVED AUTOMATICALLY",
            new Vector2(0f, -1010f),
            new Vector2(660f, 45f),
            20f,
            Muted,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
    }

    private void BuildPauseScreen()
    {
        pauseScreen = CreateScreen("ModernPauseScreen", new Color(0.24f, 0.025f, 0.55f, 0.98f), new Color(0.02f, 0.015f, 0.06f, 0.98f));

        RectTransform card = CreateCard(
            pauseScreen.transform,
            "PauseCard",
            Vector2.zero,
            new Vector2(800f, 1020f),
            new Color(0.055f, 0.035f, 0.14f, 0.98f),
            new Color(0.54f, 0.24f, 1f, 0.75f));
        card.gameObject.AddComponent<UiPanelAnimator>();

        CreateText(
            card,
            "PauseTitle",
            "PAUSED",
            new Vector2(0f, 355f),
            new Vector2(650f, 120f),
            68f,
            Color.white,
            FontStyles.Bold,
            TextAlignmentOptions.Center);

        CreateButton(card, "ModernResumeButton", "RESUME", new Vector2(0f, 160f), new Vector2(610f, 120f), Lime, Ink, ResumeGame, 36f);
        CreateButton(card, "PauseSettingsButton", "SETTINGS", new Vector2(0f, 15f), new Vector2(610f, 110f), PurpleBright, Color.white, () => ShowSettingsScreen(SettingsReturnTarget.Pause), 31f);
        CreateButton(card, "PauseRestartButtonModern", "RESTART", new Vector2(0f, -130f), new Vector2(610f, 110f), Orange, Ink, RestartGame, 31f);
        CreateButton(card, "PauseMenuButtonModern", "MAIN MENU", new Vector2(0f, -275f), new Vector2(610f, 105f), InkSoft, Color.white, ReturnToMainMenu, 29f);

        CreateText(
            card,
            "PauseHint",
            "YOUR RUN IS WAITING",
            new Vector2(0f, -405f),
            new Vector2(600f, 45f),
            20f,
            Muted,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
    }

    private void BuildGameplayOverlay()
    {
        gameplayOverlay = new GameObject("ModernGameplayOverlay", typeof(RectTransform));
        gameplayOverlay.transform.SetParent(safeRoot, false);
        Stretch(gameplayOverlay.GetComponent<RectTransform>());

        CreateButton(
            gameplayOverlay.transform,
            "ModernPauseButton",
            "II",
            new Vector2(430f, 1030f),
            new Vector2(105f, 105f),
            InkSoft,
            Color.white,
            PauseGame,
            38f);

        gameplaySkinText = CreatePillText(
            gameplayOverlay.transform,
            "GameplaySkin",
            "CLASSIC",
            new Vector2(-350f, 1030f),
            new Vector2(310f, 74f),
            new Color(0.04f, 0.03f, 0.10f, 0.80f),
            Color.white);
        gameplaySkinText.fontSizeMax = 24f;
    }

    private void BuildToast()
    {
        RectTransform toast = CreateCard(
            safeRoot,
            "Toast",
            new Vector2(0f, -1040f),
            new Vector2(650f, 92f),
            new Color(0.035f, 0.025f, 0.085f, 0.96f),
            new Color(1f, 0.50f, 0.08f, 0.75f));
        toast.SetAsLastSibling();
        toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;
        toastGroup.blocksRaycasts = false;
        toastGroup.interactable = false;
        toastText = CreateText(
            toast,
            "ToastText",
            string.Empty,
            Vector2.zero,
            new Vector2(590f, 70f),
            26f,
            Color.white,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
    }

    private void CreateHeader(Transform parent, string title, UnityAction backAction)
    {
        CreateButton(
            parent,
            title + "BackButton",
            "<",
            new Vector2(-430f, 755f),
            new Vector2(105f, 105f),
            InkSoft,
            Color.white,
            backAction,
            44f);

        TMP_Text heading = CreateText(
            parent,
            title + "Title",
            title,
            new Vector2(0f, 755f),
            new Vector2(600f, 115f),
            64f,
            Color.white,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        AddTextShadow(heading, new Color(0f, 0f, 0f, 0.55f), new Vector2(4f, -5f));

        CreatePillText(
            parent,
            title + "Coins",
            "◉  " + BalloonDogEconomy.Coins,
            new Vector2(350f, 755f),
            new Vector2(270f, 80f),
            Orange,
            Ink).gameObject.AddComponent<BalloonDogCoinLabel>();
    }

    private void CreateTopBar(Transform parent, string screenName, UnityAction rightAction)
    {
        CreatePillText(
            parent,
            screenName + "Tokens",
            "◉  " + BalloonDogEconomy.Coins,
            new Vector2(-365f, 1030f),
            new Vector2(290f, 92f),
            MenuCream,
            new Color(0.31f, 0.18f, 0.02f, 1f)).gameObject.AddComponent<BalloonDogCoinLabel>();

        CreateButton(
            parent,
            screenName + "TopButton",
            screenName == "Settings" || screenName == "Privacy" ? "X" : "⚙",
            new Vector2(430f, 1030f),
            new Vector2(105f, 105f),
            MenuBlue,
            Color.white,
            rightAction,
            46f);
    }

    private static void CreateRibbon(Transform parent, string title, Vector2 position)
    {
        RectTransform ribbon = CreateCard(
            parent,
            title + "Ribbon",
            position,
            new Vector2(720f, 180f),
            MenuBlue,
            new Color(1f, 1f, 1f, 0.20f));

        CreateImage(
            ribbon,
            "GreenAccent",
            new Vector2(0f, -70f),
            new Vector2(520f, 18f),
            MenuGreen,
            false);

        CreateText(
            ribbon,
            "Label",
            title,
            new Vector2(0f, 8f),
            new Vector2(640f, 112f),
            54f,
            Color.white,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
    }

    private static Button CreateRoundNavButton(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        UnityAction action)
    {
        Button button = CreateButton(
            parent,
            name,
            label,
            position,
            new Vector2(118f, 118f),
            MenuBlue,
            Color.white,
            action,
            44f);
        Image image = button.GetComponent<Image>();
        image.sprite = GetCircleSprite();
        image.type = Image.Type.Simple;
        return button;
    }

    private static Image CreateResourceImage(
        Transform parent,
        string name,
        string resourcePath,
        Vector2 position,
        Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent);
        SetRect(rect, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = GetResourceSprite(resourcePath);
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static Button CreateTransparentButton(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        UnityAction action)
    {
        RectTransform rect = CreateRect(name, parent);
        SetRect(rect, position, size);
        Image hitArea = rect.gameObject.AddComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0.001f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = hitArea;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(action);
        rect.gameObject.AddComponent<MenuPressScale>();
        return button;
    }

    private Slider CreateSlider(Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform root = CreateRect("VolumeSlider", parent);
        SetRect(root, position, size);

        Image background = CreateImage(root, "Background", Vector2.zero, size, new Color(0.83f, 0.91f, 0.95f, 1f), false);
        background.raycastTarget = true;

        RectTransform fillArea = CreateRect("Fill Area", root);
        fillArea.anchorMin = new Vector2(0f, 0f);
        fillArea.anchorMax = new Vector2(1f, 1f);
        fillArea.offsetMin = new Vector2(10f, 10f);
        fillArea.offsetMax = new Vector2(-10f, -10f);

        Image fill = CreateImage(fillArea, "Fill", Vector2.zero, Vector2.zero, MenuGreen, false);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;

        Image handle = CreateImage(root, "Handle", Vector2.zero, new Vector2(72f, 72f), MenuBlue, true);
        AddGraphicShadow(handle, new Color(0f, 0f, 0f, 0.30f), new Vector2(3f, -4f));

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        return slider;
    }

    private ToggleRowView CreateToggleRow(
        Transform parent,
        string label,
        Vector2 position,
        UnityAction action)
    {
        RectTransform row = CreateCard(
            parent,
            label + "Row",
            position,
            new Vector2(760f, 120f),
            MenuBlueCard,
            new Color(0.62f, 0.84f, 1f, 0.42f));

        CreateText(
            row,
            label + "Label",
            label,
            new Vector2(-235f, 0f),
            new Vector2(280f, 75f),
            29f,
            Color.white,
            FontStyles.Bold,
            TextAlignmentOptions.Left);

        Button button = CreateButton(
            row,
            label + "Toggle",
            string.Empty,
            new Vector2(260f, 0f),
            new Vector2(190f, 76f),
            MenuBlueDark,
            Color.white,
            action,
            23f);

        Image stateBackground = button.GetComponent<Image>();
        TMP_Text stateLabel = button.GetComponentInChildren<TMP_Text>(true);
        return new ToggleRowView
        {
            StateLabel = stateLabel,
            StateBackground = stateBackground
        };
    }

    private void CreateInfoRow(Transform parent, string label, string value, Vector2 position)
    {
        RectTransform row = CreateCard(
            parent,
            label + "InfoRow",
            position,
            new Vector2(760f, 105f),
            new Color(0.055f, 0.29f, 0.44f, 1f),
            new Color(0.62f, 0.84f, 1f, 0.34f));

        CreateText(row, "Label", label, new Vector2(-230f, 0f), new Vector2(290f, 60f), 25f, Color.white, FontStyles.Bold, TextAlignmentOptions.Left);
        CreateText(row, "Value", value, new Vector2(190f, 0f), new Vector2(380f, 60f), 23f, MenuCream, FontStyles.Bold, TextAlignmentOptions.Right);
    }

    private void StartGame()
    {
        resultRewardGranted = false;
        lastRunReward = 0;
        HideAllScreens();
        gameplayOverlay.SetActive(true);
        menuController ??= FindAnyObjectByType<MainMenuController>();
        menuController?.StartGame();
    }

    private void PauseGame()
    {
        if (gameManager != null && gameManager.IsGameOver)
        {
            return;
        }

        Time.timeScale = 0f;
        gameplayOverlay.SetActive(false);
        HideAllScreens();
        pauseScreen.SetActive(true);
    }

    private void ResumeGame()
    {
        HideAllScreens();
        gameplayOverlay.SetActive(true);
        Time.timeScale = 1f;
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        gameManager ??= GameManager.Instance ?? FindAnyObjectByType<GameManager>();
        gameManager?.RestartGame();
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        gameManager ??= GameManager.Instance ?? FindAnyObjectByType<GameManager>();
        gameManager?.ReturnToMainMenu();
    }

    private void ShowMainScreen()
    {
        Time.timeScale = 0f;
        HideAllScreens();
        gameplayOverlay.SetActive(false);
        mainScreen.SetActive(true);
        DisableLegacyPanels();
        RefreshPersistentViews();
    }

    private void ShowMarketScreen()
    {
        HideAllScreens();
        gameplayOverlay.SetActive(false);
        marketScreen.SetActive(true);
        RefreshPersistentViews();
    }

    private void ShowSkinsScreen()
    {
        HideAllScreens();
        gameplayOverlay.SetActive(false);
        skinsScreen.SetActive(true);
        RefreshPersistentViews();
    }

    private void ShowSettingsScreen(SettingsReturnTarget returnTarget)
    {
        settingsReturnTarget = returnTarget;
        Time.timeScale = 0f;
        HideAllScreens();
        gameplayOverlay.SetActive(false);
        settingsScreen.SetActive(true);
        RefreshSettingsViews();
    }

    private void CloseSettings()
    {
        if (settingsReturnTarget == SettingsReturnTarget.Pause)
        {
            HideAllScreens();
            pauseScreen.SetActive(true);
            return;
        }

        ShowMainScreen();
    }

    private void ShowPrivacyScreen()
    {
        privacyReturnToSettings = true;
        if (pauseScreen != null && pauseScreen.activeSelf)
        {
            settingsReturnTarget = SettingsReturnTarget.Pause;
        }

        HideAllScreens();
        gameplayOverlay.SetActive(false);
        privacyScreen.SetActive(true);
    }

    private void ShowPrivacyFromMain()
    {
        privacyReturnToSettings = false;
        HideAllScreens();
        gameplayOverlay.SetActive(false);
        privacyScreen.SetActive(true);
    }

    private void ShowSettingsFromPrivacy()
    {
        if (privacyReturnToSettings)
        {
            ShowSettingsScreen(settingsReturnTarget);
        }
        else
        {
            ShowMainScreen();
        }
    }

    private void ShowGameplayOverlay()
    {
        HideAllScreens();
        gameplayOverlay.SetActive(true);
    }

    private void HideAllScreens()
    {
        SetActive(mainScreen, false);
        SetActive(marketScreen, false);
        SetActive(skinsScreen, false);
        SetActive(settingsScreen, false);
        SetActive(privacyScreen, false);
        SetActive(resultScreen, false);
        SetActive(pauseScreen, false);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void HandleGameEnded()
    {
        if (gameManager == null || !gameManager.IsGameOver || resultScreen == null)
        {
            return;
        }

        if (!resultRewardGranted)
        {
            lastRunReward = BalloonDogEconomy.CalculateRunReward(
                gameManager.LastScore,
                gameManager.LastRunCompleted);
            BalloonDogEconomy.AddCoins(lastRunReward);
            resultRewardGranted = true;
        }

        resultTitleText.text = gameManager.LastRunCompleted
            ? "LEVEL COMPLETE"
            : "GAME OVER";
        resultTitleText.color = gameManager.LastRunCompleted ? Lime : Color.white;
        resultReasonText.text = string.IsNullOrWhiteSpace(gameManager.LastEndReason)
            ? "KEEP GOING"
            : gameManager.LastEndReason.ToUpperInvariant();
        resultScoreText.text = gameManager.LastScore.ToString("N0");
        resultBestText.text = GameManager.BestScore.ToString("N0");
        resultRewardText.text = "+" + lastRunReward.ToString("N0");

        HideAllScreens();
        gameplayOverlay.SetActive(false);
        resultScreen.SetActive(true);
        resultScreen.transform.SetAsLastSibling();
        Time.timeScale = 0f;
        RefreshPersistentViews();
    }

    private void HandleSkinAction(string skinId, bool marketMode)
    {
        bool owned = BalloonDogEconomy.IsOwned(skinId);
        bool success;

        if (owned)
        {
            success = BalloonDogEconomy.Equip(skinId);
        }
        else if (marketMode)
        {
            ShowMarketScreen();
            ShowToast("USE THE MARKET REWARD WHEEL");
            return;
        }
        else
        {
            ShowMarketScreen();
            ShowToast("UNLOCK IT IN THE MARKET");
            return;
        }

        if (success)
        {
            BalloonDogSkinSystem.ApplySelectedSkin();
            RefreshPersistentViews();
        }
    }

    private void StartWheelUnlock()
    {
        if (wheelSpinning)
        {
            return;
        }

        if (BalloonDogEconomy.AllSkinsOwned)
        {
            ShowToast("ALL SKINS ARE OWNED");
            return;
        }

        if (BalloonDogEconomy.Coins < WheelTokenCost)
        {
            ShowToast("NOT ENOUGH TOKENS");
            return;
        }

        StartCoroutine(AnimateWheelUnlock());
    }

    private IEnumerator AnimateWheelUnlock()
    {
        wheelSpinning = true;
        if (wheelButton != null)
        {
            wheelButton.interactable = false;
        }
        if (wheelButtonLabel != null)
        {
            wheelButtonLabel.text = "UNLOCKING...";
        }

        BalloonDogSkinDefinition reward = BalloonDogEconomy.PeekNextLockedSkin();
        int rewardIndex = 0;
        BalloonDogSkinDefinition[] skins = BalloonDogEconomy.Skins;
        for (int index = 0; index < skins.Length; index++)
        {
            if (skins[index].Id == reward.Id)
            {
                rewardIndex = index;
                break;
            }
        }

        float startAngle = wheelTransform != null
            ? wheelTransform.localEulerAngles.z
            : 0f;
        float targetAngle = startAngle - 1440f + rewardIndex * 60f;
        const float duration = 1.8f;
        float elapsed = 0f;
        while (elapsed < duration && wheelTransform != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);
            wheelTransform.localEulerAngles = new Vector3(
                0f,
                0f,
                Mathf.LerpUnclamped(startAngle, targetAngle, eased));
            yield return null;
        }

        if (BalloonDogEconomy.TryUnlockNextSkin(WheelTokenCost, out BalloonDogSkinDefinition unlocked))
        {
            BalloonDogSkinSystem.ApplySelectedSkin();
            ShowToast(unlocked.DisplayName + " UNLOCKED + EQUIPPED");
        }
        else
        {
            ShowToast("UNLOCK COULD NOT BE COMPLETED");
        }

        wheelSpinning = false;
        RefreshPersistentViews();
    }

    private void ToggleSound()
    {
        menuController ??= FindAnyObjectByType<MainMenuController>();
        menuController?.ToggleSound();
        ApplySavedAudioSettings();
        RefreshSettingsViews();
    }

    private void ToggleVibration()
    {
        menuController ??= FindAnyObjectByType<MainMenuController>();
        menuController?.ToggleVibration();
        RefreshSettingsViews();
    }

    private void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        PlayerPrefs.Save();

        bool soundEnabled = IsSoundEnabled();
        AudioListener.volume = soundEnabled ? value : 0f;
    }

    private void ApplySavedAudioSettings()
    {
        float volume = PlayerPrefs.GetFloat(MasterVolumeKey, 0.85f);
        bool soundEnabled = IsSoundEnabled();
        AudioListener.volume = soundEnabled ? volume : 0f;
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(volume);
        }
    }

    private void RefreshPersistentViews()
    {
        int coins = BalloonDogEconomy.Coins;
        BalloonDogSkinDefinition equipped = BalloonDogEconomy.EquippedSkin;
        lastKnownCoins = coins;
        lastKnownSkin = equipped.Id;

        if (coinText != null)
        {
            coinText.text = "◉  " + coins.ToString("N0");
        }
        if (mainBestText != null)
        {
            mainBestText.text = "BEST  " + GameManager.BestScore.ToString("N0");
        }
        if (equippedNameText != null)
        {
            equippedNameText.text = "EQUIPPED  •  " + equipped.DisplayName;
        }
        if (gameplaySkinText != null)
        {
            gameplaySkinText.text = equipped.DisplayName;
        }

        foreach (Image part in menuPreviewParts)
        {
            if (part != null)
            {
                part.color = equipped.PrimaryColor;
            }
        }

        foreach (SkinCardView view in skinCards)
        {
            RefreshSkinCard(view, coins, equipped.Id);
        }

        foreach (BalloonDogCoinLabel label in
                 safeRoot != null
                     ? safeRoot.GetComponentsInChildren<BalloonDogCoinLabel>(true)
                     : new BalloonDogCoinLabel[0])
        {
            TMP_Text text = label.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.text = "◉  " + coins.ToString("N0");
            }
        }

        RefreshWheelView();
        RefreshSettingsViews();
    }

    private void RefreshWheelView()
    {
        if (wheelNextText == null || wheelButton == null)
        {
            return;
        }

        Image buttonImage = wheelButton.GetComponent<Image>();
        if (BalloonDogEconomy.AllSkinsOwned)
        {
            wheelNextText.text = "COLLECTION COMPLETE";
            wheelNextText.color = Lime;
            if (wheelButtonLabel != null)
            {
                wheelButtonLabel.text = "ALL SKINS OWNED";
            }
            wheelButton.interactable = false;
            if (buttonImage != null)
            {
                buttonImage.color = Lime;
            }
            return;
        }

        BalloonDogSkinDefinition next = BalloonDogEconomy.PeekNextLockedSkin();
        wheelNextText.text = "NEXT REWARD  •  " + next.DisplayName;
        wheelNextText.color = next.AccentColor;
        if (wheelButtonLabel != null && !wheelSpinning)
        {
            wheelButtonLabel.text = BalloonDogEconomy.Coins >= WheelTokenCost
                ? "UNLOCK NEXT  •  " + WheelTokenCost + " TOKENS"
                : "NEED " + WheelTokenCost + " TOKENS";
        }
        wheelButton.interactable = !wheelSpinning;
        if (buttonImage != null)
        {
            buttonImage.color = BalloonDogEconomy.Coins >= WheelTokenCost
                ? Orange
                : InkSoft;
        }
    }

    private static void RefreshSkinCard(SkinCardView view, int coins, string equippedId)
    {
        if (view == null || view.ActionButton == null)
        {
            return;
        }

        bool owned = BalloonDogEconomy.IsOwned(view.Skin.Id);
        bool equipped = string.Equals(view.Skin.Id, equippedId);
        Image buttonImage = view.ActionButton.GetComponent<Image>();

        if (equipped)
        {
            view.Status.text = "CURRENT LOOK";
            view.Status.color = MenuGreen;
            view.ActionLabel.text = "EQUIPPED";
            buttonImage.color = MenuGreen;
            view.ActionButton.interactable = false;
        }
        else if (owned)
        {
            view.Status.text = "OWNED";
            view.Status.color = MenuGreen;
            view.ActionLabel.text = "EQUIP";
            buttonImage.color = MenuBlue;
            view.ActionButton.interactable = true;
        }
        else if (view.MarketMode)
        {
            view.Status.text = coins >= view.Skin.Price ? "AVAILABLE" : "KEEP RUNNING";
            view.Status.color = coins >= view.Skin.Price ? MenuCream : Muted;
            view.ActionLabel.text = view.Skin.Price + " TOKENS";
            buttonImage.color = coins >= view.Skin.Price ? MenuGreen : MenuBlueDark;
            view.ActionButton.interactable = true;
        }
        else
        {
            view.Status.text = "LOCKED";
            view.Status.color = new Color(0.72f, 0.84f, 0.92f, 1f);
            view.ActionLabel.text = "MARKET";
            buttonImage.color = MenuBlueDark;
            view.ActionButton.interactable = true;
        }
    }

    private void RefreshSettingsViews()
    {
        if (soundToggle != null)
        {
            bool enabled = IsSoundEnabled();
            SetToggleVisual(soundToggle, enabled);
        }

        if (vibrationToggle != null)
        {
            SetToggleVisual(vibrationToggle, MainMenuController.VibrationEnabled);
        }
    }

    private static void SetToggleVisual(ToggleRowView view, bool enabled)
    {
        if (view.StateLabel != null)
        {
            view.StateLabel.text = enabled ? "ON" : "OFF";
            view.StateLabel.color = Color.white;
        }
        if (view.StateBackground != null)
        {
            view.StateBackground.color = enabled ? MenuGreen : MenuBlueDark;
        }
    }

    private static bool IsSoundEnabled()
    {
        return PlayerPrefs.GetInt(SoundPreferenceKey, 1) == 1;
    }

    private static bool DetectGameplayState()
    {
        GameObject legacyMain = FindNamedSceneObject("MainMenuPanel");
        if (legacyMain != null && legacyMain.activeSelf)
        {
            return false;
        }

        GameObject gameplayHud = FindNamedSceneObject("V5GameplayHud");
        if (gameplayHud != null && gameplayHud.activeSelf)
        {
            return true;
        }

        return Time.timeScale > 0.01f;
    }

    private void ShowToast(string message)
    {
        if (toastText == null || toastGroup == null)
        {
            return;
        }

        toastText.text = message;
        toastTimer = 2.2f;
        toastGroup.alpha = 1f;
        toastGroup.transform.SetAsLastSibling();
    }

    private void UpdateToast()
    {
        if (toastGroup == null)
        {
            return;
        }

        if (toastTimer > 0f)
        {
            toastTimer -= Time.unscaledDeltaTime;
            toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, 1f, Time.unscaledDeltaTime * 8f);
            return;
        }

        toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, 0f, Time.unscaledDeltaTime * 4f);
    }

    private GameObject CreateScreen(string name, Color top, Color bottom)
    {
        RectTransform root = CreateRect(name, safeRoot);
        Stretch(root);

        Image background = root.gameObject.AddComponent<Image>();
        background.color = Color.white;
        background.raycastTarget = true;
        UiVerticalGradient gradient = root.gameObject.AddComponent<UiVerticalGradient>();
        gradient.Configure(top, bottom);

        RectTransform pattern = CreateRect("FigmaPattern", root);
        Stretch(pattern);
        Image patternImage = pattern.gameObject.AddComponent<Image>();
        patternImage.sprite = GetPatternSprite();
        patternImage.type = Image.Type.Tiled;
        patternImage.pixelsPerUnitMultiplier = 0.72f;
        patternImage.color = new Color(1f, 1f, 1f, 0.065f);
        patternImage.raycastTarget = false;
        return root.gameObject;
    }

    private static RectTransform CreateCard(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color fill,
        Color outlineColor)
    {
        RectTransform rect = CreateRect(name, parent);
        SetRect(rect, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = GetRoundedSprite();
        image.type = Image.Type.Sliced;
        image.color = fill;
        image.raycastTarget = true;

        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(3f, -3f);

        AddGraphicShadow(image, new Color(0f, 0f, 0f, 0.45f), new Vector2(0f, -9f));
        return rect;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        Vector2 size,
        Color backgroundColor,
        Color labelColor,
        UnityAction action,
        float fontSize)
    {
        RectTransform rect = CreateRect(name, parent);
        SetRect(rect, position, size);

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = GetRoundedSprite();
        image.type = Image.Type.Sliced;
        image.color = backgroundColor;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.48f, 0.48f, 0.55f, 0.72f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        if (action != null)
        {
            button.onClick.AddListener(action);
        }

        rect.gameObject.AddComponent<MenuPressScale>();
        AddGraphicShadow(image, new Color(0f, 0f, 0f, 0.45f), new Vector2(0f, -8f));

        TMP_Text text = CreateText(
            rect,
            "Label",
            label,
            Vector2.zero,
            size - new Vector2(26f, 18f),
            fontSize,
            labelColor,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        text.raycastTarget = false;
        return button;
    }

    private static TMP_Text CreatePillText(
        Transform parent,
        string name,
        string text,
        Vector2 position,
        Vector2 size,
        Color background,
        Color? foreground = null)
    {
        RectTransform card = CreateCard(
            parent,
            name,
            position,
            size,
            background,
            new Color(1f, 1f, 1f, 0.08f));
        TMP_Text label = CreateText(
            card,
            "Label",
            text,
            Vector2.zero,
            size - new Vector2(24f, 14f),
            28f,
            foreground ?? Color.white,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = 30f;
        return label;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string text,
        Vector2 position,
        Vector2 size,
        float fontSize,
        Color color,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        SetRect(rect, position, size);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.fontStyle = style;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        return label;
    }

    private static Image CreateImage(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color color,
        bool circle)
    {
        RectTransform rect = CreateRect(name, parent);
        SetRect(rect, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = circle ? GetCircleSprite() : GetRoundedSprite();
        image.type = circle ? Image.Type.Simple : Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void CreateDecorBubble(
        Transform parent,
        Vector2 position,
        float size,
        float alpha)
    {
        Image bubble = CreateImage(
            parent,
            "DecorBubble",
            position,
            Vector2.one * size,
            new Color(1f, 1f, 1f, alpha),
            true);
        bubble.transform.SetAsFirstSibling();
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void AddGraphicShadow(Graphic graphic, Color color, Vector2 distance)
    {
        Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void AddTextShadow(TMP_Text text, Color color, Vector2 distance)
    {
        AddGraphicShadow(text, color, distance);
    }

    private static void RemoveButtonShadow(Button button)
    {
        if (button == null)
        {
            return;
        }

        foreach (Shadow shadow in button.GetComponents<Shadow>())
        {
            Destroy(shadow);
        }
    }

    private static Sprite GetRoundedSprite()
    {
        if (roundedSprite == null)
        {
            roundedSprite = CreateRoundedSprite(64, 18f, true);
        }
        return roundedSprite;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite == null)
        {
            circleSprite = CreateRoundedSprite(64, 31f, false);
        }
        return circleSprite;
    }

    private static Sprite GetResourceSprite(string path)
    {
        if (ResourceSprites.TryGetValue(path, out Sprite cached) && cached != null)
        {
            return cached;
        }

        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        sprite.name = path.Replace('/', '_');
        ResourceSprites[path] = sprite;
        return sprite;
    }

    private static Sprite GetPatternSprite()
    {
        if (patternSprite != null)
        {
            return patternSprite;
        }

        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "BalloonDogFigmaPattern";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        Color32 clear = new Color32(255, 255, 255, 0);
        Color32 mark = new Color32(52, 8, 139, 150);
        Color32[] pixels = new Color32[size * size];

        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = clear;
        }

        PaintCircle(pixels, size, 48, 50, 12, mark);
        PaintCircle(pixels, size, 33, 34, 7, mark);
        PaintCircle(pixels, size, 48, 29, 7, mark);
        PaintCircle(pixels, size, 63, 34, 7, mark);
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        patternSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f);
        patternSprite.name = texture.name;
        return patternSprite;
    }

    private static void PaintCircle(
        Color32[] pixels,
        int textureSize,
        int centerX,
        int centerY,
        int radius,
        Color32 color)
    {
        int radiusSquared = radius * radius;
        for (int y = Mathf.Max(0, centerY - radius); y < Mathf.Min(textureSize, centerY + radius + 1); y++)
        {
            for (int x = Mathf.Max(0, centerX - radius); x < Mathf.Min(textureSize, centerX + radius + 1); x++)
            {
                int deltaX = x - centerX;
                int deltaY = y - centerY;
                if (deltaX * deltaX + deltaY * deltaY <= radiusSquared)
                {
                    pixels[y * textureSize + x] = color;
                }
            }
        }
    }

    private static Sprite CreateRoundedSprite(int size, float radius, bool sliced)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = sliced ? "ModernUiRounded" : "ModernUiCircle";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color32[] pixels = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        float inner = half - radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - half) - inner, 0f);
                float dy = Mathf.Max(Mathf.Abs(y - half) - inner, 0f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                byte alpha = (byte)Mathf.RoundToInt(
                    Mathf.Clamp01(radius + 0.5f - distance) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        Vector4 border = sliced
            ? new Vector4(20f, 20f, 20f, 20f)
            : Vector4.zero;
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            border);
        sprite.name = texture.name;
        return sprite;
    }

    private static GameObject FindNamedSceneObject(string objectName)
    {
        foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (candidate != null && candidate.name == objectName &&
                candidate.gameObject.scene.IsValid())
            {
                return candidate.gameObject;
            }
        }

        return null;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (economySubscribed)
        {
            BalloonDogEconomy.Changed -= RefreshPersistentViews;
            economySubscribed = false;
        }
        if (gameManager != null)
        {
            gameManager.GameEnded -= HandleGameEnded;
        }
    }
}

/// <summary>Anchors its RectTransform to the device's current safe area.</summary>
public sealed class BalloonDogSafeArea : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        Apply();
    }

    private void Update()
    {
        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        if (safeArea != lastSafeArea || screenSize != lastScreenSize)
        {
            Apply();
        }
    }

    private void Apply()
    {
        if (rectTransform == null || Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }
}

/// <summary>Marker used to refresh coin labels created in screen headers.</summary>
public sealed class BalloonDogCoinLabel : MonoBehaviour
{
}
