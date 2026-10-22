using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>
/// Balloon-themed UI cleanup runtime.
/// Keeps the balloon title, fixes the duplicated market issue,
/// cleans coin / settings / level labels, and tidies the result screen.
/// </summary>
public sealed class BalloonDogBalloonThemeRuntime : MonoBehaviour
{
    private const string RuntimeObjectName = "__BalloonDogBalloonThemeRuntime";

    private float nextRefreshTime;
    private static Sprite roundedUiSprite;
    private static Sprite coinIconSprite;
    private static Sprite gearIconSprite;
    private static Sprite marketIconSprite;
    private static Sprite skinsIconSprite;
    private static Sprite secondaryBackgroundSprite;
    private static readonly HashSet<EntityId> PolishedTextIds = new HashSet<EntityId>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntime()
    {
        GameObject existing = GameObject.Find(RuntimeObjectName);
        if (existing != null)
        {
            return;
        }

        GameObject runtime = new GameObject(RuntimeObjectName);
        DontDestroyOnLoad(runtime);
        runtime.AddComponent<BalloonDogBalloonThemeRuntime>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RemoveOld3DLetterObjects();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        RefreshNow();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + 0.35f;
        RefreshNow();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PolishedTextIds.Clear();
        RemoveOld3DLetterObjects();
        RefreshNow();
    }

    private void RefreshNow()
    {
        GameObject mainScreen = FindSceneObject("ModernMainScreen");
        if (mainScreen != null && mainScreen.activeInHierarchy)
        {
            StyleBackground(mainScreen);
            BuildOrRefreshTitle();
            FixMenuButtons();
            FixCoinAndSettings();
            FixBestPill();
            FixLevelSelector();
        }

        FixSecondaryScreens();
        FixPauseScreen();
        FixGameplayHud();
        FixResultScreen();
    }

