using System;
using System.Collections.Concurrent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Frozen-run continue offer. A real rewarded-ad provider must report success.</summary>
public sealed class BalloonDogContinuePrompt : MonoBehaviour
{
    public const float OfferSeconds = 4f;
    // Assign from the ad integration. Report true only after the SDK earns the reward.
    // Report false on cancellation, no fill or error. Never grant on ad close alone.
    public static Action<Action<bool>> ShowRewardedAd;

    private readonly ConcurrentQueue<Action> callbacks = new ConcurrentQueue<Action>();
    private GameManager manager;
    private GameObject overlay;
    private Image timerFill;
    private TMP_Text hint;
    private Button watch;
    private bool activeOffer;
    private bool waitingForAd;
    private float remaining;
    private float adWait;
    private int generation;
    private static Sprite rounded;
    private Sprite panelSprite;
    private Sprite timerSprite;

    public float RemainingSeconds => remaining;
    public bool WaitingForAd => waitingForAd;

    public void Show(GameManager owner)
    {
        Close();
        manager = owner;
        activeOffer = true;
        remaining = OfferSeconds;
        Build();
        UpdateTimer();
    }

    private void Update() { Advance(Time.unscaledDeltaTime); }

    // Kept separate so the frozen-time countdown can be tested deterministically.
    public void Advance(float unscaledSeconds)
    {
        if (!activeOffer) return;
        // Time passes before processing input/callbacks, so an expired offer cannot be resurrected.
        if (!waitingForAd)
        {
            remaining = Mathf.Max(0f, remaining - Mathf.Max(0f, unscaledSeconds));
            UpdateTimer();
            if (remaining <= 0f) { manager.ExpireContinueOffer(); return; }
        }
        else
        {
            adWait += Mathf.Max(0f, unscaledSeconds);
            if (adWait >= 120f)
            {
                waitingForAd = false;
                generation++;
                watch.interactable = true;
                hint.text = "AD UNAVAILABLE — TRY AGAIN";
            }
        }
        while (callbacks.TryDequeue(out Action callback)) callback();
    }

    public void WatchAd()
    {
        if (!activeOffer || waitingForAd || remaining <= 0f) return;
        if (ShowRewardedAd == null)
        {
            hint.text = "AD NOT AVAILABLE";
            return; // Continue the existing timer; never fake a rewarded view.
        }
        waitingForAd = true;
        adWait = 0f;
        watch.interactable = false;
        hint.text = "LOADING AD...";
        int requestGeneration = ++generation;
        try
        {
            ShowRewardedAd(success => callbacks.Enqueue(() =>
            {
                if (!activeOffer || !waitingForAd || requestGeneration != generation) return;
                waitingForAd = false;
                generation++; // Ignore duplicate SDK callbacks.
                if (success) manager.CompleteRewardedContinue();
                else
                {
                    watch.interactable = true;
                    hint.text = "AD NOT COMPLETED";
                }
            }));
        }
        catch (Exception error)
        {
            waitingForAd = false;
            generation++;
            watch.interactable = true;
            hint.text = "AD NOT AVAILABLE";
            Debug.LogWarning("Rewarded continue failed: " + error.Message, this);
        }
    }

    public void Close()
    {
        activeOffer = false;
        waitingForAd = false;
        generation++;
        while (callbacks.TryDequeue(out Action ignored)) { }
        if (overlay != null)
        {
            overlay.SetActive(false);
            Destroy(overlay);
        }
        overlay = null;
        if (panelSprite != null) Destroy(panelSprite);
        panelSprite = null;
        if (timerSprite != null)
        {
            Destroy(timerSprite.texture);
            Destroy(timerSprite);
        }
        timerSprite = null;
    }

    private void UpdateTimer()
    {
        if (timerFill != null) timerFill.rectTransform.anchorMax = new Vector2(remaining / OfferSeconds, 1f);
    }

