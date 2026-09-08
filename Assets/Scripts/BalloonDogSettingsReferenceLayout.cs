using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Unity.VectorGraphics;

/// <summary>Settings-only layout measured in the supplied 941 x 1672 reference.</summary>
public sealed class BalloonDogSettingsReferenceLayout : MonoBehaviour
{
    public TMP_Text MusicState { get; private set; }
    public TMP_Text SoundState { get; private set; }
    public TMP_Text VibrationState { get; private set; }
    private RectTransform content;
    private Vector2 lastSize;
    private RectTransform backdrop;
    private Sprite blue;
    private Sprite mint;
    private TMP_FontAsset font;

    public void Build(UnityAction music, UnityAction sound, UnityAction vibration,
        UnityAction privacy, UnityAction done)
    {
        font = BalloonDogTitanFont.Get();
        if (font == null) throw new InvalidOperationException("Settings requires the supplied Titan One font.");
        blue = ButtonSprite("Settings/Buttons/SettingsButtonBlue", new Rect(40f, 60f, 1970f, 560f), 390f, new Vector4(240f, 270f, 240f, 240f));
        mint = ButtonSprite("Settings/Buttons/SettingsButtonMint", new Rect(20f, 75f, 1993f, 600f), 420f, new Vector4(290f, 275f, 290f, 275f));
        // Background is independent of the fitted reference composition.
        backdrop = Rect("SettingsBackground", transform, 470.5f, 836f, 941f, 1672f);
        var background = backdrop.gameObject.AddComponent<BalloonDogSettingsBackdrop>();
        background.raycastTarget = true;
        content = Rect("SettingsReferenceContent", transform, 470.5f, 836f, 941f, 1672f);
        content.anchoredPosition = Vector2.zero;
        Decorations();
        Text("SettingsTitle", content, "SETTINGS", 470.5f, 307f, 575f, 126f, 108f, TextAlignmentOptions.Center);
        for (int side = -1; side <= 1; side += 2)
        for (int index = 0; index < 3; index++)
        {
            float x = side < 0 ? 84f + index * 10f : 857f - index * 10f;
            var ray = Pill("TitleAccent", content, x, 279f + index * 27f, 38f, 10f, Color.white, Color.white);
            ray.rectTransform.localEulerAngles = new Vector3(0f, 0f, side * (index - 1) * 23f);
        }
        MusicState = Toggle("MUSIC", "MusicNote", 549f, music);
        SoundState = Toggle("SFX", "VolumeHigh", 735f, sound);
        VibrationState = Toggle("VIBRATION", "Vibrate", 921f, vibration);
        Action("SettingsPrivacyButton", "PRIVACY", "ShieldLock", 1136f, 698f, blue, privacy);
        Action("SettingsClose", "DONE", "CheckBold", 1349f, 656f, mint, done);
        var footer = Text("SettingsSafetyFooter", content, "GAME IS SAFE", 470.5f, 1500f, 180f, 32f, 22f, TextAlignmentOptions.Center, false);
        footer.color = new Color32(20, 143, 150, 255);
        Pill("FooterLeftLine", content, 364f, 1501f, 23f, 4f, footer.color, footer.color);
        Pill("FooterRightLine", content, 572f, 1501f, 23f, 4f, footer.color, footer.color);
        Fit();
    }

    private TMP_Text Toggle(string label, string icon, float y, UnityAction action)
    {
        Button button = Button(label + "Toggle", y, 698f, blue, action);
        // Child coordinates use the reference viewport too, with y=836 at the button centre.
        Icon(label + "Icon", button.transform, icon, 238f, 836f, 98f, 90f);
        Text(label + "Label", button.transform, label, 467f, 836f, 260f, 76f,
            label == "VIBRATION" ? 44f : 49f, TextAlignmentOptions.Left);
        var rim = Pill(label + "StateCapsule", button.transform, 686f, 836f, 170f, 84f,
            new Color32(132, 234, 255, 255), new Color32(63, 207, 244, 255));
        Pill(label + "StateFill", rim.transform, 470.5f, 836f, 155f, 69f,
            new Color32(53, 191, 244, 255), new Color32(0, 160, 211, 255));
        return Text(label + "State", rim.transform, "ON", 470.5f, 836f, 140f, 65f, 44f, TextAlignmentOptions.Center);
    }

    private void Action(string name, string label, string icon, float y, float width, Sprite sprite, UnityAction action)
    {
        Button button = Button(name, y, width, sprite, action);
        Icon(name + "Icon", button.transform, icon, 283f, 836f, 88f, 90f);
        Text(name + "Label", button.transform, label, 516f, 836f, 310f, 88f, 51f, TextAlignmentOptions.Center);
    }

    private Button Button(string name, float y, float width, Sprite sprite, UnityAction action)
    {
        var rect = Rect(name, content, 470.5f, y, width, 144f);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(action);
        return button;
    }

