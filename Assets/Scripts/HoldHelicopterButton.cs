using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class HoldHelicopterButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [Header("Ability")]
    [SerializeField] private PlayerFormController formController;
    [SerializeField, Min(0.1f)] private float activeDuration = 1.25f;
    [SerializeField, Min(0f)] private float cooldownDuration = 2.25f;

    [Header("Optional UI References")]
    [SerializeField] private Image buttonBackground;
    [SerializeField] private Image helicopterIcon;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text cooldownText;

    private bool helicopterActive;
    private float effectEndsAt;
    private float cooldownEndsAt;

    private static Sprite cachedCircleSprite;
    private static Sprite cachedHelicopterSprite;

    private void Awake()
    {
        ResolveReferences();
        BuildRoundButtonUi();
        RefreshUi();
    }

    private void Update()
    {
        float now = Time.unscaledTime;

        if (helicopterActive && now >= effectEndsAt)
        {
            helicopterActive = false;

            if (formController != null)
            {
                formController.SetHelicopterRequested(false);
            }
        }

        RefreshUi();
    }

    public void Configure(PlayerFormController controller)
    {
        formController = controller;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TryActivate();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Yeni sistem tek dokunuşla çalışır; parmağı kaldırmak yeteneği kapatmaz.
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Pointer dışarı çıksa da etkinlik kendi süresini tamamlar.
    }

    public bool TryActivate()
    {
        ResolveReferences();

        float now = Time.unscaledTime;

        if (formController == null ||
            now < cooldownEndsAt ||
            (GameManager.Instance != null && GameManager.Instance.IsGameOver))
        {
            return false;
        }

        helicopterActive = true;
        effectEndsAt = now + activeDuration;
        cooldownEndsAt = effectEndsAt + cooldownDuration;

        formController.SetHelicopterRequested(true);
        RefreshUi();
        return true;
    }

    private void OnDisable()
    {
        helicopterActive = false;
        effectEndsAt = 0f;
        cooldownEndsAt = 0f;

        if (formController != null)
        {
            formController.SetHelicopterRequested(false);
        }
    }

    private void ResolveReferences()
    {
        if (formController == null)
        {
            formController = FindAnyObjectByType<PlayerFormController>();
        }

        buttonBackground ??= GetComponent<Image>();
    }

    private void BuildRoundButtonUi()
    {
        RectTransform rootRect = transform as RectTransform;
        if (rootRect != null &&
            rootRect.anchorMin == rootRect.anchorMax)
        {
            rootRect.sizeDelta = new Vector2(128f, 128f);
        }

        Sprite circle = GetCircleSprite();

        if (buttonBackground == null)
        {
            buttonBackground = gameObject.AddComponent<Image>();
        }

        buttonBackground.sprite = circle;
        buttonBackground.type = Image.Type.Simple;
        buttonBackground.color = new Color(0.12f, 0.75f, 0.96f, 0.98f);
        buttonBackground.raycastTarget = true;

        if (helicopterIcon == null)
        {
            Transform existing = transform.Find("HelicopterIcon");
            if (existing != null)
            {
                helicopterIcon = existing.GetComponent<Image>();
            }
        }

        if (helicopterIcon == null)
        {
            GameObject iconObject = new GameObject(
                "HelicopterIcon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            iconObject.transform.SetParent(transform, false);
            helicopterIcon = iconObject.GetComponent<Image>();

            RectTransform iconRect = helicopterIcon.rectTransform;
            iconRect.anchorMin = new Vector2(0.18f, 0.18f);
            iconRect.anchorMax = new Vector2(0.82f, 0.82f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
        }

        helicopterIcon.sprite = GetHelicopterSprite();
        helicopterIcon.preserveAspect = true;
        helicopterIcon.raycastTarget = false;
        helicopterIcon.color = Color.white;

        if (cooldownOverlay == null)
        {
            Transform existing = transform.Find("CooldownOverlay");
            if (existing != null)
            {
                cooldownOverlay = existing.GetComponent<Image>();
            }
        }

        if (cooldownOverlay == null)
        {
            GameObject overlayObject = new GameObject(
                "CooldownOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            overlayObject.transform.SetParent(transform, false);
            cooldownOverlay = overlayObject.GetComponent<Image>();

            RectTransform overlayRect = cooldownOverlay.rectTransform;
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
        }

        cooldownOverlay.sprite = circle;
        cooldownOverlay.type = Image.Type.Filled;
        cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
        cooldownOverlay.fillOrigin = 2;
        cooldownOverlay.fillClockwise = false;
        cooldownOverlay.color = new Color(0.02f, 0.08f, 0.16f, 0.68f);
        cooldownOverlay.raycastTarget = false;

        if (cooldownText == null)
        {
            Transform existing = transform.Find("CooldownText");
            if (existing != null)
            {
                cooldownText = existing.GetComponent<TMP_Text>();
            }
        }

        if (cooldownText == null)
        {
            GameObject textObject = new GameObject(
                "CooldownText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            textObject.transform.SetParent(transform, false);
            cooldownText = textObject.GetComponent<TextMeshProUGUI>();

            RectTransform textRect = cooldownText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        cooldownText.alignment = TextAlignmentOptions.Center;
        cooldownText.fontStyle = FontStyles.Bold;
        cooldownText.fontSize = 34f;
        cooldownText.color = Color.white;
        cooldownText.raycastTarget = false;
    }

    private void RefreshUi()
    {
        if (cooldownOverlay == null || cooldownText == null)
        {
            return;
        }

        float now = Time.unscaledTime;

        if (helicopterActive)
        {
            float remaining = Mathf.Max(0f, effectEndsAt - now);
            cooldownOverlay.fillAmount = 0f;
            cooldownText.text = remaining > 0.05f
                ? remaining.ToString("0.0")
                : string.Empty;

            if (buttonBackground != null)
            {
                buttonBackground.color =
                    new Color(0.98f, 0.72f, 0.16f, 1f);
            }

            return;
        }

        if (now < cooldownEndsAt)
        {
            float remaining = cooldownEndsAt - now;
            float denominator = Mathf.Max(0.01f, cooldownDuration);

            cooldownOverlay.fillAmount = Mathf.Clamp01(
                remaining / denominator);

            cooldownText.text = Mathf.CeilToInt(remaining).ToString();

            if (buttonBackground != null)
            {
                buttonBackground.color =
                    new Color(0.35f, 0.55f, 0.65f, 0.95f);
            }

            return;
        }

        cooldownOverlay.fillAmount = 0f;
        cooldownText.text = string.Empty;

        if (buttonBackground != null)
        {
            buttonBackground.color =
                new Color(0.12f, 0.75f, 0.96f, 0.98f);
        }
    }

    private static Sprite GetCircleSprite()
    {
        if (cachedCircleSprite != null)
        {
            return cachedCircleSprite;
        }

        const int size = 128;
        Texture2D texture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false);

        texture.name = "RuntimeCircle";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;
        float feather = 2.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(
                    new Vector2(x, y),
                    center);

                float alpha = Mathf.Clamp01(
                    (radius - distance) / feather);

                pixels[y * size + x] =
                    new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        cachedCircleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f);

        return cachedCircleSprite;
    }

    private static Sprite GetHelicopterSprite()
    {
        if (cachedHelicopterSprite != null)
        {
            return cachedHelicopterSprite;
        }

        Texture2D texture = Resources.Load<Texture2D>("helicopter_icon");
        if (texture == null)
        {
            return null;
        }

        cachedHelicopterSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        return cachedHelicopterSprite;
    }
}
