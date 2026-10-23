using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>Real startup preparation progress and persistent scene-transition cover.</summary>
[DefaultExecutionOrder(-10000)]
public sealed class BalloonDogSceneFlow : MonoBehaviour
{
    public const string BootScenePath = "Assets/Scenes/Boot.unity";
    public const string GameScenePath = "Assets/Scenes/Game.unity";
    private const float FadeSeconds = .45f;
    private const float MinimumBlackSeconds = .3f;
    private static BalloonDogSceneFlow instance;
    public static bool IsBusy => instance != null && instance.busy;
    public float LoadingProgress { get; private set; }
    public float CoverAlpha => cover != null ? cover.alpha : 0f;
    private bool busy = true;
    private bool failed;
    private bool replay;
    private Canvas overlay;
    private CanvasGroup artwork;
    private CanvasGroup cover;
    private RawImage illustration;
    private Image fill;
    private Text caption;
    private GameObject retryButton;
    private Sprite pill;
    private Texture2D pillTexture;
    private Coroutine routine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        MainMenuController.StartImmediatelyOnNextSceneLoad = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance != null) return;
        new GameObject("__BalloonDogSceneFlow").AddComponent<BalloonDogSceneFlow>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }

    private void Start()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != BootScenePath && scene.path != GameScenePath)
        {
            busy = false;
            overlay.gameObject.SetActive(false);
            return;
        }
        routine = StartCoroutine(Startup(scene.path == BootScenePath));
    }

    private IEnumerator Startup(bool loadGame)
    {
        // Boot loads only the cover asset first, so a frame can be presented
        // before Unity begins loading/activating the much larger game scene.
        ResourceRequest image = Resources.LoadAsync<Texture2D>("MenuRefresh/LoadingScreen");
        while (!image.isDone)
        {
            SetProgress(.1f * image.progress);
            yield return null;
        }
        illustration.texture = image.asset as Texture2D;
        illustration.enabled = illustration.texture != null;
        if (illustration.texture == null) { ShowFailure(); yield break; }
        SetProgress(.1f);
        artwork.alpha = 1f;
        cover.alpha = 0f;
        yield return null;
        if (loadGame) yield return LoadGameScene(true);
        if (failed) yield break;
        yield return WaitForPreparedScene(true);
        if (failed) yield break;
        SetProgress(1f);
        // 100% means both scene and runtime setup are ready. Render it once.
        yield return null;
        yield return Fade(artwork, 0f);
        Finish();
    }

    public static void ReloadGame(bool startImmediately)
    {
        if (instance == null) Install();
        if (instance.busy) return; // Duplicate taps cannot enqueue another reload.
        instance.busy = true;
        instance.replay = startImmediately;
        instance.failed = false;
        instance.overlay.gameObject.SetActive(true);
        instance.artwork.alpha = 0f;
        instance.routine = instance.StartCoroutine(instance.ReloadUnderBlack());
    }

    private IEnumerator ReloadUnderBlack()
    {
        Time.timeScale = 0f;
        // Keep the current result/pause UI in place until it is fully covered.
        yield return Fade(cover, 1f);
        float blackAt = Time.realtimeSinceStartup;
        MainMenuController.StartImmediatelyOnNextSceneLoad = replay;
        GameAudioController.SetPaused(false);
        yield return null; // Submit an opaque frame before scene activation.
        yield return LoadGameScene(false);
        if (failed) yield break;
        yield return WaitForPreparedScene(false);
        if (failed) yield break;
        while (Time.realtimeSinceStartup - blackAt < MinimumBlackSeconds) yield return null;
        yield return Fade(cover, 0f);
        Finish();
    }

    private IEnumerator LoadGameScene(bool reportProgress)
    {
        AsyncOperation operation = null;
        try { operation = SceneManager.LoadSceneAsync(GameScenePath, LoadSceneMode.Single); }
        catch (Exception exception) { Debug.LogException(exception); }
        if (operation == null) { ShowFailure(); yield break; }
        while (!operation.isDone)
        {
            if (reportProgress) SetProgress(SceneLoadProgress(operation.progress));
            yield return null;
        }
        // These older one-shot helpers were originally installed only for the
        // application's first scene. Boot is now first, so recreate scene-owned
        // helpers after loading Game, while the new scene is still covered.
        if (FindAnyObjectByType<BalloonUiPolishRuntime>() == null)
            new GameObject("__BalloonUiPolishV14").AddComponent<BalloonUiPolishRuntime>();
        if (FindAnyObjectByType<WorldLabelDirectionFix>() == null)
            new GameObject("__WorldLabelDirectionFixV15").AddComponent<WorldLabelDirectionFix>();
        if (FindAnyObjectByType<GroundPlainCleanupRuntime>() == null)
            new GameObject("__GroundPlainCleanupV15").AddComponent<GroundPlainCleanupRuntime>();
        if (reportProgress) SetProgress(.7f);
    }

    private IEnumerator WaitForPreparedScene(bool reportProgress)
    {
        float startedAt = Time.realtimeSinceStartup;
        while (true)
        {
            var course = FindAnyObjectByType<BalloonDogEndlessRunner>();
            var ui = FindAnyObjectByType<BalloonDogModernUI>();
            bool uiReady = ui != null && ui.IsReady &&
                           ui.PreparedScene == SceneManager.GetActiveScene();
            bool courseReady = course != null && course.IsInitialized;
            if (reportProgress)
                SetProgress(PreparationProgress(course != null ? course.InitializationProgress : 0f, uiReady));
            if (courseReady && uiReady) break;
            if (course != null && course.InitializationFailed || Time.realtimeSinceStartup - startedAt > 60f)
            {
                ShowFailure();
                yield break;
            }
            yield return null;
        }
        // Let deferred destruction, selected-skin setup, LateUpdate and canvas
        // layout complete behind the cover; a sceneLoaded callback alone is early.
        BalloonDogSkinSystem.ApplySelectedSkin();
        Canvas.ForceUpdateCanvases();
        yield return null;
        yield return null;
    }

    private static float SceneLoadProgress(float progress) => .1f + .6f * Mathf.Clamp01(progress / .9f);
    private static float PreparationProgress(float courseProgress, bool uiReady) =>
        .7f + .25f * Mathf.Clamp01(courseProgress) + (uiReady ? .04f : 0f);

    private void SetProgress(float progress)
    {
        // Completion only comes from readiness, never elapsed time or a timer.
        LoadingProgress = Mathf.Max(LoadingProgress, Mathf.Clamp01(progress));
        fill.rectTransform.anchorMax = new Vector2(LoadingProgress, 1f);
        fill.gameObject.SetActive(LoadingProgress > 0f);
    }

    private IEnumerator Fade(CanvasGroup group, float target)
    {
        float start = group.alpha;
        float elapsed = 0f;
        // Present the starting opacity before easing; a long previous frame
        // must not make the button's own frame jump straight to opaque black.
        yield return null;
        while (elapsed < FadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, FadeAmount(elapsed / FadeSeconds));
            yield return null;
        }
        group.alpha = target;
    }

    private static float FadeAmount(float progress) => Mathf.SmoothStep(0f, 1f, progress);

    private void Finish()
    {
        busy = false;
        routine = null;
        overlay.gameObject.SetActive(false);
        // Menu owns its paused state; an immediate replay begins its countdown
        // on the next frame, after the cover is fully gone.
    }

    private void LateUpdate()
    {
        if (busy) Time.timeScale = 0f;
    }

    private void ShowFailure()
    {
        failed = true;
        artwork.alpha = 1f;
        illustration.enabled = illustration.texture != null;
        cover.alpha = 0f;
        caption.text = "COULDN'T LOAD";
        retryButton.SetActive(true);
        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("SceneFlowEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        // Keep the overlay and input cover up; never reveal a partially built UI.
    }

    private void Retry()
    {
        if (!failed) return;
        failed = false;
        retryButton.SetActive(false);
        caption.text = "LOADING...";
        LoadingProgress = 0f;
        SetProgress(0f);
        MainMenuController.StartImmediatelyOnNextSceneLoad = replay;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Startup(true));
    }

    private void BuildOverlay()
    {
        var host = new GameObject("SceneFlowCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        host.transform.SetParent(transform, false);
        overlay = host.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 32760; // Above continue, pause, legacy and modern UI.
        CanvasScaler scaler = host.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        Image backdrop = MakeImage("InputCover", host.transform, new Color(0f, 0f, 0f, .001f));
        Stretch(backdrop.rectTransform);
        backdrop.raycastTarget = true;
        RectTransform loadingLayer = MakeRect("LoadingLayer", host.transform);
        Stretch(loadingLayer);
        artwork = loadingLayer.gameObject.AddComponent<CanvasGroup>();
        artwork.alpha = 0f;
        Image sky = MakeImage("LoadingBackdrop", loadingLayer, Color.white);
        Stretch(sky.rectTransform);
        sky.gameObject.AddComponent<UiVerticalGradient>().Configure(
            new Color(.08f, .62f, 1f), new Color(.92f, .94f, 1f));
        RectTransform art = MakeRect("LoadingArtwork", loadingLayer);
        Stretch(art);
        var aspect = art.gameObject.AddComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspect.aspectRatio = 9f / 16f;
        illustration = MakeRect("ApprovedRedDogIllustration", art).gameObject.AddComponent<RawImage>();
        Stretch(illustration.rectTransform);
        illustration.raycastTarget = false;

        RectTransform label = MakeRect("LoadingLabel", art);
        Anchor(label, new Vector2(.1f, .162f), new Vector2(.9f, .202f));
        caption = label.gameObject.AddComponent<Text>();
        caption.text = "LOADING...";
        caption.font = Resources.Load<Font>("Fonts/TitanOne/TitanOne-Regular");
        caption.fontSize = 58;
        caption.resizeTextForBestFit = true;
        caption.resizeTextMinSize = 20;
        caption.resizeTextMaxSize = 58;
        caption.alignment = TextAnchor.MiddleCenter;
        caption.color = new Color(.015f, .15f, .38f);
        caption.raycastTarget = false;

        pill = CreatePill();
        Image border = MakeImage("LoadingBarBorder", art, new Color(.015f, .16f, .38f));
        Anchor(border.rectTransform, new Vector2(.135f, .119f), new Vector2(.865f, .157f));
        border.sprite = pill;
        border.type = Image.Type.Sliced;
        Image track = MakeImage("LoadingBarTrack", border.transform, new Color(.75f, .91f, 1f));
        Stretch(track.rectTransform, 6f);
        track.sprite = pill;
        track.type = Image.Type.Sliced;
        fill = MakeImage("RealLoadingFill", track.transform, Color.white);
        Stretch(fill.rectTransform);
        fill.sprite = pill;
        fill.type = Image.Type.Sliced;
        fill.gameObject.AddComponent<UiVerticalGradient>().Configure(
            new Color(.45f, 1f, 1f), new Color(0f, .56f, .96f));
        SetProgress(0f);

        retryButton = MakeRect("RetryLoading", art).gameObject;
        var retryRect = retryButton.GetComponent<RectTransform>();
        Anchor(retryRect, new Vector2(.3f, .07f), new Vector2(.7f, .115f));
        Image retryImage = retryButton.AddComponent<Image>();
        retryImage.color = new Color(.02f, .35f, .75f);
        Button retry = retryButton.AddComponent<Button>();
        retry.targetGraphic = retryImage;
        retry.onClick.AddListener(Retry);
        Text retryLabel = MakeRect("Label", retryButton.transform).gameObject.AddComponent<Text>();
        Stretch(retryLabel.rectTransform);
        retryLabel.font = caption.font;
        retryLabel.fontSize = 40;
        retryLabel.text = "RETRY";
        retryLabel.alignment = TextAnchor.MiddleCenter;
        retryLabel.color = Color.white;
        retryLabel.raycastTarget = false;
        retryButton.SetActive(false);

        Image black = MakeImage("TransitionBlack", host.transform, Color.black);
        Stretch(black.rectTransform);
        black.raycastTarget = false;
        cover = black.gameObject.AddComponent<CanvasGroup>();
        cover.alpha = 1f;
    }

    private static RectTransform MakeRect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }
    private static Image MakeImage(string name, Transform parent, Color color)
    {
        Image image = MakeRect(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
    }
    private static void Anchor(RectTransform rect, Vector2 minimum, Vector2 maximum)
    {
        rect.anchorMin = minimum;
        rect.anchorMax = maximum;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private Sprite CreatePill()
    {
        const int size = 64;
        pillTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        pillTexture.name = "LoadingUiRoundedShape";
        pillTexture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * (size * .5f));
            pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(size * .5f - distance));
        }
        pillTexture.SetPixels(pixels);
        pillTexture.Apply(false, true);
        return Sprite.Create(pillTexture, new Rect(0, 0, size, size), Vector2.one * .5f, 100f, 0,
            SpriteMeshType.FullRect, new Vector4(31f, 31f, 31f, 31f));
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (pill != null) Destroy(pill);
        if (pillTexture != null) Destroy(pillTexture);
    }
}