    private TMP_Text Text(string name, Transform parent, string value, float x, float y,
        float width, float height, float fontSize, TextAlignmentOptions alignment, bool shadow = true)
    {
        var rect = Rect(name, parent, x, y, width, height);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontStyle = FontStyles.Normal;
        text.fontWeight = FontWeight.Regular;
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Truncate;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.margin = Vector4.zero;
        if (shadow)
        {
            var effect = rect.gameObject.AddComponent<Shadow>();
            effect.effectColor = new Color(0.02f, 0.29f, 0.50f, 0.30f);
            effect.effectDistance = new Vector2(0f, -3f);
        }
        return text;
    }

    private static SVGImage Icon(string name, Transform parent, string resource, float x, float y, float width, float height)
    {
        Sprite sprite = RequiredSprite("Settings/Icons/" + resource);
        var rect = Rect(name, parent, x, y, width, height);
        var graphic = rect.gameObject.AddComponent<SVGImage>();
        graphic.sprite = sprite;
        graphic.preserveAspect = true;
        graphic.color = Color.white;
        graphic.raycastTarget = false;
        // The originals use CSS currentColor. Apply the reference's white UI tint
        // after tessellation, retaining alpha and every source path unchanged.
        rect.gameObject.AddComponent<BalloonDogSettingsSvgTint>();
        return graphic;
    }

    private void Decorations()
    {
        Cloud(1, 267f, 130f, 163f, 94f, -4f);
        Cloud(2, 828f, 245f, 114f, 70f, 7f);
        Cloud(3, 70f, 555f, 137f, 90f, -3f);
        Cloud(4, 875f, 929f, 111f, 64f, 7f);
        Cloud(5, 142f, 1344f, 139f, 108f, 5f);
        Cloud(6, 746f, 1508f, 125f, 79f, -3f);
        Cloud(7, 840f, 1535f, 120f, 67f, -4f);
        Sprite dog = RequiredSprite("PauseMenu/Decor/BalloonDogSilhouette");
        Dog(dog, 1, 102f, 258f, 137f, 137f, 12f);
        Dog(dog, 2, 842f, 444f, 121f, 107f, 12f);
        Dog(dog, 3, 91f, 801f, 151f, 144f, 10f);
        Dog(dog, 4, 863f, 1130f, 124f, 112f, 10f);
        Dog(dog, 5, 187f, 1515f, 151f, 126f, 12f);
        Dog(dog, 6, 785f, 1418f, 151f, 107f, 2f);
    }

    private void Cloud(int index, float x, float y, float width, float height, float rotation)
    {
        var cloud = Icon("SettingsCloud_" + index, content, "Cloud", x, y, width, height);
        cloud.color = new Color(1f, 1f, 1f, 0.20f);
        cloud.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotation);
    }

    private void Dog(Sprite sprite, int index, float x, float y, float width, float height, float rotation)
    {
        var rect = Rect("SettingsBalloonDog_" + index, content, x, y, width, height);
        rect.localEulerAngles = new Vector3(0f, 0f, rotation);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = new Color(1f, 1f, 1f, 0.18f);
        image.raycastTarget = false;
    }

    private static BalloonDogSettingsPill Pill(string name, Transform parent, float x, float y,
        float width, float height, Color top, Color bottom)
    {
        var rect = Rect(name, parent, x, y, width, height);
        var pill = rect.gameObject.AddComponent<BalloonDogSettingsPill>();
        pill.Top = top;
        pill.Bottom = bottom;
        pill.raycastTarget = false;
        return pill;
    }

    private static Sprite ButtonSprite(string path, Rect region, float pixelsPerUnit, Vector4 border)
    {
        var texture = Resources.Load<Texture2D>(path);
        if (texture == null) throw new InvalidOperationException("Settings button texture missing: " + path);
        // Sprite rectangle trims only transparent export padding; source PNG bytes are unchanged.
        return Sprite.Create(texture, region, new Vector2(0.5f, 0.5f), pixelsPerUnit,
            0, SpriteMeshType.FullRect, border);
    }
    private void OnDestroy()
    {
        if (blue != null) Destroy(blue);
        if (mint != null) Destroy(mint);
    }

    private static Sprite RequiredSprite(string path)
    {
        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Settings sprite not imported: " + path);
        return sprite;
    }

    private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x - 470.5f, 836f - y);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        return rect;
    }

    private void OnEnable() { Fit(); }
    private void OnRectTransformDimensionsChange() { Fit(); }
    private void LateUpdate()
    {
        if (((RectTransform)transform).rect.size != lastSize) Fit();
    }
    private void Fit()
    {
        if (content == null) return;
        lastSize = ((RectTransform)transform).rect.size;
        var canvas = GetComponentInParent<Canvas>();
        if (backdrop != null && canvas != null)
        {
            var corners = new Vector3[4];
            ((RectTransform)canvas.transform).GetWorldCorners(corners);
            Vector3 min = transform.InverseTransformPoint(corners[0]);
            Vector3 max = transform.InverseTransformPoint(corners[2]);
            backdrop.anchoredPosition = (min + max) * 0.5f;
            backdrop.sizeDelta = max - min;
        }
        // One uniform reference scale; every child retains (1,1,1). Safe Area is
        // already applied by the parent, so notches and navigation bars are excluded.
        float scale = Mathf.Min(lastSize.x / 941f, lastSize.y / 1672f);
        content.localScale = Vector3.one * Mathf.Max(0.001f, scale);
    }
}
