using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Release campaign glue and a small additive UI layer. It keeps the existing
/// Figma-styled front end intact while adding saved level selection, progress,
/// next-level flow and Android back-button behaviour.
/// </summary>
public sealed class BalloonDogCampaignRuntime : MonoBehaviour
{
    private const string RuntimeName = "__BalloonDogCampaignRuntime";

    private static readonly Color DeepPurple = new Color(0.12f, 0.015f, 0.38f, 0.97f);
    private static readonly Color BrightPurple = new Color(0.34f, 0.06f, 0.86f, 1f);
    private static readonly Color Orange = new Color(1f, 0.48f, 0.035f, 1f);
    private static readonly Color Lime = new Color(0.36f, 0.96f, 0.03f, 1f);
    private static readonly Color Gold = new Color(1f, 0.70f, 0.12f, 1f);

    private Canvas canvas;
    private RectTransform safeRoot;
    private GameObject selector;
    private TMP_Text selectorLabel;
    private GameObject gameplayProgress;
    private TMP_Text progressLabel;
    private Image progressFill;
    private GameObject nextLevelButton;
    private TMP_Text nextLevelLabel;

    private GameManager gameManager;
    private PlayerRunner player;
    private bool processedCurrentResult;
    private bool completedResult;
    private bool privacyNoticeUpdated;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private static Sprite roundedSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntime()
    {
        if (GameObject.Find(RuntimeName) != null)
        {
            return;
        }

        GameObject runtime = new GameObject(RuntimeName);
        DontDestroyOnLoad(runtime);
        runtime.AddComponent<BalloonDogCampaignRuntime>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BalloonDogCampaign.Changed += RefreshLabels;
        BuildCanvas();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        gameManager = null;
        player = null;
        processedCurrentResult = false;
        completedResult = false;
        privacyNoticeUpdated = false;
        RefreshLabels();
    }

    private void Update()
    {
        ResolveGameplay();
        ProcessGameResult();
        RefreshVisibility();
        RefreshProgress();
        ApplySafeArea();
        HandleAndroidBack();
    }

