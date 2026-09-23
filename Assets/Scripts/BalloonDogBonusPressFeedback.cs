using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Keeps the supplied sprite sharp while giving the bonus button a pressed state.
[RequireComponent(typeof(Image))]
public sealed class BalloonDogBonusPressFeedback : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private Image image;
    private RectTransform rect;

    private void Awake()
    {
        image = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
    }

    private void OnDisable()
    {
        Restore();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rect.localScale = Vector3.one * 0.95f;
        image.color = new Color(0.62f, 0.69f, 0.64f, 1f);
        foreach (BalloonDogStarGlint star in GetComponentsInChildren<BalloonDogStarGlint>())
            star.Burst();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Restore();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Restore();
    }

    private void Restore()
    {
        if (rect != null) rect.localScale = Vector3.one;
        if (image != null) image.color = Color.white;
    }
}

 
// Soft, separate glints over the painted stars. Uses unscaled time so the
// result screen continues to sparkle when gameplay time is paused.
[RequireComponent(typeof(Image))]
public sealed class BalloonDogStarGlint : MonoBehaviour
{
    private static Sprite sprite;
    private Image image;
    private RectTransform rect;
    private float phase;
    private bool bonusStar;
    private float burstUntil;
    private Vector2 origin;

    public static Sprite Sprite
    {
        get
        {
            if (sprite != null) return sprite;
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "GameOverSoftStar";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                float dy = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                // A single filled four-point star with curved inward sides.
                // This matches the gold stars in the title art instead of
                // drawing two thin lines that resemble a plus sign.
                float reach = Mathf.Sqrt(Mathf.Abs(dx)) +
                    Mathf.Sqrt(Mathf.Abs(dy));
                float alpha = Mathf.Clamp01((1f - reach) * 14f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            return sprite;
        }
    }

    public void Configure(float offset, bool belongsToBonus)
    {
        phase = offset;
        bonusStar = belongsToBonus;
    }

    public void Burst()
    {
        burstUntil = Time.unscaledTime + 0.28f;
    }

    private void Awake()
    {
        image = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
        origin = rect.anchoredPosition;
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        float wave = 0.5f + 0.5f * Mathf.Sin(t * (bonusStar ? 2.7f : 2.2f) + phase * 2.73f);
        // The title gets a short accent once every few seconds.
        float accent = bonusStar ? 0f :
            Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 1.4f + phase)), 18f);
        float burst = Mathf.Clamp01((burstUntil - t) / 0.28f);
        float glow = Mathf.Clamp01(0.30f + 0.47f * wave + 0.33f * accent + 0.72f * burst);
        image.color = new Color(1f, 0.83f, 0.39f, glow);
        rect.localScale = Vector3.one *
            (0.86f + 0.20f * wave + 0.26f * accent + 0.22f * burst);
        // Two slow frequencies per axis give each star a small diagonal drift.
        float x = Mathf.Sin(t * 0.83f + phase * 1.7f) * 7f +
            Mathf.Sin(t * 1.31f + phase) * 3f;
        float y = Mathf.Cos(t * 0.69f + phase * 2.1f) * 6f +
            Mathf.Sin(t * 1.17f + phase * 0.7f) * 3f;
        rect.anchoredPosition = origin + new Vector2(x, y);
    }
}
