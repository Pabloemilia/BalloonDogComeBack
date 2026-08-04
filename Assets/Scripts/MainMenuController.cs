using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    private const string SoundPreferenceKey = "BalloonDog.SoundEnabled";
    private const string VibrationPreferenceKey = "BalloonDog.VibrationEnabled";

    public static bool StartImmediatelyOnNextSceneLoad { get; set; }
    public static bool VibrationEnabled { get; private set; } = true;

    private GameObject mainMenuPanel;
    private GameObject settingsPanel;
    private PlayerRunner runner;
    private PlayerHorizontalController horizontalController;
    private PlayerFormController formController;
    private BalloonSizeController sizeController;
    private AirController airController;
    private Rigidbody playerBody;

    private TMP_Text soundButtonLabel;
    private TMP_Text vibrationButtonLabel;
    private bool soundEnabled;
    private StartCountdownController countdownController;

    private static Sprite cachedCircleSprite;
    private static Sprite cachedRoundedSprite;
    private static Sprite cachedShrinkIconSprite;

    private void Awake()
    {
        Time.timeScale = 0f;
        ResolveReferences();
        ApplyBalloonMenuTheme();
        BindButtons();
        LoadPreferences();
        StartCoroutine(RefreshThemeAfterSceneSetup());
    }

    private IEnumerator RefreshThemeAfterSceneSetup()
    {
        // Bootstrap bazı UI nesnelerini Start sonrasında üretebildiği için
        // iki kare bekleyip temayı ve yerleşimi yeniden uygula.
        yield return null;
        yield return null;

        ResolveReferences();
        ApplyBalloonMenuTheme();
        BindButtons();
    }

    private void Start()
    {
        if (StartImmediatelyOnNextSceneLoad)
        {
            StartImmediatelyOnNextSceneLoad = false;
            StartGame();
            return;
        }

        ShowMainMenu();
    }

    public void StartGame()
    {
        airController?.ResetToFull();
        sizeController?.SnapToCurrentAir();

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        SetGameplayUiVisible(true);
        SetPlayerControlsEnabled(false);

        countdownController ??= FindAnyObjectByType<StartCountdownController>();

        if (countdownController != null)
        {
            countdownController.Begin(() =>
            {
                SetPlayerControlsEnabled(true);
                Time.timeScale = 1f;
            });
        }
        else
        {
            SetPlayerControlsEnabled(true);
            Time.timeScale = 1f;
        }
    }

    public void ShowMainMenu()
    {
        Time.timeScale = 0f;
        SetPlayerControlsEnabled(false);
        SetGameplayUiVisible(false);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.transform.SetAsLastSibling();
            mainMenuPanel.SetActive(true);
        }
    }

    public void ShowSettings()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.transform.SetAsLastSibling();
            settingsPanel.SetActive(true);
        }
    }

    public void ToggleSound()
    {
        soundEnabled = !soundEnabled;
        ApplySoundPreference();

        PlayerPrefs.SetInt(
            SoundPreferenceKey,
            soundEnabled ? 1 : 0);

        PlayerPrefs.Save();
    }

    public void ToggleVibration()
    {
        VibrationEnabled = !VibrationEnabled;

        PlayerPrefs.SetInt(
            VibrationPreferenceKey,
            VibrationEnabled ? 1 : 0);

        PlayerPrefs.Save();
        RefreshPreferenceLabels();
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ResolveReferences()
    {
        mainMenuPanel = FindNamedObject("MainMenuPanel");
        settingsPanel = FindNamedObject("SettingsPanel");

        runner = FindAnyObjectByType<PlayerRunner>();
        horizontalController =
            FindAnyObjectByType<PlayerHorizontalController>();

        formController =
            FindAnyObjectByType<PlayerFormController>();

        sizeController =
            FindAnyObjectByType<BalloonSizeController>();

        airController =
            FindAnyObjectByType<AirController>();

        playerBody = runner != null
            ? runner.GetComponent<Rigidbody>()
            : null;

        countdownController =
            FindAnyObjectByType<StartCountdownController>();

        soundButtonLabel =
            FindNamedComponent<TMP_Text>("SoundButtonLabel");

        vibrationButtonLabel =
            FindNamedComponent<TMP_Text>("VibrationButtonLabel");
    }

    private void BindButtons()
    {
        BindButton("PlayButton", StartGame);
        BindButton("SettingsButton", ShowSettings);
        BindButton("SettingsBackButton", ShowMainMenu);
        BindButton("SoundToggleButton", ToggleSound);
        BindButton("VibrationToggleButton", ToggleVibration);
        BindButton("ExitButton", ExitGame);

        GameManager manager =
            GameManager.Instance ?? FindAnyObjectByType<GameManager>();

        if (manager != null)
        {
            BindButton("RestartButton", manager.RestartGame);
            BindButton("EndMenuButton", manager.ReturnToMainMenu);
        }
    }

    private static void BindButton(
        string objectName,
        UnityEngine.Events.UnityAction action)
    {
        Button button = FindNamedComponent<Button>(objectName);

        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void LoadPreferences()
    {
        soundEnabled =
            PlayerPrefs.GetInt(SoundPreferenceKey, 1) == 1;

        VibrationEnabled =
            PlayerPrefs.GetInt(VibrationPreferenceKey, 1) == 1;

        ApplySoundPreference();
    }

    private void ApplySoundPreference()
    {
        AudioListener.volume = soundEnabled ? 1f : 0f;
        RefreshPreferenceLabels();
    }

    private void RefreshPreferenceLabels()
    {
        if (soundButtonLabel != null)
        {
            soundButtonLabel.text =
                soundEnabled ? "SES: AÇIK" : "SES: KAPALI";
        }

        if (vibrationButtonLabel != null)
        {
            vibrationButtonLabel.text = VibrationEnabled
                ? "TİTREŞİM: AÇIK"
                : "TİTREŞİM: KAPALI";
        }
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        if (runner != null)
        {
            runner.SetMovementEnabled(enabled);
        }

        if (horizontalController != null)
        {
            horizontalController.enabled = enabled;
        }

        if (formController != null)
        {
            if (!enabled)
            {
                formController.ForceBalloonForm();
            }

            formController.enabled = enabled;
        }

        if (sizeController != null)
        {
            if (!enabled)
            {
                sizeController.CancelShrinkImmediately();
            }

            sizeController.enabled = enabled;
        }

        if (!enabled && playerBody != null)
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
        }
    }

    private static void SetGameplayUiVisible(bool visible)
    {
        string[] names =
        {
            "SpeedText",
            "AirSlider",
            "ScoreText",
            "ControlHint",
            "ShrinkButton",
            "HelicopterButton",
            "ProgressSlider",
            "V5GameplayHud"
        };

        foreach (string objectName in names)
        {
            GameObject gameObject = FindNamedObject(objectName);

            if (gameObject != null)
            {
                gameObject.SetActive(visible);
            }
        }
    }

    private void ApplyBalloonMenuTheme()
    {
        StylePanel(mainMenuPanel, new Color(0.055f, 0.38f, 0.63f, 0.98f));
        StylePanel(settingsPanel, new Color(0.045f, 0.30f, 0.53f, 0.98f));

        CreateBubbleBackdrop(mainMenuPanel);
        CreateBubbleBackdrop(settingsPanel);

        StyleButton(
            "PlayButton",
            new Color(1f, 0.47f, 0.02f, 1f),
            new Color(1f, 0.62f, 0.13f, 1f),
            48f);

        StyleButton(
            "SettingsButton",
            new Color(0.05f, 0.68f, 0.88f, 1f),
            new Color(0.18f, 0.80f, 0.98f, 1f),
            38f);

        StyleButton(
            "ExitButton",
            new Color(0.90f, 0.05f, 0.16f, 1f),
            new Color(1f, 0.18f, 0.28f, 1f),
            34f);

        StyleButton(
            "SettingsBackButton",
            new Color(0.05f, 0.68f, 0.88f, 1f),
            new Color(0.18f, 0.80f, 0.98f, 1f),
            34f);

        StyleButton(
            "SoundToggleButton",
            new Color(0.12f, 0.63f, 0.86f, 1f),
            new Color(0.25f, 0.77f, 0.98f, 1f),
            30f);

        StyleButton(
            "VibrationToggleButton",
            new Color(0.12f, 0.63f, 0.86f, 1f),
            new Color(0.25f, 0.77f, 0.98f, 1f),
            30f);

        ApplyCleanMainMenuLayout();
        StyleShrinkButton();
    }

    private void ApplyCleanMainMenuLayout()
    {
        if (mainMenuPanel == null)
        {
            return;
        }

        RectTransform layoutRoot = GetOrCreateMenuLayoutRoot(mainMenuPanel.transform);
        TMP_Text[] allTexts =
            mainMenuPanel.GetComponentsInChildren<TMP_Text>(true);

        TMP_Text title = FindFirstMenuText(
            allTexts,
            new[] { "MenuTitle", "TitleText", "GameTitle" },
            new[] { "BALLOON" });

        TMP_Text subtitle = FindFirstMenuText(
            allTexts,
            new[] { "MenuSubtitle", "SubtitleText", "TaglineText" },
            new[] { "HAVANI", "KAÇ", "FIRLAT" });

        TMP_Text footer = FindFirstMenuText(
            allTexts,
            new[] { "FooterText", "VersionText" },
            new[] { "PROTOTİP", "MOBİL RUNNER" });

        List<TMP_Text> bestScoreTexts = new List<TMP_Text>();

        foreach (TMP_Text text in allTexts)
        {
            if (text == null)
            {
                continue;
            }

            string value = text.text != null
                ? text.text.ToUpperInvariant()
                : string.Empty;

            string objectName = text.name.ToUpperInvariant();

            if (value.Contains("EN İYİ") ||
                objectName.Contains("BESTSCORE") ||
                objectName.Contains("HIGHSCORE"))
            {
                bestScoreTexts.Add(text);
            }
        }

        TMP_Text bestScore = null;

        foreach (TMP_Text candidate in bestScoreTexts)
        {
            if (candidate.name == "BestScoreText" ||
                candidate.name == "HighScoreText")
            {
                bestScore = candidate;
                break;
            }
        }

        if (bestScore == null && bestScoreTexts.Count > 0)
        {
            bestScore = bestScoreTexts[0];
        }

        foreach (TMP_Text duplicate in bestScoreTexts)
        {
            if (duplicate != null)
            {
                duplicate.gameObject.SetActive(duplicate == bestScore);
            }
        }

        ReparentToLayout(title, layoutRoot);
        ReparentToLayout(bestScore, layoutRoot);
        ReparentToLayout(subtitle, layoutRoot);
        ReparentToLayout(footer, layoutRoot);

        Button playButton = ReparentButton("PlayButton", layoutRoot);
        Button settingsButton = ReparentButton("SettingsButton", layoutRoot);
        Button exitButton = ReparentButton("ExitButton", layoutRoot);

        if (title != null)
        {
            title.text = "BALLOON DOG";
            title.fontStyle = FontStyles.Bold;
            title.enableAutoSizing = true;
            title.fontSizeMin = 46f;
            title.fontSizeMax = 76f;
            title.enableWordWrapping = false;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.white;

            SetNormalizedRect(
                title.rectTransform,
                new Vector2(0.5f, 0.83f),
                new Vector2(900f, 115f));
        }

        if (bestScore != null)
        {
            bestScore.enableAutoSizing = true;
            bestScore.fontSizeMin = 22f;
            bestScore.fontSizeMax = 34f;
            bestScore.enableWordWrapping = false;
            bestScore.alignment = TextAlignmentOptions.Center;
            bestScore.color = new Color(1f, 0.78f, 0.18f, 1f);

            SetNormalizedRect(
                bestScore.rectTransform,
                new Vector2(0.5f, 0.69f),
                new Vector2(520f, 52f));
        }

        SetNormalizedButton(
            playButton,
            new Vector2(0.5f, 0.565f),
            new Vector2(470f, 78f));

        if (subtitle != null)
        {
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 20f;
            subtitle.fontSizeMax = 31f;
            subtitle.enableWordWrapping = false;
            subtitle.overflowMode = TextOverflowModes.Ellipsis;
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.color = new Color(0.86f, 0.95f, 1f, 1f);

            SetNormalizedRect(
                subtitle.rectTransform,
                new Vector2(0.5f, 0.435f),
                new Vector2(930f, 54f));
        }

        SetNormalizedButton(
            settingsButton,
            new Vector2(0.5f, 0.31f),
            new Vector2(400f, 68f));

        SetNormalizedButton(
            exitButton,
            new Vector2(0.5f, 0.19f),
            new Vector2(330f, 60f));

        if (footer != null)
        {
            footer.enableAutoSizing = true;
            footer.fontSizeMin = 14f;
            footer.fontSizeMax = 20f;
            footer.enableWordWrapping = false;
            footer.alignment = TextAlignmentOptions.Center;
            footer.color = new Color(0.55f, 0.78f, 0.92f, 1f);

            SetNormalizedRect(
                footer.rectTransform,
                new Vector2(0.5f, 0.055f),
                new Vector2(620f, 34f));
        }
    }

    private static RectTransform GetOrCreateMenuLayoutRoot(Transform parent)
    {
        Transform existing = parent.Find("V11MenuLayout");

        if (existing != null)
        {
            return existing as RectTransform;
        }

        GameObject root = new GameObject(
            "V11MenuLayout",
            typeof(RectTransform));

        root.transform.SetParent(parent, false);
        root.transform.SetAsLastSibling();

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        return rect;
    }

    private static TMP_Text FindFirstMenuText(
        TMP_Text[] texts,
        string[] preferredNames,
        string[] contentTokens)
    {
        foreach (string preferredName in preferredNames)
        {
            foreach (TMP_Text text in texts)
            {
                if (text != null && text.name == preferredName)
                {
                    return text;
                }
            }
        }

        foreach (TMP_Text text in texts)
        {
            if (text == null || string.IsNullOrEmpty(text.text))
            {
                continue;
            }

            string value = text.text.ToUpperInvariant();

            foreach (string token in contentTokens)
            {
                if (value.Contains(token))
                {
                    return text;
                }
            }
        }

        return null;
    }

    private static void ReparentToLayout(
        Component component,
        RectTransform layoutRoot)
    {
        if (component == null || layoutRoot == null)
        {
            return;
        }

        component.transform.SetParent(layoutRoot, false);
    }

    private static Button ReparentButton(
        string objectName,
        RectTransform layoutRoot)
    {
        Button button = FindNamedComponent<Button>(objectName);

        if (button != null && layoutRoot != null)
        {
            button.transform.SetParent(layoutRoot, false);
        }

        return button;
    }

    private static void SetNormalizedButton(
        Button button,
        Vector2 anchor,
        Vector2 size)
    {
        if (button == null)
        {
            return;
        }

        SetNormalizedRect(
            button.transform as RectTransform,
            anchor,
            size);
    }

    private static void SetNormalizedRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void StyleShrinkButton()
    {
        GameObject shrinkButton = FindNamedObject("ShrinkButton");

        if (shrinkButton == null)
        {
            return;
        }

        RectTransform rootRect = shrinkButton.transform as RectTransform;

        if (rootRect != null)
        {
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(0f, 115f);
            rootRect.sizeDelta = new Vector2(170f, 170f);
            rootRect.localScale = Vector3.one;
        }

        Image background =
            shrinkButton.GetComponent<Image>() ??
            shrinkButton.AddComponent<Image>();

        background.sprite = GetCircleSprite();
        background.type = Image.Type.Simple;
        background.color = new Color(0.06f, 0.68f, 0.92f, 0.98f);
        background.raycastTarget = true;

        Button button = shrinkButton.GetComponent<Button>();

        if (button != null)
        {
            button.targetGraphic = background;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.82f, 0.90f, 0.96f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.65f, 0.72f, 0.55f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        foreach (TMP_Text label in
                 shrinkButton.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label != null)
            {
                label.gameObject.SetActive(false);
            }
        }

        Transform iconTransform =
            shrinkButton.transform.Find("ShrinkTransformIcon");

        Image icon;

        if (iconTransform == null)
        {
            GameObject iconObject = new GameObject(
                "ShrinkTransformIcon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            iconObject.transform.SetParent(shrinkButton.transform, false);
            icon = iconObject.GetComponent<Image>();
        }
        else
        {
            icon = iconTransform.GetComponent<Image>() ??
                   iconTransform.gameObject.AddComponent<Image>();
        }

        icon.sprite = GetShrinkIconSprite();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.color = Color.white;

        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0.10f, 0.10f);
        iconRect.anchorMax = new Vector2(0.90f, 0.90f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        iconRect.localScale = Vector3.one;

        Outline outline =
            background.GetComponent<Outline>() ??
            background.gameObject.AddComponent<Outline>();

        outline.effectColor = new Color(0.01f, 0.18f, 0.32f, 0.75f);
        outline.effectDistance = new Vector2(5f, -5f);
    }

    private static Sprite GetShrinkIconSprite()
    {
        if (cachedShrinkIconSprite != null)
        {
            return cachedShrinkIconSprite;
        }

        Texture2D texture =
            Resources.Load<Texture2D>("shrink_transform_icon");

        if (texture == null)
        {
            return null;
        }

        cachedShrinkIconSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        return cachedShrinkIconSprite;
    }

    private static void StylePanel(
        GameObject panel,
        Color color)
    {
        if (panel == null)
        {
            return;
        }

        Image image =
            panel.GetComponent<Image>() ??
            panel.AddComponent<Image>();

        image.color = color;
        image.raycastTarget = true;
    }

    private static void StyleButton(
        string objectName,
        Color normal,
        Color highlighted,
        float fontSize)
    {
        Button button = FindNamedComponent<Button>(objectName);

        if (button == null)
        {
            return;
        }

        Image image =
            button.GetComponent<Image>() ??
            button.gameObject.AddComponent<Image>();

        image.sprite = GetRoundedSprite();
        image.type = Image.Type.Sliced;
        image.color = normal;

        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.pressedColor = Color.Lerp(normal, Color.black, 0.12f);
        colors.selectedColor = highlighted;
        colors.disabledColor = new Color(
            normal.r,
            normal.g,
            normal.b,
            0.45f);

        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.targetGraphic = image;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

        if (label != null)
        {
            label.fontStyle = FontStyles.Bold;
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(20f, fontSize * 0.58f);
            label.fontSizeMax = fontSize;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.margin = new Vector4(20f, 8f, 20f, 8f);
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;

            Outline outline =
                label.GetComponent<Outline>() ??
                label.gameObject.AddComponent<Outline>();

            outline.effectColor =
                new Color(0.02f, 0.12f, 0.22f, 0.75f);

            outline.effectDistance =
                new Vector2(2f, -2f);
        }
    }

    private static void CreateBubbleBackdrop(GameObject panel)
    {
        if (panel == null ||
            panel.transform.Find("BalloonThemeBubbles") != null)
        {
            return;
        }

        GameObject root = new GameObject(
            "BalloonThemeBubbles",
            typeof(RectTransform));

        root.transform.SetParent(panel.transform, false);
        root.transform.SetAsFirstSibling();

        RectTransform rootRect =
            root.GetComponent<RectTransform>();

        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Vector2[] positions =
        {
            new Vector2(0.08f, 0.14f),
            new Vector2(0.18f, 0.78f),
            new Vector2(0.32f, 0.92f),
            new Vector2(0.72f, 0.86f),
            new Vector2(0.88f, 0.68f),
            new Vector2(0.92f, 0.22f),
            new Vector2(0.64f, 0.10f),
            new Vector2(0.42f, 0.18f)
        };

        float[] sizes =
        {
            92f, 54f, 38f, 76f,
            46f, 110f, 58f, 34f
        };

        for (int index = 0; index < positions.Length; index++)
        {
            GameObject bubble = new GameObject(
                $"Bubble_{index + 1}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            bubble.transform.SetParent(root.transform, false);

            Image image = bubble.GetComponent<Image>();
            image.sprite = GetCircleSprite();
            image.color = new Color(
                1f,
                1f,
                1f,
                index % 2 == 0 ? 0.15f : 0.09f);

            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = positions[index];
            rect.anchorMax = positions[index];
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.one * sizes[index];
        }
    }

    private static Sprite GetCircleSprite()
    {
        if (cachedCircleSprite != null)
        {
            return cachedCircleSprite;
        }

        cachedCircleSprite = CreateProceduralSprite(
            128,
            0.48f,
            0.48f);

        return cachedCircleSprite;
    }

    private static Sprite GetRoundedSprite()
    {
        if (cachedRoundedSprite != null)
        {
            return cachedRoundedSprite;
        }

        cachedRoundedSprite = CreateProceduralSprite(
            128,
            0.46f,
            0.22f);

        return cachedRoundedSprite;
    }

    private static Sprite CreateProceduralSprite(
        int size,
        float horizontalRadius,
        float verticalRadius)
    {
        Texture2D texture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false);

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        Vector2 center =
            new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        float radiusX = size * horizontalRadius;
        float radiusY = size * verticalRadius;
        float feather = 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float normalizedX =
                    Mathf.Abs(x - center.x) / radiusX;

                float normalizedY =
                    Mathf.Abs(y - center.y) / radiusY;

                float distance = Mathf.Max(
                    normalizedX,
                    normalizedY);

                float alpha = Mathf.Clamp01(
                    (1f - distance) * size / feather);

                pixels[y * size + x] =
                    new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private static GameObject FindNamedObject(string objectName)
    {
        Transform[] transforms =
            Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform candidate in transforms)
        {
            if (candidate == null ||
                candidate.name != objectName)
            {
                continue;
            }

            if (!candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            return candidate.gameObject;
        }

        return null;
    }

    private static T FindNamedComponent<T>(
        string objectName) where T : Component
    {
        GameObject gameObject = FindNamedObject(objectName);

        return gameObject != null
            ? gameObject.GetComponent<T>()
            : null;
    }
}
