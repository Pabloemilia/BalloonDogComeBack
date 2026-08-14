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
    private const string SpriteResourcePath = "BalloonLetterSprites";

    private float nextRefreshTime;
    private RectTransform titleRoot;
    private string builtTitle = string.Empty;
    private readonly Dictionary<char, Sprite> letterSprites = new Dictionary<char, Sprite>();
    private static Sprite roundedUiSprite;
    private static Sprite coinIconSprite;
    private static Sprite gearIconSprite;
    private static Sprite marketIconSprite;
    private static Sprite skinsIconSprite;
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
        LoadLetterSprites();
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
        titleRoot = null;
        builtTitle = string.Empty;
        PolishedTextIds.Clear();
        RemoveOld3DLetterObjects();
        LoadLetterSprites();
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
        FixGameplayHud();
        FixResultScreen();
    }

    private void StyleBackground(GameObject mainScreen)
    {
        Image image = mainScreen.GetComponent<Image>();
        if (image != null)
        {
            image.enabled = true;
            image.color = new Color(0.05f, 0.07f, 0.10f, 0.12f);
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
        if (titleRect == null)
        {
            return;
        }

        TMP_Text legacyText = titleRect.GetComponent<TMP_Text>();
        if (legacyText != null)
        {
            legacyText.text = string.Empty;
            legacyText.enabled = false;
        }

        if (titleRoot == null)
        {
            Transform existing = titleRect.Find("BalloonSpriteTitle");
            if (existing != null)
            {
                titleRoot = existing as RectTransform;
            }
            else
            {
                GameObject go = new GameObject("BalloonSpriteTitle", typeof(RectTransform));
                go.transform.SetParent(titleRect, false);
                titleRoot = go.GetComponent<RectTransform>();
                Stretch(titleRoot);
            }
        }

        if (letterSprites.Count == 0)
        {
            if (legacyText != null)
            {
                legacyText.enabled = true;
                legacyText.text = "BALLOON DOG";
                legacyText.fontSize = 80f;
                legacyText.fontStyle = FontStyles.Bold;
                legacyText.alignment = TextAlignmentOptions.Center;
                legacyText.color = Color.white;
            }

            if (titleRoot != null)
            {
                titleRoot.gameObject.SetActive(false);
            }
            return;
        }

        titleRoot.gameObject.SetActive(true);
        if (builtTitle != "BALLOON DOG")
        {
            BuildSpriteWord(titleRoot, "BALLOON DOG", 0.92f, 8f);
            builtTitle = "BALLOON DOG";
        }
    }

    private void BuildSpriteWord(RectTransform root, string text, float heightFill, float spacing)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }

        float targetHeight = Mathf.Max(1f, root.rect.height * heightFill);
        List<(Sprite sprite, float width)> parts = new List<(Sprite sprite, float width)>();
        float totalWidth = 0f;

        foreach (char rawChar in text.ToUpperInvariant())
        {
            if (rawChar == ' ')
            {
                float gap = targetHeight * 0.36f;
                parts.Add((null, gap));
                totalWidth += gap;
                continue;
            }

            if (!letterSprites.TryGetValue(rawChar, out Sprite sprite) || sprite == null)
            {
                float gap = targetHeight * 0.45f;
                parts.Add((null, gap));
                totalWidth += gap;
                continue;
            }

            float aspect = sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 0.75f;
            float width = targetHeight * Mathf.Clamp(aspect, 0.30f, 1.20f);
            parts.Add((sprite, width));
            totalWidth += width;
        }

        totalWidth += Mathf.Max(0, parts.Count - 1) * spacing;
        float scaleDown = totalWidth > root.rect.width && totalWidth > 0f ? root.rect.width / totalWidth : 1f;
        targetHeight *= scaleDown;
        spacing *= scaleDown;

        float finalWidth = 0f;
        foreach ((Sprite _, float originalWidth) in parts)
        {
            finalWidth += originalWidth * scaleDown;
        }
        finalWidth += Mathf.Max(0, parts.Count - 1) * spacing;

        float cursor = -finalWidth * 0.5f;
        for (int index = 0; index < parts.Count; index++)
        {
            Sprite sprite = parts[index].sprite;
            float width = parts[index].width * scaleDown;
            if (sprite != null)
            {
                GameObject letterObject = new GameObject("Letter_" + index, typeof(RectTransform), typeof(Image));
                letterObject.transform.SetParent(root, false);
                RectTransform rect = letterObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(width, targetHeight);
                rect.anchoredPosition = new Vector2(cursor + width * 0.5f, 0f);

                Image image = letterObject.GetComponent<Image>();
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;

                BalloonDogFloatingLetter floatAnim = letterObject.AddComponent<BalloonDogFloatingLetter>();
                // Keep the playful balloon motion subtle. The old 10-18 px
                // amplitude could pull an O far away from the rest of the word.
                floatAnim.Configure(
                    index,
                    Mathf.Lerp(2.5f, 4.5f, Random.value),
                    Mathf.Lerp(0.65f, 0.95f, Random.value),
                    Random.Range(0f, 6.28318f));
            }

            cursor += width + spacing;
        }
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
            play.anchoredPosition = new Vector2(0f, -920f);
            play.sizeDelta = new Vector2(890f, 190f);
            StyleButton(play, new Color(0.35f, 0.84f, 0.23f, 1f), "PLAY", 88f);
            SetButtonLabelTuning(play, 16f, new Vector4(8f, 2f, 8f, 4f));
            SetButtonIcon(play, null);
        }

        RectTransform market = FindRect("MarketNavButton");
        if (market != null)
        {
            market.anchoredPosition = new Vector2(-310f, -650f);
            market.sizeDelta = new Vector2(460f, 164f);
            StyleButton(market, new Color(0.18f, 0.54f, 0.96f, 1f), "MARKET", 60f);
            SetButtonLabelTuning(market, 6f, new Vector4(10f, 4f, 10f, 6f));
            SetButtonIcon(market, GetMarketIconSprite());
        }

        RectTransform skins = FindRect("SkinsNavButton");
        if (skins != null)
        {
            skins.anchoredPosition = new Vector2(310f, -650f);
            skins.sizeDelta = new Vector2(460f, 164f);
            StyleButton(skins, new Color(0.18f, 0.54f, 0.96f, 1f), "SKINS", 60f);
            SetButtonLabelTuning(skins, 6f, new Vector4(10f, 4f, 10f, 6f));
            SetButtonIcon(skins, GetSkinsIconSprite());
        }
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
            coinCard.sizeDelta = new Vector2(258f, 86f);
            coinCard.anchoredPosition = new Vector2(18f, -14f);
            EnsurePillBackground(coinCard, new Color(0.91f, 0.86f, 0.63f, 0.96f), new Color(1f, 1f, 1f, 0.03f));
            SoftenPillOutline(coinCard, 0.015f, 0.06f);
            SetPillIcon(coinCard, GetCoinIconSprite(), Color.white, 44f, 34f);
        }

        if (coin != null)
        {
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

        RectTransform settings = FindRect("MainSettingsButton");
        if (settings != null)
        {
            settings.anchorMin = new Vector2(1f, 1f);
            settings.anchorMax = new Vector2(1f, 1f);
            settings.pivot = new Vector2(1f, 1f);
            settings.anchoredPosition = new Vector2(-10f, -10f);
            settings.sizeDelta = new Vector2(112f, 112f);
            StyleIconButton(settings, new Color(0.24f, 0.50f, 0.94f, 1f), GetGearIconSprite());
            SoftenPillOutline(settings, 0.02f, 0.10f);
        }
    }

    private void FixSecondaryScreens()
    {
        StyleSecondaryTypography("ModernSettingsScreen");
        StyleSecondaryTypography("ModernSkinsScreen");
        StyleSecondaryTypography("ModernMarketScreen");

        StyleSecondaryCoin("SettingsTokens");
        StyleSecondaryCoin("SkinsTokens");
        StyleSecondaryCoin("MarketTokens");

        StyleSecondaryTopButton("SettingsTopButton", true);
        StyleSecondaryTopButton("SkinsTopButton", false);
        StyleSecondaryTopButton("MarketTopButton", false);

        StyleSettingsControls();
        StyleSkinControls();
        StyleMarketTabs();

        StyleNavigationButton("SettingsClose", "DONE", new Vector2(0f, -1035f), new Vector2(330f, 100f));
        StyleNavigationButton("SkinsMarket", "MARKET", new Vector2(-190f, -1035f), new Vector2(300f, 100f));
        StyleNavigationButton("SkinsClose", "HOME", new Vector2(190f, -1035f), new Vector2(300f, 100f));
        StyleNavigationButton("MarketClose", "HOME", new Vector2(-190f, -1035f), new Vector2(300f, 100f));
        StyleNavigationButton("MarketSkins", "COLLECTION", new Vector2(190f, -1035f), new Vector2(380f, 100f));
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
            coinCard.sizeDelta = new Vector2(258f, 86f);
            coinCard.anchoredPosition = new Vector2(18f, -14f);
            EnsurePillBackground(
                coinCard,
                new Color(0.91f, 0.86f, 0.63f, 0.96f),
                new Color(1f, 1f, 1f, 0.03f));
            SoftenPillOutline(coinCard, 0.015f, 0.06f);
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

    private static void StyleMarketTabs()
    {
        GameObject skinsPanel = FindSceneObject("MarketSkinOffersPanel");
        bool skinsActive = skinsPanel != null && skinsPanel.activeSelf;

        RectTransform skinsTab = FindRect("MarketSkinsTabButton");
        if (skinsTab != null)
        {
            StyleButton(
                skinsTab,
                skinsActive
                    ? new Color(0.35f, 0.84f, 0.23f, 1f)
                    : new Color(0.18f, 0.54f, 0.96f, 1f),
                "SKINS",
                29f);
            SoftenPillOutline(skinsTab, 0.04f, 0.12f);
        }

        RectTransform extrasTab = FindRect("MarketExtrasTabButton");
        if (extrasTab != null)
        {
            StyleButton(
                extrasTab,
                skinsActive
                    ? new Color(0.18f, 0.54f, 0.96f, 1f)
                    : new Color(0.35f, 0.84f, 0.23f, 1f),
                "EXTRAS",
                29f);
            SoftenPillOutline(extrasTab, 0.04f, 0.12f);
        }

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
        StyleButton(rect, new Color(0.18f, 0.54f, 0.96f, 1f), label, 27f);
        SoftenPillOutline(rect, 0.035f, 0.12f);

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
            bestCard.anchoredPosition = new Vector2(0f, 585f);
            EnsurePillBackground(bestCard, new Color(0.24f, 0.50f, 0.94f, 0.96f), new Color(1f, 1f, 1f, 0.05f));
            SoftenPillOutline(bestCard, 0.02f, 0.08f);
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
            level.text = "LEVEL " + Mathf.Max(1, BalloonDogCampaign.CurrentLevel);
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
            skin.text = "LEVEL " + Mathf.Max(1, BalloonDogCampaign.CurrentLevel);
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

        RectTransform nextLevel = FindRect("CampaignNextLevelButton");
        if (nextLevel != null)
        {
            nextLevel.anchoredPosition = new Vector2(0f, -665f);
            nextLevel.sizeDelta = new Vector2(790f, 138f);
            string label = BalloonDogCampaign.CurrentLevel >= BalloonDogCampaign.LevelCount ? "PLAY AGAIN" : "NEXT LEVEL";
            StyleButton(nextLevel, new Color(0.35f, 0.82f, 0.24f, 0.99f), label, 34f);
        }

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
        foreach (TMP_FontAsset asset in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (asset == null)
            {
                continue;
            }

            if (asset.name.Contains("Audex"))
            {
                return asset;
            }
        }

        foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
        {
            if (font == null)
            {
                continue;
            }

            if (!font.name.Contains("Audex"))
            {
                continue;
            }

            try
            {
                TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                created.name = "Audex Runtime SDF";
                return created;
            }
            catch
            {
            }
        }

        foreach (TMP_FontAsset asset in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (asset == null)
            {
                continue;
            }

            if (asset.name.Contains("LiberationSans SDF"))
            {
                return asset;
            }
        }

        return null;
    }

    private static void ApplySmoothLowPolyText(TMP_Text text, float characterSpacing = 0f)
    {
        if (text == null)
        {
            return;
        }

        TMP_FontAsset uiFont = ResolveUiFont();
        if (uiFont != null)
        {
            text.font = uiFont;
        }

        text.fontStyle = FontStyles.Normal;
        text.fontWeight = FontWeight.Bold;
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

    private void LoadLetterSprites()
    {
        letterSprites.Clear();
        Sprite[] sprites = Resources.LoadAll<Sprite>(SpriteResourcePath);
        foreach (Sprite sprite in sprites)
        {
            if (sprite == null)
            {
                continue;
            }

            string normalized = sprite.name.ToUpperInvariant();
            char found = '\0';
            for (char c = 'A'; c <= 'Z'; c++)
            {
                if (normalized == c.ToString() || normalized.EndsWith("_" + c) || normalized.EndsWith("-" + c))
                {
                    found = c;
                    break;
                }
            }

            if (found == '\0')
            {
                for (char c = '0'; c <= '9'; c++)
                {
                    if (normalized == c.ToString() || normalized.EndsWith("_" + c) || normalized.EndsWith("-" + c))
                    {
                        found = c;
                        break;
                    }
                }
            }

            if (found != '\0')
            {
                letterSprites[found] = sprite;
            }
        }
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
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        label.enableWordWrapping = false;
        label.characterSpacing = 0f;
        label.margin = new Vector4(12f, 6f, 12f, 8f);
        ApplySmoothLowPolyText(label, 0f);
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
        if (outline == null)
        {
            outline = rect.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(1f, 1f, 1f, 0.15f);
        outline.effectDistance = new Vector2(1f, 1f);

        Shadow shadow = rect.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = rect.gameObject.AddComponent<Shadow>();
        }
        shadow.effectColor = new Color(0f, 0f, 0f, 0.20f);
        shadow.effectDistance = new Vector2(0f, -4f);

        Image topGloss = EnsureOverlayImage(rect, "ThemeTopGloss", new Color(1f, 1f, 1f, 0.14f));
        RectTransform topRect = topGloss.rectTransform;
        topRect.anchorMin = new Vector2(0.04f, 0.52f);
        topRect.anchorMax = new Vector2(0.96f, 0.95f);
        topRect.offsetMin = Vector2.zero;
        topRect.offsetMax = Vector2.zero;

        Image bottomShade = EnsureOverlayImage(rect, "ThemeBottomShade", new Color(0f, 0f, 0f, 0.08f));
        RectTransform shadeRect = bottomShade.rectTransform;
        shadeRect.anchorMin = new Vector2(0.03f, 0.05f);
        shadeRect.anchorMax = new Vector2(0.97f, 0.40f);
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
        if (marketIconSprite != null)
        {
            return marketIconSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "BalloonDogMarketIcon";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        Color clear = new Color(0f,0f,0f,0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }
        void DrawRect(int x0, int y0, int x1, int y1) { for(int y=y0;y<=y1;y++) for(int x=x0;x<=x1;x++) if(x>=0&&x<size&&y>=0&&y<size) texture.SetPixel(x,y,Color.white); }
        void DrawCircle(int cx,int cy,int r){ for(int y=cy-r;y<=cy+r;y++) for(int x=cx-r;x<=cx+r;x++) if(x>=0&&x<size&&y>=0&&y<size && (x-cx)*(x-cx)+(y-cy)*(y-cy)<=r*r) texture.SetPixel(x,y,Color.white);}
        DrawRect(16, 26, 40, 30); DrawRect(20, 22, 36, 34); DrawRect(14, 34, 18, 38); DrawRect(36, 34, 40, 38); DrawRect(12, 18, 18, 22); DrawRect(18, 18, 20, 20); DrawCircle(20, 18, 5); DrawCircle(36, 18, 5);
        texture.Apply();
        marketIconSprite = Sprite.Create(texture, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), 100f);
        marketIconSprite.name = "BalloonDogMarketIconSprite";
        return marketIconSprite;
    }

    private static Sprite GetSkinsIconSprite()
    {
        if (skinsIconSprite != null)
        {
            return skinsIconSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "BalloonDogSkinsIcon";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        Color clear = new Color(0f,0f,0f,0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }
        for (int y = 12; y < 44; y++)
        {
            for (int x = 16; x < 48; x++)
            {
                bool fill = false;
                if (y >= 20 && x >= 22 && x <= 42) fill = true;
                if (y >= 12 && y < 22 && ((x >= 16 && x <= 24) || (x >= 40 && x <= 48) || (x >= 24 && x <= 40))) fill = true;
                if (y >= 22 && y < 44 && x >= 24 && x <= 40) fill = true;
                if (fill) texture.SetPixel(x, y, Color.white);
            }
        }
        texture.Apply();
        skinsIconSprite = Sprite.Create(texture, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), 100f);
        skinsIconSprite.name = "BalloonDogSkinsIconSprite";
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