    private void ResolveGameplay()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
        }
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerRunner>();
        }
    }

    private void ProcessGameResult()
    {
        if (gameManager == null)
        {
            return;
        }

        if (!gameManager.IsGameOver)
        {
            processedCurrentResult = false;
            completedResult = false;
            return;
        }
        if (processedCurrentResult)
        {
            return;
        }

        processedCurrentResult = true;
        completedResult = gameManager.LastRunCompleted;
        if (!completedResult)
        {
            return;
        }

        int completedLevel = BalloonDogCampaign.CurrentLevel;
        BalloonDogCampaign.MarkCurrentLevelComplete();
        if (completedLevel < BalloonDogCampaign.LevelCount)
        {
            BalloonDogCampaign.SelectLevel(completedLevel + 1);
        }
        RefreshLabels();
    }

    private void RefreshVisibility()
    {
        bool mainVisible = IsActive("ModernMainScreen");
        bool gameplayVisible = IsActive("ModernGameplayOverlay") &&
                               gameManager != null &&
                               !gameManager.IsGameOver;
        bool resultVisible = IsActive("ModernResultScreen") &&
                             gameManager != null &&
                             gameManager.IsGameOver &&
                             completedResult;

        selector.SetActive(mainVisible);
        gameplayProgress.SetActive(gameplayVisible);
        nextLevelButton.SetActive(resultVisible);

        GameObject restartButton = GameObject.Find("ModernRestartButton");
        if (restartButton != null)
        {
            restartButton.SetActive(!resultVisible);
        }
        if (IsActive("ModernPrivacyScreen"))
        {
            RefreshPrivacyNotice();
        }
    }

    private void RefreshProgress()
    {
        if (!gameplayProgress.activeSelf)
        {
            return;
        }

        float progress = player != null
            ? Mathf.Clamp01(player.transform.position.z /
                            Mathf.Max(1f, BalloonDogLevelDirector.FinishZ))
            : 0f;
        progressFill.fillAmount = progress;
        progressLabel.text =
            "LEVEL " + BalloonDogCampaign.CurrentLevel +
            "  •  " + Mathf.RoundToInt(progress * 100f) + "%";
    }

    private void SelectPrevious()
    {
        int current = BalloonDogCampaign.CurrentLevel;
        if (current <= 1)
        {
            return;
        }

        BalloonDogCampaign.SelectLevel(current - 1);
        BalloonDogLevelDirector.RebuildSelectedLevel();
        player = FindAnyObjectByType<PlayerRunner>();
    }

    private void SelectNext()
    {
        int current = BalloonDogCampaign.CurrentLevel;
        if (current >= BalloonDogCampaign.UnlockedLevel)
        {
            return;
        }

        BalloonDogCampaign.SelectLevel(current + 1);
        BalloonDogLevelDirector.RebuildSelectedLevel();
        player = FindAnyObjectByType<PlayerRunner>();
    }

    private void ContinueCampaign()
    {
        if (gameManager == null)
        {
            return;
        }

        Time.timeScale = 1f;
        gameManager.RestartGame();
    }

    private void RefreshLabels()
    {
        if (selectorLabel != null)
        {
            int level = BalloonDogCampaign.CurrentLevel;
            selectorLabel.text =
                "LEVEL " + level + " / " + BalloonDogCampaign.LevelCount +
                "\n" + BalloonDogCampaign.GetLevelName(level);
        }

        if (nextLevelLabel != null)
        {
            nextLevelLabel.text =
                BalloonDogCampaign.CurrentLevel >= BalloonDogCampaign.LevelCount
                    ? "PLAY AGAIN"
                    : "NEXT LEVEL";
        }
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject(
            "BalloonDogCampaignCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2348f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        safeRoot = CreateRect("CampaignSafeArea", canvasObject.transform);
        Stretch(safeRoot);

        selector = CreateRect("CampaignLevelSelector", safeRoot).gameObject;
        SetRect(selector.GetComponent<RectTransform>(), new Vector2(0f, 495f), new Vector2(850f, 116f));
        CreatePanel(selector.transform, new Vector2(0f, 0f), new Vector2(600f, 116f), DeepPurple);
        selectorLabel = CreateText(selector.transform, "LevelLabel", "LEVEL 1", Vector2.zero, new Vector2(550f, 104f), 27f, Color.white);
        CreateButton(selector.transform, "PreviousLevel", "", new Vector2(-370f, 0f), new Vector2(104f, 104f), DeepPurple, Color.white, SelectPrevious);
        CreateButton(selector.transform, "NextLevel", "", new Vector2(370f, 0f), new Vector2(104f, 104f), Orange, new Color(0.15f, 0.05f, 0.01f), SelectNext);

        gameplayProgress = CreateRect("CampaignGameplayProgress", safeRoot).gameObject;
        SetRect(gameplayProgress.GetComponent<RectTransform>(), new Vector2(0f, 955f), new Vector2(430f, 104f));
        CreatePanel(gameplayProgress.transform, Vector2.zero, new Vector2(430f, 104f), new Color(0.035f, 0.02f, 0.10f, 0.84f));
        CreatePanel(gameplayProgress.transform, new Vector2(0f, -26f), new Vector2(350f, 18f), new Color(1f, 1f, 1f, 0.24f));
        progressFill = CreatePanel(gameplayProgress.transform, new Vector2(-175f, -26f), new Vector2(350f, 18f), Orange);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;
        progressFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        progressLabel = CreateText(gameplayProgress.transform, "ProgressLabel", "LEVEL 1  •  0%", new Vector2(0f, 17f), new Vector2(380f, 48f), 23f, Gold);

        Button resultButton = CreateButton(
            safeRoot,
            "CampaignNextLevelButton",
            "NEXT LEVEL",
            new Vector2(0f, -665f),
            new Vector2(790f, 150f),
            Lime,
            new Color(0.08f, 0.25f, 0.015f),
            ContinueCampaign);
        nextLevelButton = resultButton.gameObject;
        nextLevelLabel = resultButton.GetComponentInChildren<TMP_Text>(true);

        selector.SetActive(false);
        gameplayProgress.SetActive(false);
        nextLevelButton.SetActive(false);
        RefreshLabels();
        ApplySafeArea();
    }

    private void HandleAndroidBack()
    {
        if (Keyboard.current == null ||
            !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (IsActive("ModernPrivacyScreen"))
        {
            InvokeButton("PrivacyBackButton");
        }
        else if (IsActive("ModernSettingsScreen"))
        {
            InvokeButton("SettingsClose");
        }
        else if (IsActive("ModernMarketScreen"))
        {
            InvokeButton("MarketClose");
        }
        else if (IsActive("ModernSkinsScreen"))
        {
            InvokeButton("SkinsClose");
        }
        else if (IsActive("ModernPauseScreen"))
        {
            InvokeButton("ModernResumeButton");
        }
        else if (IsActive("ModernGameplayOverlay"))
        {
            InvokeButton("ModernPauseButton");
        }
        else if (IsActive("ModernResultScreen"))
        {
            InvokeButton("ModernResultMenuButton");
        }
        else if (IsActive("ModernMainScreen"))
        {
            Application.Quit();
        }
    }

    private void RefreshPrivacyNotice()
    {
        if (privacyNoticeUpdated)
        {
            return;
        }

        GameObject privacyCopy = GameObject.Find("PrivacyCopy");
        TMP_Text text = privacyCopy != null
            ? privacyCopy.GetComponent<TMP_Text>()
            : null;
        if (text == null)
        {
            return;
        }

        text.text =
            "Balloon Dog stores campaign progress, high score, tokens, owned " +
            "skins, the selected skin and settings only on this device.\n\n" +
            "The game does not require an account and this release contains " +
            "no advertising, analytics, cloud save, location, microphone, " +
            "camera or in-app purchase service.\n\n" +
            "Deleting the app or clearing its local data may remove progress. " +
            "If online services are added later, this notice and the public " +
            "privacy policy must be updated before release.\n\n" +
            "Privacy notice • Updated August 7, 2026";
        privacyNoticeUpdated = true;
    }

    private static void InvokeButton(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        Button button = target != null ? target.GetComponent<Button>() : null;
        if (button != null && button.interactable)
        {
            button.onClick.Invoke();
        }
    }

    private void ApplySafeArea()
    {
        Rect area = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        if (area == lastSafeArea && screenSize == lastScreenSize)
        {
            return;
        }
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        lastSafeArea = area;
        lastScreenSize = screenSize;
        Vector2 anchorMin = area.position;
        Vector2 anchorMax = area.position + area.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        safeRoot.anchorMin = anchorMin;
        safeRoot.anchorMax = anchorMax;
        safeRoot.offsetMin = Vector2.zero;
        safeRoot.offsetMax = Vector2.zero;
    }

    private static bool IsActive(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        return target != null && target.activeInHierarchy;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        Vector2 size,
        Color background,
        Color foreground,
        UnityEngine.Events.UnityAction action)
    {
        Image image = CreatePanel(parent, position, size, background);
        image.gameObject.name = name;
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(action);
        CreateText(image.transform, "Label", label, Vector2.zero, size - new Vector2(20f, 12f), 35f, foreground);
        return button;
    }

    private static Image CreatePanel(Transform parent, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = CreateRect("Panel", parent);
        SetRect(rect, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = GetRoundedSprite();
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize,
        Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        SetRect(rect, position, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.enableAutoSizing = true;
        text.fontSizeMin = 17f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject created = new GameObject(name, typeof(RectTransform));
        created.transform.SetParent(parent, false);
        return created.GetComponent<RectTransform>();
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
    }

    private static Sprite GetRoundedSprite()
    {
        if (roundedSprite != null)
        {
            return roundedSprite;
        }

        const int size = 64;
        const float radius = 15f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "CampaignRoundedRect";
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - (size - 1) * 0.5f) - ((size - 1) * 0.5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - (size - 1) * 0.5f) - ((size - 1) * 0.5f - radius), 0f);
                float alpha = 1f - Mathf.SmoothStep(radius - 1f, radius + 1f, Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        roundedSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(16f, 16f, 16f, 16f));
        return roundedSprite;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        BalloonDogCampaign.Changed -= RefreshLabels;
    }
}

/// <summary>Direct campaign finish without the obsolete launch minigame.</summary>
[RequireComponent(typeof(Collider))]
public sealed class BalloonDogCampaignFinish : MonoBehaviour
{
    private bool completed;
    private ScoreController playerScore;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        playerScore ??= FindAnyObjectByType<ScoreController>();
        if (!completed && playerScore != null &&
            playerScore.transform.position.z >= transform.position.z - 0.35f)
        {
            Complete(playerScore);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed || other == null ||
            other.GetComponent<NearMissSensor>() != null)
        {
            return;
        }

        ScoreController score = other.GetComponentInParent<ScoreController>();
        if (score != null)
        {
            Complete(score);
        }
    }

    private void Complete(ScoreController score)
    {
        if (completed)
        {
            return;
        }
        completed = true;

        PlayerRunner runner = score.GetComponent<PlayerRunner>();
        if (runner != null)
        {
            runner.SetMovementEnabled(false);
        }
        PlayerHorizontalController horizontal =
            score.GetComponent<PlayerHorizontalController>();
        if (horizontal != null)
        {
            horizontal.enabled = false;
        }

        GameAudioController.PlayFinish();
        GameManager manager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
        manager?.TriggerLevelComplete(score.CurrentScore);
    }
}
