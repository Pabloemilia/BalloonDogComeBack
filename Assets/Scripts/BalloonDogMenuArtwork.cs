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
            case "butonmavi": frame = new Rect(41f, 70f, 1968f, 546f); break;
            case "butonyesil": frame = new Rect(19f, 96f, 1995f, 582f); break;
            default: return Sprites[name] = source;
        }
        Sprite sprite = Sprite.Create(source.texture, frame, new Vector2(.5f, .5f),
            100f, 0, SpriteMeshType.FullRect);
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
}