    private void Build()
    {
        overlay = new GameObject("ContinueOfferCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlay.transform.SetParent(transform, false);
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        Image dim = Box("DimmedFrozenGame", overlay.transform, Vector2.zero, Vector2.zero, new Color(0.015f, 0.025f, 0.06f, 0.68f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        RectTransform safe = Rect("ContinueSafeArea", overlay.transform, Vector2.zero, Vector2.zero);
        Stretch(safe);
        safe.gameObject.AddComponent<BalloonDogSafeArea>();
        RectTransform layout = Rect("ContinueLayout", safe, Vector2.zero, new Vector2(1080f, 1920f));
        layout.gameObject.AddComponent<BalloonDogSettingsFit>();
        Texture2D artwork = Resources.Load<Texture2D>("ContinueUI/ContinuePanel");
        if (artwork == null)
        {
            Debug.LogError("Missing ContinueUI/ContinuePanel artwork", this);
            manager.ExpireContinueOffer();
            return;
        }
        // Runtime sprite cropping leaves the approved source artwork unchanged.
        // Its transparent outer margin is excluded; the visible card stays 900 x 600.
        float sx = artwork.width / 1536f;
        float sy = artwork.height / 1024f;
        panelSprite = Sprite.Create(artwork,
            new UnityEngine.Rect(52f * sx, 79f * sy, 1433f * sx, 865f * sy),
            new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
        Image panel = Box("ContinueCard", layout, Vector2.zero, new Vector2(900f, 600f), Color.white);
        panel.sprite = panelSprite;
        panel.raycastTarget = false;

        // Replace the painted preview timer with a live strip in the same location.
        Image track = Box("CountdownTrack", panel.transform, ArtPosition(770f, 140f),
            ArtSize(1318f, 42f), new Color(0f, .25f, .55f, 1f));
        track.sprite = RoundedSprite(); track.type = Image.Type.Sliced;
        track.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        track.raycastTarget = false;
        Texture2D gradient = new Texture2D(256, 1, TextureFormat.RGBA32, false);
        gradient.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < 256; x++)
            gradient.SetPixel(x, 0, Color.Lerp(new Color(0f, .91f, 1f), new Color(.87f, 1f, .30f), x / 255f));
        gradient.Apply(false, true);
        timerSprite = Sprite.Create(gradient, new UnityEngine.Rect(0, 0, 256, 1), new Vector2(.5f, .5f), 100f);
        timerFill = Box("CountdownFill", track.transform, Vector2.zero, Vector2.zero, Color.white);
        timerFill.sprite = timerSprite;
        Stretch(timerFill.rectTransform);
        timerFill.raycastTarget = false;

        // The new capsule covers the complete old painted face and its rim.
        // Only this standalone button animates; the surrounding card stays still.
        RectTransform buttonRect = Rect("WatchAdButton", panel.transform,
            ArtPosition(769f, 680f), ArtSize(1200f, 310f));
        Image face = buttonRect.gameObject.AddComponent<Image>();
        BalloonDogMenuArtwork.ApplyButtonBackground(face, true);
        Text(buttonRect, "WatchAdLabel", "WATCH AD", Vector2.zero,
            buttonRect.sizeDelta - new Vector2(80f, 32f), 68f, Color.white);
        watch = buttonRect.gameObject.AddComponent<Button>();
        watch.targetGraphic = face;
        watch.navigation = new Navigation { mode = Navigation.Mode.None };
        watch.onClick.AddListener(WatchAd);
        BalloonDogDirectPressButton input = buttonRect.gameObject.AddComponent<BalloonDogDirectPressButton>();
        input.Configure(watch, buttonRect, new Vector2(155f / 1200f, .5f), false);

        // Transient ad status sits below the card; the approved bottom caption
        // remains untouched in the panel art.
        hint = Text(panel.transform, "ContinueHint", "", new Vector2(0f, -340f),
            new Vector2(930f, 64f), 28f, Color.white);
        hint.outlineColor = new Color(.01f, .12f, .26f, 1f);
        hint.outlineWidth = .18f;
    }

    // Source-space coordinates, measured from the top-left of the approved PNG.
    private static Vector2 ArtPosition(float x, float y)
    {
        return new Vector2(((x - 52f) / 1433f - .5f) * 900f,
            (.5f - (y - 80f) / 865f) * 600f);
    }
    private static Vector2 ArtSize(float width, float height)
    {
        return new Vector2(width / 1433f * 900f, height / 865f * 600f);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static Image Box(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        Image image = Rect(name, parent, pos, size).gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = false;
        return image;
    }
    private static TMP_Text Text(Transform parent, string name, string value, Vector2 pos, Vector2 size, float fontSize, Color color)
    {
        TextMeshProUGUI text = Rect(name, parent, pos, size).gameObject.AddComponent<TextMeshProUGUI>();
        BalloonDogTitanFont.Apply(text);
        text.text = value; text.fontSize = fontSize; text.color = color;
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        text.enableAutoSizing = true; text.fontSizeMin = fontSize * 0.7f; text.fontSizeMax = fontSize;
        return text;
    }
    private static Sprite RoundedSprite()
    {
        if (rounded != null) return rounded;
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Max(16f - x - .5f, x + .5f - 48f, 0f);
            float dy = Mathf.Max(16f - y - .5f, y + .5f - 48f, 0f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(16f - Mathf.Sqrt(dx * dx + dy * dy))));
        }
        tex.Apply(false, true);
        rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16,16,16,16));
        return rounded;
    }
    private void OnDestroy() { Close(); }
}