    private static void FixPauseScreen()
    {
        GameObject screen = FindSceneObject("ModernPauseScreen");
        if (screen == null)
        {
            return;
        }

        foreach (TMP_Text text in screen.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null)
            {
                continue;
            }

            BalloonDogTitanFont.Apply(text);
        }
    }

    private void StyleBackground(GameObject mainScreen)
    {
        Image image = mainScreen.GetComponent<Image>();
        if (image != null)
        {
            image.enabled = true;
            image.color = Color.white;
        }

        UiVerticalGradient gradient = mainScreen.GetComponent<UiVerticalGradient>();
        if (gradient != null)
        {
            gradient.enabled = false;
        }

        SetChildActive(mainScreen.transform, "FigmaPattern", false);
        SetChildActive(mainScreen.transform, "FigmaMainButtons", false);
        SetChildActive(mainScreen.transform, "FigmaMiscButtons", false);
        SetChildActive(mainScreen.transform, "PrivacyLinkButton", false);
    }

    private void BuildOrRefreshTitle()
    {
        RectTransform titleRect = FindRect("MainTitle");
        if (titleRect == null) return;
        TMP_Text legacyText = titleRect.GetComponent<TMP_Text>();
        if (legacyText != null)
        {
            legacyText.text = string.Empty;
            legacyText.enabled = false;
        }
        Transform oldLetters = titleRect.Find("BalloonSpriteTitle");
        if (oldLetters != null) oldLetters.gameObject.SetActive(false);
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(.5f, 1f);
        titleRect.pivot = new Vector2(.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -185f);
        titleRect.sizeDelta = new Vector2(960f, 458f);
        Transform existing = titleRect.Find("BalloonDogLogo");
        RectTransform logo;
        if (existing == null)
        {
            GameObject go = new GameObject("BalloonDogLogo", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(titleRect, false);
            logo = go.GetComponent<RectTransform>();
            Stretch(logo);
        }
        else logo = existing as RectTransform;
        BalloonDogMenuArtwork.Apply(logo, "balloon_dog_logo");
    }

    private void FixMenuButtons()
    {
        GameObject quickMarket = FindSceneObject("QuickMarketButton");
        if (quickMarket != null)
        {
            quickMarket.SetActive(false);
        }

        RectTransform play = FindRect("ModernPlayButton");
        if (play != null)
        {
            play.anchorMin = play.anchorMax = new Vector2(.5f, 0f);
            play.anchoredPosition = new Vector2(0f, 180f);
            play.sizeDelta = new Vector2(890f, 270f);
            StyleButton(play, new Color(0.35f, 0.84f, 0.23f, 1f), "PLAY", 112f);
            SetButtonIcon(play, null);
            SetButtonLabelTuning(play, 8f, new Vector4(30f, 6f, 30f, 12f));
            BalloonDogMenuArtwork.Apply(play, "butonyesil", true);
        }

        RectTransform market = FindRect("MarketNavButton");
        if (market != null)
        {
            market.anchorMin = market.anchorMax = new Vector2(.5f, 0f);
            GetMainNavigationLayout(market.parent, out Vector2 size, out float offset);
            market.anchoredPosition = new Vector2(-offset, 450f);
            market.sizeDelta = size;
            StyleButton(market, new Color(0.18f, 0.54f, 0.96f, 1f), "MARKET", 68f);
            SetButtonLabelTuning(market, 0f, new Vector4(10f, 4f, 10f, 6f));
            SetButtonIcon(market, GetMarketIconSprite(), 104f);
            BalloonDogMenuArtwork.Apply(market, "butonmavi", true);
            LayoutNavigationContent(market);
        }

        RectTransform skins = FindRect("SkinsNavButton");
        if (skins != null)
        {
            skins.anchorMin = skins.anchorMax = new Vector2(.5f, 0f);
            GetMainNavigationLayout(skins.parent, out Vector2 size, out float offset);
            skins.anchoredPosition = new Vector2(offset, 450f);
            skins.sizeDelta = size;
            StyleButton(skins, new Color(0.18f, 0.54f, 0.96f, 1f), "SKINS", 68f);
            SetButtonLabelTuning(skins, 0f, new Vector4(10f, 4f, 10f, 6f));
            SetButtonIcon(skins, GetSkinsIconSprite(), 104f);
            BalloonDogMenuArtwork.Apply(skins, "butonmavi", true);
            LayoutNavigationContent(skins);
        }
    }

    private static void LayoutNavigationContent(RectTransform button)
    {
        RectTransform icon = button.Find("ThemeIcon") as RectTransform;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (icon == null || label == null) return;
        label.margin = Vector4.zero;
        label.enableAutoSizing = false;
        label.fontSize = 68f;
        float labelWidth = label.GetPreferredValues(label.text).x + 8f;
        float scale = Mathf.Min(1f, (button.sizeDelta.x - 80f) / (104f + 20f + labelWidth));
        float iconSize = 104f * scale;
        float gap = 20f * scale;
        labelWidth *= scale;
        label.fontSize *= scale;
        float groupWidth = iconSize + gap + labelWidth;
        icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(.5f, .5f);
        icon.sizeDelta = new Vector2(iconSize, iconSize);
        icon.anchoredPosition = new Vector2((-groupWidth + iconSize) * .5f, 6f);
        RectTransform text = label.rectTransform;
        text.anchorMin = text.anchorMax = text.pivot = new Vector2(.5f, .5f);
        text.anchoredPosition = new Vector2((groupWidth - labelWidth) * .5f, 6f);
        text.sizeDelta = new Vector2(labelWidth, 150f);
    }

    private static void GetMainNavigationLayout(Transform parent, out Vector2 size, out float offset)
    {
        RectTransform screen = parent as RectTransform;
        float width = screen != null && screen.rect.width > 0f ? screen.rect.width : 1080f;
        // Grow the artwork up to 25%, keeping a 32-unit gap and side margins.
        float buttonWidth = Mathf.Min(575f, (width - 96f) * .5f);
        size = new Vector2(buttonWidth, 210f);
        offset = (buttonWidth + 32f) * .5f;
    }

    private void FixCoinAndSettings()
    {
        RectTransform coinCard = FindRect("MainTokens");
        TMP_Text coin = FindComponent<TMP_Text>("MainTokens");
        if (coinCard != null)
        {
            coinCard.anchorMin = new Vector2(0f, 1f);
            coinCard.anchorMax = new Vector2(0f, 1f);
            coinCard.pivot = new Vector2(0f, 1f);
            coinCard.sizeDelta = new Vector2(290f, 96f);
            coinCard.anchoredPosition = new Vector2(18f, -14f);
            EnsurePillBackground(coinCard, new Color(0.91f, 0.86f, 0.63f, 0.96f), new Color(1f, 1f, 1f, 0.03f));
            BalloonDogMenuArtwork.Apply(coinCard, "coin_panel_background");
            SetPillIcon(coinCard, GetCoinIconSprite(), Color.white, 62f, 44f);
            RectTransform icon = coinCard.Find("ThemePillIcon") as RectTransform;
            if (icon != null) icon.anchoredPosition = new Vector2(44f, 6f);
        }

        if (coin != null)
        {
            coin.text = BalloonDogEconomy.Coins.ToString("N0");
            coin.fontSize = 46f;
            coin.fontSizeMin = 20f;
            coin.fontSizeMax = 46f;
            coin.fontStyle = FontStyles.Normal;
            coin.fontWeight = FontWeight.Bold;
            coin.alignment = TextAlignmentOptions.MidlineLeft;
            coin.color = new Color(0.32f, 0.18f, 0.02f, 1f);
            coin.enableWordWrapping = false;
            ApplySmoothLowPolyText(coin, 1.2f);
            RectTransform labelRect = coin.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(88f, 18f);
            labelRect.offsetMax = new Vector2(-18f, -6f);
            coin.margin = Vector4.zero;
        }

        RectTransform settings = FindRect("MainSettingsButton");
        if (settings != null)
        {
            settings.anchorMin = new Vector2(1f, 1f);
            settings.anchorMax = new Vector2(1f, 1f);
            settings.pivot = new Vector2(1f, 1f);
            settings.anchoredPosition = new Vector2(-10f, -10f);
            settings.sizeDelta = new Vector2(112f, 112f);
            StyleIconButton(settings, Color.white, GetGearIconSprite());
            BalloonDogMenuArtwork.Apply(settings, "butonmavi", true);
        }
    }

    private void FixSecondaryScreens()
    {
        GameObject settings = FindSceneObject("ModernSettingsScreen");
        bool ownsSettings = settings != null && settings.transform.Find("__PauseButtonSettingsLayoutV2") != null;
        if (!ownsSettings) StyleSecondaryBackground("ModernSettingsScreen");
        StyleSecondaryBackground("ModernSkinsScreen");
        StyleSecondaryBackground("ModernMarketScreen");
        GameObject privacy = FindSceneObject("ModernPrivacyScreen");
        bool ownsPrivacy = privacy != null && privacy.transform.Find("PrivacyContent") != null;
        if (!ownsPrivacy) StyleSecondaryBackground("ModernPrivacyScreen");

        if (!ownsSettings) StyleSecondaryTypography("ModernSettingsScreen");
        GameObject collection = FindSceneObject("ModernSkinsScreen");
        bool ownsCollection = collection != null && collection.transform.Find("CollectionContent") != null;
        if (!ownsCollection) StyleSecondaryTypography("ModernSkinsScreen");
        StyleSecondaryTypography("ModernMarketScreen");
        if (!ownsPrivacy) StyleSecondaryTypography("ModernPrivacyScreen");

        StyleSecondaryCoin("SettingsTokens");
        StyleSecondaryCoin("SkinsTokens");
        StyleSecondaryCoin("MarketTokens");

        StyleSecondaryTopButton("SettingsTopButton", true);
        StyleSecondaryTopButton("SkinsTopButton", false);
        StyleSecondaryTopButton("MarketTopButton", false);

        if (!ownsSettings) StyleSettingsControls();
        if (!ownsCollection) StyleSkinControls();
        // Market builds its own artwork buttons and Titan One labels.
        // Styling them here recreates legacy TMP labels over the new text.
        StyleMarketExtras();

        if (!ownsSettings) StyleNavigationButton("SettingsClose", "DONE", new Vector2(0f, -1035f), new Vector2(420f, 104f));
        StyleNavigationButton("SkinsMarket", "MARKET", new Vector2(-225f, -1035f), new Vector2(370f, 104f));
        StyleNavigationButton("SkinsClose", "HOME", new Vector2(225f, -1035f), new Vector2(370f, 104f));

        if (!ownsSettings) RemoveSecondaryOutlines("ModernSettingsScreen");
        RemoveSecondaryOutlines("ModernSkinsScreen");
        RemoveSecondaryOutlines("ModernMarketScreen");
        if (!ownsPrivacy) RemoveSecondaryOutlines("ModernPrivacyScreen");
    }

    private static void StyleSecondaryBackground(string screenName)
    {
        GameObject screen = FindSceneObject(screenName);
        if (screen == null)
        {
            return;
        }

        if (secondaryBackgroundSprite == null)
        {
            secondaryBackgroundSprite = Resources.Load<Sprite>("ModernUI/SecondaryGradientBackground");
        }

        Image background = screen.GetComponent<Image>();
        if (background == null) return;
        background.enabled = true;
        background.sprite = secondaryBackgroundSprite;
        background.type = Image.Type.Simple;
        background.preserveAspect = false;
        background.color = Color.white;

        UiVerticalGradient gradient = screen.GetComponent<UiVerticalGradient>();
        if (secondaryBackgroundSprite != null)
        {
            if (gradient != null) gradient.enabled = false;
        }
        else
        {
            // The optional background PNG is absent in some project versions.
            // Keep a real blue-to-mint surface instead of an untextured white image.
            if (gradient == null) gradient = screen.AddComponent<UiVerticalGradient>();
            gradient.Configure(new Color(.13f, .44f, .96f, 1f), new Color(.27f, .84f, .53f, 1f));
            gradient.enabled = true;
        }

        SetChildActive(screen.transform, "FigmaPattern", false);

        foreach (Transform child in screen.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name.Contains("TreeBubble"))
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private static void RemoveSecondaryOutlines(string screenName)
    {
        GameObject screen = FindSceneObject(screenName);
        if (screen == null)
        {
            return;
        }

        foreach (Outline outline in screen.GetComponentsInChildren<Outline>(true))
        {
            if (outline != null)
            {
                outline.enabled = false;
            }
        }
    }

    private static void StyleSecondaryTypography(string screenName)
    {
        GameObject screen = FindSceneObject(screenName);
        if (screen == null)
        {
            return;
        }

        foreach (TMP_Text text in screen.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null)
            {
                continue;
            }

            ApplySmoothLowPolyText(text, text.characterSpacing);
        }
    }

    private static void StyleSecondaryCoin(string objectName)
    {
        RectTransform coinCard = FindRect(objectName);
        TMP_Text coin = FindComponent<TMP_Text>(objectName);
        if (coinCard != null)
        {
            coinCard.anchorMin = new Vector2(0f, 1f);
            coinCard.anchorMax = new Vector2(0f, 1f);
            coinCard.pivot = new Vector2(0f, 1f);
            coinCard.sizeDelta = new Vector2(290f, 96f);
            coinCard.anchoredPosition = new Vector2(18f, -14f);
            EnsurePillBackground(
                coinCard,
                new Color(0.91f, 0.86f, 0.63f, 0.96f),
                new Color(1f, 1f, 1f, 0.03f));
            BalloonDogMenuArtwork.Apply(coinCard, "coin_panel_background");
            SetPillIcon(coinCard, GetCoinIconSprite(), Color.white, 44f, 34f);
        }

        if (coin == null)
        {
            return;
        }

        coin.text = BalloonDogEconomy.Coins.ToString("N0");
        coin.fontSize = 38f;
        coin.fontStyle = FontStyles.Normal;
        coin.fontWeight = FontWeight.Bold;
        coin.alignment = TextAlignmentOptions.MidlineLeft;
        coin.color = new Color(0.32f, 0.18f, 0.02f, 1f);
        coin.enableWordWrapping = false;
        ApplySmoothLowPolyText(coin, 1.2f);

        RectTransform labelRect = coin.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        coin.margin = new Vector4(76f, 0f, 18f, 0f);
    }

    private static void StyleSecondaryTopButton(string objectName, bool closeButton)
    {
        RectTransform rect = FindRect(objectName);
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-10f, -10f);
        rect.sizeDelta = new Vector2(112f, 112f);

        Color blue = new Color(0.24f, 0.50f, 0.94f, 1f);
        if (closeButton)
        {
            StyleButton(rect, blue, "X", 38f);
            Transform oldIcon = rect.Find("ThemeIcon");
            if (oldIcon != null)
            {
                oldIcon.gameObject.SetActive(false);
            }
        }
        else
        {
            StyleIconButton(rect, blue, GetGearIconSprite());
        }

        SoftenPillOutline(rect, 0.02f, 0.10f);
    }

    private static void StyleSettingsControls()
    {
        RectTransform privacy = FindRect("SettingsPrivacyButton");
        if (privacy != null)
        {
            privacy.sizeDelta = new Vector2(700f, 104f);
            StyleButton(privacy, new Color(0.35f, 0.84f, 0.23f, 1f), "PRIVACY NOTICE", 28f);
        }

        StyleToggleButton("SFXToggle");
        StyleToggleButton("VIBRATIONToggle");
    }

    private static void StyleToggleButton(string objectName)
    {
        RectTransform rect = FindRect(objectName);
        TMP_Text label = FindComponent<TMP_Text>(objectName);
        if (rect == null || label == null)
        {
            return;
        }

        bool enabled = label.text == "ON";
        rect.sizeDelta = new Vector2(190f, 76f);
        StyleButton(
            rect,
            enabled
                ? new Color(0.35f, 0.84f, 0.23f, 1f)
                : new Color(0.08f, 0.31f, 0.46f, 1f),
            enabled ? "ON" : "OFF",
            23f);
    }

    private static void StyleSkinControls()
    {
        GameObject screen = FindSceneObject("ModernSkinsScreen");
        if (screen == null)
        {
            return;
        }

        foreach (Button button in screen.GetComponentsInChildren<Button>(true))
        {
            if (button == null || button.name != "SkinAction")
            {
                continue;
            }

            RectTransform rect = button.transform as RectTransform;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (rect == null || label == null)
            {
                continue;
            }

            string value = label.text;
            Color fill;
            if (value == "EQUIPPED")
            {
                fill = new Color(0.22f, 0.68f, 0.31f, 1f);
            }
            else if (value == "EQUIP")
            {
                fill = new Color(0.18f, 0.54f, 0.96f, 1f);
            }
            else
            {
                fill = new Color(0.08f, 0.31f, 0.46f, 1f);
            }

            rect.sizeDelta = new Vector2(310f, 80f);
            StyleButton(rect, fill, value, 25f);
        }
    }

    private static void StyleMarketExtras()
    {
        GameObject extrasPanel = FindSceneObject("MarketExtrasPanel");
        if (extrasPanel == null)
        {
            return;
        }

        foreach (Button button in extrasPanel.GetComponentsInChildren<Button>(true))
        {
            if (button == null || button.name != "StoreOfferButton")
            {
                continue;
            }

            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                StyleButton(
                    rect,
                    new Color(0.18f, 0.54f, 0.96f, 1f),
                    "COMING SOON",
                    22f);
            }
        }
    }

    private static void StyleNavigationButton(
        string objectName,
        string label,
        Vector2 position,
        Vector2 size)
    {
        RectTransform rect = FindRect(objectName);
        if (rect == null)
        {
            return;
        }

        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        StyleButton(rect, new Color(0.12f, 0.48f, 0.94f, 1f), label, 28f);

        TMP_Text text = rect.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = 15f;
            text.fontSizeMax = 28f;
            text.overflowMode = TextOverflowModes.Overflow;
            text.characterSpacing = -0.5f;
            text.margin = new Vector4(10f, 4f, 10f, 6f);
        }

        Transform oldIcon = rect.Find("ThemeIcon");
        if (oldIcon != null)
        {
            oldIcon.gameObject.SetActive(false);
        }
    }

    private void FixBestPill()
    {
        RectTransform bestCard = FindRect("MainBest");
        TMP_Text best = FindComponent<TMP_Text>("MainBest");
        if (best == null)
        {
            best = FindComponent<TMP_Text>("BestScoreText");
        }

        if (bestCard != null)
        {
            bestCard.sizeDelta = new Vector2(300f, 82f);
            bestCard.anchorMin = bestCard.anchorMax = new Vector2(.5f, 1f);
            bestCard.anchoredPosition = new Vector2(0f, -700f);
            EnsurePillBackground(bestCard, new Color(0.24f, 0.50f, 0.94f, 0.96f), new Color(1f, 1f, 1f, 0.05f));
            SoftenPillOutline(bestCard, 0.02f, 0.08f);
            BalloonDogMenuArtwork.Apply(bestCard, "best_button");
        }

        if (best == null)
        {
            return;
        }

        best.text = "BEST  " + GameManager.BestScore.ToString("N0");
        best.fontSize = 28f;
        best.fontStyle = FontStyles.Normal;
        best.fontWeight = FontWeight.Bold;
        best.alignment = TextAlignmentOptions.Center;
        best.color = Color.white;
        best.enableWordWrapping = false;
        ApplySmoothLowPolyText(best, 1.2f);
        RectTransform labelRect = best.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        best.margin = new Vector4(16f, 0f, 16f, 0f);
    }

    private void FixLevelSelector()
    {
        TMP_Text level = FindComponent<TMP_Text>("LevelLabel");
        if (level != null)
        {
            level.text = "ENDLESS";
            level.fontSize = 24f;
            level.fontStyle = FontStyles.Normal;
            level.fontWeight = FontWeight.Bold;
            level.alignment = TextAlignmentOptions.Center;
            level.color = Color.white;
            level.enableWordWrapping = false;
            ApplySmoothLowPolyText(level, 0.12f);
            level.rectTransform.anchoredPosition = new Vector2(0f, 505f);
            level.rectTransform.sizeDelta = new Vector2(230f, 64f);
            EnsurePillBackground(level.rectTransform, new Color(0.18f, 0.54f, 0.96f, 0.98f), new Color(1f, 1f, 1f, 0.15f));
        }

        GameObject previous = FindSceneObject("PreviousLevel");
        if (previous != null)
        {
            previous.SetActive(false);
        }

        GameObject next = FindSceneObject("NextLevel");
        if (next != null)
        {
            next.SetActive(false);
        }
    }

    private void FixGameplayHud()
    {
        TMP_Text progress = FindComponent<TMP_Text>("ProgressLabel");
        if (progress != null)
        {
            progress.text = progress.text.Replace("\n", "  ").Trim();
            progress.fontSize = 22f;
            progress.fontStyle = FontStyles.Normal;
            progress.fontWeight = FontWeight.Regular;
            progress.color = Color.white;
            progress.alignment = TextAlignmentOptions.Center;
            progress.enableWordWrapping = false;
            ApplySmoothLowPolyText(progress, 0.15f);
        }

        TMP_Text score = FindComponent<TMP_Text>("ScoreText");
        if (score != null)
        {
            score.fontSize = 40f;
            score.fontStyle = FontStyles.Normal;
            score.fontWeight = FontWeight.Regular;
            score.color = Color.white;
            score.enableWordWrapping = false;
            ApplySmoothLowPolyText(score, 0f);
        }

        RectTransform pause = FindRect("ModernPauseButton");
        if (pause != null)
        {
            StyleButton(pause, new Color(0.18f, 0.65f, 0.96f, 0.99f), "II", 34f);
        }

        TMP_Text skin = FindComponent<TMP_Text>("GameplaySkin");
        if (skin != null)
        {
            skin.text = "ENDLESS";
            skin.fontSize = 24f;
            skin.fontStyle = FontStyles.Normal;
            skin.fontWeight = FontWeight.Regular;
            skin.color = Color.white;
            skin.alignment = TextAlignmentOptions.Center;
            skin.enableWordWrapping = false;
            ApplySmoothLowPolyText(skin, 0.1f);
        }
    }

    private void FixResultScreen()
    {
        // The bonus layout already places NO THANKS below the reward button.
        // Legacy result styling would move it over the bonus and relabel it HOME.
        if (FindRect("ResultDoubleRewardButton") != null)
        {
            return;
        }

        TMP_Text title = FindComponent<TMP_Text>("ResultTitle");
        if (title != null)
        {
            title.fontSize = 64f;
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.white;
            title.enableWordWrapping = false;
            ApplySmoothLowPolyText(title, 0.1f);
        }

        TMP_Text reason = FindComponent<TMP_Text>("ResultReason");
        if (reason != null)
        {
            reason.fontSize = 24f;
            reason.alignment = TextAlignmentOptions.Center;
            reason.color = new Color(0.88f, 0.84f, 0.96f, 1f);
            reason.enableWordWrapping = false;
            ApplySmoothLowPolyText(reason, 0f);
            reason.rectTransform.sizeDelta = new Vector2(760f, 70f);
        }

        StyleResultValue("ScoreCaption", 24f, new Color(1f, 0.82f, 0.16f, 1f), new Vector2(0f, 525f), new Vector2(420f, 55f));
        StyleResultValue("ResultScore", 62f, Color.white, new Vector2(0f, 430f), new Vector2(760f, 110f));
        StyleResultValue("TokenCaption", 24f, new Color(1f, 0.82f, 0.16f, 1f), new Vector2(0f, 175f), new Vector2(420f, 55f));
        StyleResultValue("ResultReward", 54f, new Color(1f, 0.93f, 0.34f, 1f), new Vector2(0f, 85f), new Vector2(760f, 100f));
        StyleResultValue("BestCaption", 24f, new Color(1f, 0.82f, 0.16f, 1f), new Vector2(0f, -160f), new Vector2(420f, 55f));
        StyleResultValue("ResultBest", 54f, Color.white, new Vector2(0f, -250f), new Vector2(760f, 100f));
        StyleResultValue("ResultHint", 18f, new Color(0.82f, 0.76f, 0.94f, 1f), new Vector2(0f, -1010f), new Vector2(720f, 40f));

        RectTransform restart = FindRect("ModernRestartButton");
        if (restart != null)
        {
            restart.anchoredPosition = new Vector2(0f, -665f);
            restart.sizeDelta = new Vector2(790f, 138f);
            StyleButton(restart, new Color(0.35f, 0.82f, 0.24f, 0.99f), "PLAY AGAIN", 34f);
        }

        RectTransform menu = FindRect("ModernResultMenuButton");
        if (menu != null)
        {
            menu.anchoredPosition = new Vector2(0f, -845f);
            menu.sizeDelta = new Vector2(460f, 72f);
            StyleButton(menu, new Color(0.16f, 0.50f, 0.94f, 0.99f), "HOME", 22f);
        }
    }

    private static void StyleResultValue(string objectName, float fontSize, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        TMP_Text text = FindComponent<TMP_Text>(objectName);
        if (text == null)
        {
            return;
        }

        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Normal;
        text.fontWeight = FontWeight.Regular;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        ApplySmoothLowPolyText(text, 0f);
        text.rectTransform.anchoredPosition = anchoredPosition;
        text.rectTransform.sizeDelta = size;
    }


    private static TMP_FontAsset ResolveUiFont()
    {
        return BalloonDogTitanFont.Get();
    }

    private static void ApplySmoothLowPolyText(TMP_Text text, float characterSpacing = 0f)
    {
        if (text == null)
        {
            return;
        }

        // Case labels are created with Titan One, including cards spawned while spinning.
        // The recurring legacy theme pass must not replace their font or material.
        if (text.GetComponentInParent<BalloonDogCaseOpening>(true) != null)
        {
            BalloonDogTitanFont.Apply(text);
            return;
        }

        TMP_FontAsset uiFont = ResolveUiFont();
        if (uiFont != null)
        {
            text.font = uiFont;
        }

        text.fontStyle = FontStyles.Normal;
        text.fontWeight = FontWeight.Regular;
        text.characterSpacing = characterSpacing;
        text.wordSpacing = 0f;
        text.lineSpacing = 0f;

        if (!PolishedTextIds.Add(text.GetEntityId()))
        {
            return;
        }

        Material sourceMaterial = text.fontSharedMaterial != null ? text.fontSharedMaterial : text.fontMaterial;
        if (sourceMaterial == null)
        {
            return;
        }

        Material runtimeMaterial = new Material(sourceMaterial);
        text.fontMaterial = runtimeMaterial;
        runtimeMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.10f);
        runtimeMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.06f);
        runtimeMaterial.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0.14f);
        runtimeMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.08f, 0.14f, 0.30f, 0.30f));
        runtimeMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.0f);
        runtimeMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        runtimeMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
    }

    private void RemoveOld3DLetterObjects()
    {
        string[] oldNames =
        {
            "MainTitleBalloonWord",
            "MenuTitleWord",
            "TitleWord",
            "BalloonUiCameraAnchor",
            "BalloonWordAnchor"
        };

        foreach (string oldName in oldNames)
        {
            GameObject old = GameObject.Find(oldName);
            if (old != null)
            {
                Destroy(old);
            }
        }
    }

    private static void HideTinyChildImages(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image == null || image.gameObject == root)
            {
                continue;
            }

            RectTransform rect = image.rectTransform;
            float w = rect.rect.width > 0f ? rect.rect.width : rect.sizeDelta.x;
            float h = rect.rect.height > 0f ? rect.rect.height : rect.sizeDelta.y;
            if (w <= 36f && h <= 36f)
            {
                image.enabled = false;
            }
        }
    }

    /// <summary>Reuses the actual menu surface styling for authored, noninteractive artwork.</summary>
    public static void StyleAuthoredMenu(Transform root)
    {
        Image background = root.GetComponent<Image>();
        Sprite sky = Resources.Load<Sprite>("MenuRefresh/MenuCloudSky");
        if (background != null && sky != null)
        {
            background.sprite = sky;
            background.color = Color.white;
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
        }
        foreach (string name in new[] { "Market", "Skins", "Play", "Best", "Settings", "Coins" })
        {
            RectTransform rect = root.Find(name) as RectTransform;
            if (rect == null) continue;
            Image image = rect.GetComponent<Image>();
            image.sprite = GetRoundedUiSprite();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.color = name == "Play" ? new Color(.35f, .84f, .23f, 1f) :
                name == "Coins" ? new Color(.91f, .86f, .63f, .96f) :
                new Color(.18f, .54f, .96f, 1f);
            if (name == "Market" || name == "Skins" || name == "Play")
            {
                BalloonDogMenuArtwork.Apply(rect, name == "Play" ? "butonyesil" : "butonmavi");
                if (name == "Play")
                {
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
                    rect.anchoredPosition = new Vector2(0f, 180f);
                    rect.sizeDelta = new Vector2(890f, 270f);
                }
                if (name != "Play")
                {
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
                    GetMainNavigationLayout(root, out Vector2 size, out float offset);
                    rect.anchoredPosition = new Vector2(name == "Market" ? -offset : offset, 450f);
                    rect.sizeDelta = size;
                    string iconName = name + "Icon";
                    Transform icon = rect.Find(iconName) ?? root.Find(iconName);
                    if (icon == null)
                    {
                        icon = new GameObject(iconName, typeof(RectTransform), typeof(Image)).transform;
                    }
                    icon.SetParent(rect, false);
                    Image iconImage = icon.GetComponent<Image>();
                    iconImage.sprite = name == "Market" ? GetMarketIconSprite() : GetSkinsIconSprite();
                    iconImage.preserveAspect = true;
                    iconImage.raycastTarget = false;
                    RectTransform iconRect = (RectTransform)icon;
                    iconRect.anchorMin = iconRect.anchorMax = new Vector2(.5f, .5f);
                    iconRect.anchoredPosition = new Vector2(-size.x * .5f + 86f, 6f);
                    iconRect.sizeDelta = new Vector2(92f, 92f);
                }
                Transform child = rect.Find(name + "_Label");
                if (child != null)
                {
                    Text label = child.GetComponent<Text>();
                    label.fontSize = name == "Play" ? 88 : 68;
                    RectTransform textRect = label.rectTransform;
                    textRect.sizeDelta = rect.sizeDelta;
                    if (name != "Play")
                    {
                        textRect.anchoredPosition = new Vector2(62f, 6f);
                        textRect.sizeDelta = new Vector2(rect.sizeDelta.x - 164f, 180f);
                    }
                    child.SetAsLastSibling();
                }
            }
        }
        RectTransform logo = root.Find("BalloonDogLogo") as RectTransform;
        if (logo != null)
        {
            logo.anchorMin = logo.anchorMax = new Vector2(.5f, 1f);
            logo.pivot = new Vector2(.5f, 1f);
            logo.anchoredPosition = new Vector2(0f, -185f);
            BalloonDogMenuArtwork.Apply(logo, "balloon_dog_logo");
        }
        Font titan = Resources.Load<Font>("Fonts/TitanOne/TitanOne-Regular");
        foreach (Text text in root.GetComponentsInChildren<Text>(true))
        {
            if (titan != null) text.font = titan;
            text.fontStyle = FontStyle.Normal;
        }
        RectTransform best = root.Find("Best") as RectTransform;
        if (best != null)
        {
            best.anchorMin = best.anchorMax = new Vector2(.5f, 1f);
            best.anchoredPosition = new Vector2(0f, -700f);
            BalloonDogMenuArtwork.Apply(best, "best_button");
        }
        RectTransform coins = root.Find("Coins") as RectTransform;
        if (coins != null)
        {
            coins.anchorMin = coins.anchorMax = new Vector2(0f, 1f);
            coins.pivot = new Vector2(0f, 1f);
            coins.anchoredPosition = new Vector2(18f, -14f);
            coins.sizeDelta = new Vector2(290f, 96f);
            BalloonDogMenuArtwork.Apply(coins, "coin_panel_background");
            RectTransform label = root.Find("CoinsLabel") as RectTransform;
            RectTransform icon = root.Find("CoinIcon") as RectTransform;
            // Also find the elements after a previous, idempotent styling pass.
            if (label == null) label = coins.Find("CoinsLabel") as RectTransform;
            if (icon == null) icon = coins.Find("CoinIcon") as RectTransform;
            if (label != null)
            {
                label.SetParent(coins, false);
                label.anchorMin = label.anchorMax = new Vector2(.5f, .5f);
                label.anchoredPosition = new Vector2(22f, 0f);
                label.sizeDelta = new Vector2(170f, 70f);
                label.GetComponent<Text>().fontSize = 38;
            }
            if (icon != null)
            {
                icon.SetParent(coins, false);
                icon.anchorMin = icon.anchorMax = new Vector2(0f, .5f);
                icon.anchoredPosition = new Vector2(42f, 0f);
                icon.sizeDelta = new Vector2(44f, 44f);
            }
        }
        RectTransform settings = root.Find("Settings") as RectTransform;
        RectTransform settingsIcon = root.Find("SettingsIcon") as RectTransform;
        if (settings != null)
        {
            settings.anchorMin = settings.anchorMax = Vector2.one;
            settings.pivot = Vector2.one;
            settings.anchoredPosition = new Vector2(-10f, -10f);
            settings.sizeDelta = new Vector2(112f, 112f);
            BalloonDogMenuArtwork.Apply(settings, "butonmavi");
        }
        if (settingsIcon != null)
        {
            settingsIcon.SetParent(settings, false);
            settingsIcon.anchorMin = settingsIcon.anchorMax = new Vector2(.5f, .5f);
            settingsIcon.anchoredPosition = Vector2.zero;
            settingsIcon.sizeDelta = new Vector2(60f, 60f);
            settingsIcon.GetComponent<Image>().sprite = GetGearIconSprite();
            settingsIcon.gameObject.SetActive(true);
        }
        RectTransform level = root.Find("Level") as RectTransform;
        if (level != null)
        {
            level.anchorMin = level.anchorMax = new Vector2(.5f, 1f);
            level.anchoredPosition = new Vector2(0f, -42f);
        }
    }

    private static void StyleButton(RectTransform rect, Color fill, string labelValue, float fontSize)
    {
        if (rect == null)
        {
            return;
        }

        Image image = rect.GetComponent<Image>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }

        image.sprite = GetRoundedUiSprite();
        image.type = Image.Type.Sliced;
        image.color = fill;
        image.raycastTarget = true;

        Button button = rect.GetComponent<Button>();
        if (button != null)
        {
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.98f);
            colors.pressedColor = new Color(0.92f, 0.92f, 0.95f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        EnsureSurfaceChrome(rect);

        TMP_Text label = rect.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            GameObject labelObject = new GameObject("ThemeLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(rect, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            Stretch(labelRect);
            label = labelObject.GetComponent<TextMeshProUGUI>();
        }

        label.enabled = true;
        label.text = labelValue;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = Mathf.Min(14f, fontSize);
        label.fontSizeMax = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.characterSpacing = 0f;
        label.margin = new Vector4(10f, 5f, 10f, 7f);
        ApplySmoothLowPolyText(label, 0f);
        BalloonDogMenuArtwork.ApplyButtonColor(image, fill);
    }

    private static void SetButtonLabelTuning(RectTransform rect, float characterSpacing, Vector4 margin)
    {
        if (rect == null)
        {
            return;
        }

        TMP_Text label = rect.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            return;
        }

        label.characterSpacing = characterSpacing;
        label.margin = margin;
    }

    private static void StyleIconButton(RectTransform rect, Color fill, Sprite iconSprite)
    {
        StyleButton(rect, fill, string.Empty, 10f);
        TMP_Text label = rect.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = string.Empty;
            label.enabled = false;
            label.gameObject.SetActive(false);
        }

        SetButtonIcon(rect, iconSprite, 52f, true);
    }

    private static void SetButtonIcon(RectTransform rect, Sprite iconSprite, float size = 28f, bool centered = false)
    {
        if (rect == null)
        {
            return;
        }

        Transform existing = rect.Find("ThemeIcon");
        Image icon = null;
        if (iconSprite == null)
        {
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }

            TMP_Text text = rect.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.margin = new Vector4(18f, 10f, 18f, 10f);
            }
            return;
        }

        if (existing == null)
        {
            GameObject iconGo = new GameObject("ThemeIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(rect, false);
            icon = iconGo.GetComponent<Image>();
        }
        else
        {
            icon = existing.GetComponent<Image>();
        }

        if (icon == null)
        {
            return;
        }

        icon.gameObject.SetActive(true);
        icon.sprite = iconSprite;
        icon.color = Color.white;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f);
        iconRect.anchorMax = centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(size, size);
        iconRect.anchoredPosition = centered ? Vector2.zero : new Vector2(28f + size * 0.5f, 0f);
        icon.transform.SetAsLastSibling();

        TMP_Text label = rect.GetComponentInChildren<TMP_Text>(true);
        if (label != null && !centered)
        {
            label.margin = new Vector4(size + 42f, 10f, 16f, 10f);
        }
    }

    private static void SetPillIcon(RectTransform rect, Sprite iconSprite, Color color, float size, float xOffset)
    {
        if (rect == null)
        {
            return;
        }

        Transform existing = rect.Find("ThemePillIcon");
        Image icon;
        if (existing == null)
        {
            GameObject iconGo = new GameObject("ThemePillIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(rect, false);
            icon = iconGo.GetComponent<Image>();
        }
        else
        {
            icon = existing.GetComponent<Image>();
        }

        if (icon == null)
        {
            return;
        }

        icon.sprite = iconSprite;
        icon.color = color;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(size, size);
        iconRect.anchoredPosition = new Vector2(xOffset, 0f);
    }

    private static void EnsureSurfaceChrome(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        Outline outline = rect.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }

        Shadow shadow = rect.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = rect.gameObject.AddComponent<Shadow>();
        }
        shadow.enabled = true;
        shadow.effectColor = new Color(0.015f, 0.08f, 0.22f, 0.30f);
        shadow.effectDistance = new Vector2(0f, -7f);

        Image topGloss = EnsureOverlayImage(rect, "ThemeTopGloss", new Color(1f, 1f, 1f, 0.18f));
        RectTransform topRect = topGloss.rectTransform;
        topRect.anchorMin = new Vector2(0.06f, 0.58f);
        topRect.anchorMax = new Vector2(0.94f, 0.92f);
        topRect.offsetMin = Vector2.zero;
        topRect.offsetMax = Vector2.zero;

        Image bottomShade = EnsureOverlayImage(rect, "ThemeBottomShade", new Color(0.01f, 0.08f, 0.24f, 0.13f));
        RectTransform shadeRect = bottomShade.rectTransform;
        shadeRect.anchorMin = new Vector2(0.04f, 0.05f);
        shadeRect.anchorMax = new Vector2(0.96f, 0.42f);
        shadeRect.offsetMin = Vector2.zero;
        shadeRect.offsetMax = Vector2.zero;
    }

    private static void SoftenPillOutline(RectTransform rect, float outlineAlpha, float shadowAlpha)
    {
        if (rect == null)
        {
            return;
        }

        Outline outline = rect.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = new Color(1f, 1f, 1f, outlineAlpha);
            outline.effectDistance = new Vector2(1f, 1f);
        }

        Shadow shadow = rect.GetComponent<Shadow>();
        if (shadow != null)
        {
            shadow.effectColor = new Color(0f, 0f, 0f, shadowAlpha);
            shadow.effectDistance = new Vector2(0f, -3f);
        }
    }

    private static Image EnsureOverlayImage(RectTransform parent, string name, Color color)
    {
        Transform existing = parent.Find(name);
        Image image;
        if (existing == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            image = go.GetComponent<Image>();
        }
        else
        {
            image = existing.GetComponent<Image>();
        }

        image.sprite = GetRoundedUiSprite();
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        image.transform.SetAsFirstSibling();
        return image;
    }

    private static void EnsurePillBackground(RectTransform rect, Color fill, Color outlineColor)
    {
        if (rect == null || rect.gameObject == null || !rect.gameObject.scene.IsValid())
        {
            return;
        }

        GameObject owner = rect.gameObject;
        Image image = owner.GetComponent<Image>();
        if (image == null)
        {
            image = owner.AddComponent<Image>();
        }

        if (image == null)
        {
            return;
        }

        Sprite rounded = GetRoundedUiSprite();
        if (rounded != null)
        {
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
        }

        image.color = fill;
        image.raycastTarget = false;

        Outline outline = owner.GetComponent<Outline>();
        if (outline == null)
        {
            outline = owner.AddComponent<Outline>();
        }

        if (outline != null)
        {
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1f, 1f);
        }

        Shadow shadow = owner.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = owner.AddComponent<Shadow>();
        }

        if (shadow != null)
        {
            shadow.effectColor = new Color(0f, 0f, 0f, 0.15f);
            shadow.effectDistance = new Vector2(0f, -4f);
        }

        Image topGloss = EnsureOverlayImage(rect, "ThemeTopGloss", new Color(1f, 1f, 1f, 0.18f));
        RectTransform topRect = topGloss.rectTransform;
        topRect.anchorMin = new Vector2(0.04f, 0.54f);
        topRect.anchorMax = new Vector2(0.96f, 0.92f);
        topRect.offsetMin = Vector2.zero;
        topRect.offsetMax = Vector2.zero;
    }


    private static Sprite GetCoinIconSprite()
    {
        if (coinIconSprite != null)
        {
            return coinIconSprite;
        }

        Texture2D custom = Resources.Load<Texture2D>("CustomCoin");
        if (custom != null)
        {
            coinIconSprite = Sprite.Create(custom, new Rect(0f, 0f, custom.width, custom.height), new Vector2(0.5f, 0.5f), 100f);
            coinIconSprite.name = "BalloonDogCustomCoinIcon";
            return coinIconSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "BalloonDogCoinIcon";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color outer = new Color(0.92f, 0.64f, 0.05f, 1f);
        Color inner = new Color(1f, 0.85f, 0.20f, 1f);
        Color highlight = new Color(1f, 0.96f, 0.58f, 1f);
        Vector2 c = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float r = size * 0.42f;
        float r2 = size * 0.31f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                Color col = clear;
                if (d <= r) col = outer;
                if (d <= r2) col = inner;
                if (d <= r2 && y > c.y) col = Color.Lerp(inner, outer, 0.20f);
                if (d <= r2 * 0.58f && x < c.x && y > c.y * 0.9f) col = highlight;
                texture.SetPixel(x, y, col);
            }
        }
        texture.Apply();
        coinIconSprite = Sprite.Create(texture, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), 100f);
        coinIconSprite.name = "BalloonDogCoinIconSprite";
        return coinIconSprite;
    }

    private static Sprite GetGearIconSprite()
    {
        if (gearIconSprite != null)
        {
            return gearIconSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "BalloonDogGearIcon";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Vector2 c = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float innerHole = size * 0.12f;
        float baseRadius = size * 0.22f;
        float toothRadius = size * 0.30f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y) - c;
                float angle = Mathf.Atan2(p.y, p.x);
                float radial = p.magnitude;
                float teeth = 0.04f * Mathf.Cos(angle * 8f);
                float target = toothRadius + teeth * size;
                bool inCog = radial <= target && radial >= innerHole;
                texture.SetPixel(x, y, inCog ? Color.white : new Color(0f, 0f, 0f, 0f));
            }
        }
        texture.Apply();
        gearIconSprite = Sprite.Create(texture, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), 100f);
        gearIconSprite.name = "BalloonDogGearIconSprite";
        return gearIconSprite;
    }

    private static Sprite GetMarketIconSprite()
    {
        if (marketIconSprite == null)
            marketIconSprite = Resources.Load<Sprite>("CollectionUI/MarketStorefront");
        return marketIconSprite;
    }

    private static Sprite GetSkinsIconSprite()
    {
        if (skinsIconSprite == null)
            skinsIconSprite = Resources.Load<Sprite>("CollectionUI/SkinsShirt");
        return skinsIconSprite;
    }

    private static Sprite GetRoundedUiSprite()
    {
        if (roundedUiSprite != null)
        {
            return roundedUiSprite;
        }

        const int size = 128;
        const float radius = 42f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "BalloonDogRoundedUI";
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
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + 0.5f - distance) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        roundedUiSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(42f, 42f, 42f, 42f));
        roundedUiSprite.name = "BalloonDogRoundedUI";
        return roundedUiSprite;
    }

    private static RectTransform FindRect(string objectName)
    {
        GameObject go = FindSceneObject(objectName);
        return go != null ? go.GetComponent<RectTransform>() : null;
    }

    private static T FindComponent<T>(string objectName) where T : Component
    {
        GameObject go = FindSceneObject(objectName);
        if (go == null)
        {
            return null;
        }

        T direct = go.GetComponent<T>();
        if (direct != null)
        {
            return direct;
        }

        return go.GetComponentInChildren<T>(true);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform != null && transform.name == objectName && transform.gameObject.scene.IsValid())
            {
                return transform.gameObject;
            }
        }
        return null;
    }

    private static void SetChildActive(Transform parent, string childName, bool active)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
