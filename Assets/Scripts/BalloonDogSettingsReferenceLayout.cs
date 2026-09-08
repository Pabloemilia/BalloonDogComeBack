using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies the supplied Settings reference layout after the runtime UI builder
/// creates ModernSettingsScreen. Existing button listeners and preferences are
/// preserved; this component changes presentation only.
/// </summary>
public sealed class BalloonDogSettingsReferenceLayout : MonoBehaviour
{
    private const string RuntimeName = "__BalloonDogSettingsReferenceLayout";
    private const string BlueButton = "Settings/Buttons/SettingsButtonBlue";
    private const string MintButton = "Settings/Buttons/SettingsButtonMint";
    private RectTransform currentScreen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (GameObject.Find(RuntimeName) != null)
        {
            return;
        }

        GameObject runtime = new GameObject(RuntimeName);
        DontDestroyOnLoad(runtime);
        runtime.AddComponent<BalloonDogSettingsReferenceLayout>();
    }

    private void LateUpdate()
    {
        RectTransform screen = FindSceneRect("ModernSettingsScreen");
        if (screen == null)
        {
            currentScreen = null;
            return;
        }

        if (currentScreen != screen)
        {
            currentScreen = screen;
            BuildReferenceLayout(screen);
        }

        if (screen.gameObject.activeInHierarchy)
        {
            RefreshToggle("MUSIC");
            RefreshToggle("SFX");
            RefreshToggle("VIBRATION");
        }
    }

    private static void BuildReferenceLayout(RectTransform screen)
    {
        SetChildRect(screen, "SettingsTitle", new Vector2(0f, 752f), new Vector2(820f, 150f));
        MoveTitleAccents(screen, 752f);

        ConfigureToggle(screen, "MUSIC", "Settings/Icons/MusicNote", 409f);
        ConfigureToggle(screen, "SFX", "Settings/Icons/VolumeHigh", 148f);
        ConfigureToggle(screen, "VIBRATION", "Settings/Icons/Vibrate", -111f);

        ConfigureAction(
            screen,
            "SettingsPrivacyButton",
            "Settings/Icons/ShieldLock",
            -415f,
            new Vector2(800f, 165f));
        ConfigureAction(
            screen,
            "SettingsClose",
            "Settings/Icons/CheckBold",
            -716f,
            new Vector2(750f, 165f));

        TMP_Text footer = FindChild<TMP_Text>(screen, "SettingsSafetyFooter");
        if (footer == null)
        {
            RectTransform footerRect = CreateRect("SettingsSafetyFooter", screen);
            footer = footerRect.gameObject.AddComponent<TextMeshProUGUI>();
            footer.text = "—  GAME IS SAFE  —";
            footer.color = new Color(0.02f, 0.48f, 0.58f, 0.82f);
            footer.alignment = TextAlignmentOptions.Center;
            footer.enableAutoSizing = true;
            footer.fontSizeMin = 22f;
            footer.fontSizeMax = 30f;
            footer.raycastTarget = false;
        }
        SetRect(footer.rectTransform, new Vector2(0f, -938f), new Vector2(520f, 58f));

        foreach (TMP_Text text in screen.GetComponentsInChildren<TMP_Text>(true))
        {
            BalloonDogTitanFont.Apply(text);
        }

        Sprite cloudSprite = Resources.Load<Sprite>("Settings/Icons/Cloud");
        if (cloudSprite != null)
        {
            foreach (Image image in screen.GetComponentsInChildren<Image>(true))
            {
                if (image.name.StartsWith("SettingsCloud_"))
                {
                    image.sprite = cloudSprite;
                    image.preserveAspect = true;
                }
            }
        }
    }

    private static void ConfigureToggle(
        RectTransform screen,
        string key,
        string iconPath,
        float y)
    {
        RectTransform stage = FindChild<RectTransform>(screen, key + "ToggleStage");
        RectTransform button = FindChild<RectTransform>(screen, key + "Toggle");
        if (stage == null || button == null)
        {
            return;
        }

        SetRect(stage, new Vector2(0f, y), new Vector2(800f, 165f));
        SetRect(button, new Vector2(0f, 5f), new Vector2(800f, 155f));

        Image outer = button.GetComponent<Image>();
        ApplySlicedSprite(outer, BlueButton);

        TMP_Text generated = FindChild<TMP_Text>(button, "Label");
        if (generated != null)
        {
            generated.gameObject.SetActive(false);
        }

        TMP_Text label = FindChild<TMP_Text>(button, key + "Label");
        if (label != null)
        {
            SetRect(label.rectTransform, new Vector2(-70f, 0f), new Vector2(390f, 112f));
            label.fontSizeMax = 52f;
            label.fontSizeMin = 36f;
        }

        Image icon = EnsureImage(button, key + "ReferenceIcon", iconPath);
        SetRect(icon.rectTransform, new Vector2(-282f, 0f), new Vector2(82f, 82f));

        Image stateBackground = EnsureImage(button, key + "ReferenceStateBackground", MintButton);
        stateBackground.type = Image.Type.Sliced;
        stateBackground.preserveAspect = false;
        SetRect(stateBackground.rectTransform, new Vector2(274f, 0f), new Vector2(190f, 104f));

        TMP_Text state = FindChild<TMP_Text>(button, key + "State");
        if (state != null)
        {
            state.transform.SetParent(stateBackground.transform, false);
            SetRect(state.rectTransform, Vector2.zero, new Vector2(170f, 88f));
            state.transform.SetAsLastSibling();
        }
    }

    private static void ConfigureAction(
        RectTransform screen,
        string buttonName,
        string iconPath,
        float y,
        Vector2 size)
    {
        RectTransform stage = FindChild<RectTransform>(screen, buttonName + "Stage");
        RectTransform button = FindChild<RectTransform>(screen, buttonName);
        if (stage == null || button == null)
        {
            return;
        }

        SetRect(stage, new Vector2(0f, y), size);
        SetRect(button, new Vector2(0f, 5f), new Vector2(size.x, size.y - 10f));

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            SetRect(label.rectTransform, new Vector2(55f, 0f), new Vector2(size.x - 220f, 125f));
        }

        Image icon = EnsureImage(button, buttonName + "ReferenceIcon", iconPath);
        SetRect(icon.rectTransform, new Vector2(-size.x * 0.31f, 0f), new Vector2(76f, 76f));
    }

    private static void RefreshToggle(string key)
    {
        RectTransform button = FindSceneRect(key + "Toggle");
        if (button == null || !IsInsideSettings(button))
        {
            return;
        }

        ApplySlicedSprite(button.GetComponent<Image>(), BlueButton);
        TMP_Text state = FindChild<TMP_Text>(button, key + "State");
        Image capsule = FindChild<Image>(button, key + "ReferenceStateBackground");
        if (state != null && capsule != null)
        {
            ApplySlicedSprite(capsule, state.text.Trim().ToUpperInvariant() == "ON" ? MintButton : BlueButton);
        }
    }

    private static Image EnsureImage(Transform parent, string name, string resourcePath)
    {
        Image image = FindChild<Image>(parent, name);
        if (image == null)
        {
            RectTransform rect = CreateRect(name, parent);
            image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            image.sprite = sprite;
        }
        image.color = Color.white;
        image.preserveAspect = true;
        return image;
    }

    private static void ApplySlicedSprite(Image image, string resourcePath)
    {
        if (image == null)
        {
            return;
        }
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            image.sprite = sprite;
        }
        image.type = Image.Type.Sliced;
        image.color = Color.white;
    }

    private static void MoveTitleAccents(RectTransform screen, float titleY)
    {
        int index = 0;
        foreach (RectTransform rect in screen.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name != "PauseTitleAccent")
            {
                continue;
            }
            int sideIndex = index / 3;
            int rayIndex = index % 3;
            int side = sideIndex == 0 ? -1 : 1;
            rect.anchoredPosition = new Vector2(
                side * (415f + rayIndex * 13f),
                titleY + (rayIndex - 1) * 31f);
            index++;
        }
    }

    private static void SetChildRect(RectTransform root, string name, Vector2 position, Vector2 size)
    {
        RectTransform rect = FindChild<RectTransform>(root, name);
        if (rect != null)
        {
            SetRect(rect, position, size);
        }
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

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject instance = new GameObject(name, typeof(RectTransform));
        instance.transform.SetParent(parent, false);
        return instance.GetComponent<RectTransform>();
    }

    private static T FindChild<T>(Transform root, string name) where T : Component
    {
        foreach (T component in root.GetComponentsInChildren<T>(true))
        {
            if (component.name == name)
            {
                return component;
            }
        }
        return null;
    }

    private static RectTransform FindSceneRect(string name)
    {
        foreach (RectTransform rect in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (rect != null && rect.name == name && rect.gameObject.scene.IsValid())
            {
                return rect;
            }
        }
        return null;
    }

    private static bool IsInsideSettings(Transform candidate)
    {
        for (Transform current = candidate; current != null; current = current.parent)
        {
            if (current.name == "ModernSettingsScreen")
            {
                return true;
            }
        }
        return false;
    }
}
