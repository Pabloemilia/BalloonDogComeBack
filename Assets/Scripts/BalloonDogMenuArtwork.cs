using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared supplied artwork for the authored view and interactive menu.</summary>
public static class BalloonDogMenuArtwork
{
    private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

    public static Sprite Get(string name)
    {
        if (Sprites.TryGetValue(name, out Sprite cached) && cached != null) return cached;
        Sprite source = Resources.Load<Sprite>("MenuRefresh/" + name);
        if (source == null) return null;

        // Sprite frames exclude only the transparent export padding. The PNGs
        // remain unchanged; no pixel reads or readable texture copies are needed.
        Rect frame;
        switch (name)
        {
            case "settings_button": frame = new Rect(196f, 166f, 909f, 870f); break;
            case "coin_panel_background": frame = new Rect(99f, 94f, 1841f, 578f); break;
            case "butonmavi": frame = new Rect(75f, 72f, 1898f, 520f); break;
            case "butonyesil": frame = new Rect(50f, 80f, 1954f, 511f); break;
            case "best_button": frame = new Rect(94f, 68f, 1873f, 500f); break;
            case "air_wind_icon": frame = new Rect(265f, 251f, 729f, 700f); break;
            default: return Sprites[name] = source;
        }
        bool button = name == "butonmavi" || name == "butonyesil" || name == "best_button";
        Sprite sprite = Sprite.Create(source.texture, frame, new Vector2(.5f, .5f),
            100f, 0, SpriteMeshType.FullRect, button ? new Vector4(260f, 95f, 260f, 95f) : Vector4.zero);
        sprite.name = name + " UI Frame";
        return Sprites[name] = sprite;
    }

    public static void Apply(RectTransform rect, string name, bool interactive = false)
    {
        if (rect == null) return;
        Sprite sprite = Get(name);
        if (sprite == null) return;
        Image image = rect.GetComponent<Image>();
        if (image == null) image = rect.gameObject.AddComponent<Image>();
        if (name == "butonmavi" || name == "butonyesil" || name == "best_button")
        {
            ApplyButtonSprite(image, sprite);
            image.raycastTarget = interactive;
            return;
        }
        image.enabled = true;
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = interactive;

        // The supplied PNG already contains its highlights, rim and shadow.
        foreach (Shadow effect in rect.GetComponents<Shadow>()) effect.enabled = false;
        foreach (string child in new[] { "ThemeTopGloss", "ThemeBottomShade" })
        {
            Transform overlay = rect.Find(child);
            if (overlay != null) overlay.gameObject.SetActive(false);
        }
        Button button = rect.GetComponent<Button>();
        if (button != null) button.targetGraphic = image;
    }

    public static bool ApplyButtonBackground(Image image, bool green)
    {
        Sprite sprite = Get(green ? "butonyesil" : "butonmavi");
        if (image == null || sprite == null) return false;
        ApplyButtonSprite(image, sprite);
        return true;
    }

    public static bool ApplyButtonColor(Image image, Color color)
    {
        if (color.a < .1f) return false;
        bool green = color.g > color.r * 1.15f && color.g > color.b * 1.15f;
        bool blue = color.b > color.r * 1.15f && color.b >= color.g * .9f && color.g > color.r;
        return (green || blue) && ApplyButtonBackground(image, green);
    }

    private static void ApplyButtonSprite(Image image, Sprite sprite)
    {
        image.enabled = true;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        float height = image.rectTransform.rect.height;
        if (height <= 0f) height = image.rectTransform.sizeDelta.y;
        image.pixelsPerUnitMultiplier = sprite.rect.height / Mathf.Max(1f, height);
        image.color = Color.white;
        image.material = null;
        UiVerticalGradient gradient = image.GetComponent<UiVerticalGradient>();
        if (gradient != null) gradient.enabled = false;
        foreach (Shadow shadow in image.GetComponents<Shadow>()) shadow.enabled = false;
        foreach (string name in new[] { "ThemeTopGloss", "ThemeBottomShade" })
        {
            Transform overlay = image.transform.Find(name);
            if (overlay != null) overlay.gameObject.SetActive(false);
        }
        Button button = image.GetComponent<Button>();
        if (button != null)
        {
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
            colors.pressedColor = new Color(.92f, .95f, 1f, 1f);
            button.colors = colors;
        }
    }
}
