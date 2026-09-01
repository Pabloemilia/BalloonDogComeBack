using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Replaces only the legacy SFX and VIBRATION rows with clean standalone
/// buttons built directly from the supplied blue and mint sprites.
/// </summary>
public sealed class BalloonDogSettingsToggleVisualFix : MonoBehaviour
{
    private const string RuntimeName =
        "__BalloonDogSettingsToggleVisualFix";
    private const string BlueButtonPath =
        "PauseMenu/Buttons/PauseButtonBlueClean";
    private const string MintButtonPath =
        "PauseMenu/Buttons/PauseButtonMintClean";

    private sealed class FreshToggle
    {
        public Button LegacyButton;
        public TMP_Text LegacyState;
        public TMP_Text State;
        public Image Background;
    }

    private FreshToggle sfxToggle;
    private FreshToggle vibrationToggle;
    private float nextRefreshTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (GameObject.Find(RuntimeName) != null)
        {
            return;
        }

        GameObject runtime = new GameObject(RuntimeName);
        DontDestroyOnLoad(runtime);
        runtime.AddComponent<BalloonDogSettingsToggleVisualFix>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + 0.15f;
        sfxToggle = EnsureFreshToggle("SFX", sfxToggle);
        vibrationToggle =
            EnsureFreshToggle("VIBRATION", vibrationToggle);
        RefreshToggle(sfxToggle);
        RefreshToggle(vibrationToggle);
    }

    private static FreshToggle EnsureFreshToggle(
        string controlName,
        FreshToggle existing)
    {
        if (existing != null && existing.Background != null)
        {
            return existing;
        }

        RectTransform legacyRow =
            FindSettingsRect(controlName + "Row");
        Button legacyButton =
            FindSettingsButton(controlName + "Toggle");
        if (legacyRow == null || legacyButton == null)
        {
            return existing;
        }

        RectTransform alreadyCreated =
            FindSettingsRect(controlName + "FreshToggle");
        if (alreadyCreated != null)
        {
            return new FreshToggle
            {
                LegacyButton = legacyButton,
                LegacyState = legacyButton.GetComponentInChildren<TMP_Text>(true),
                State = FindChildText(alreadyCreated, "State"),
                Background = alreadyCreated.GetComponent<Image>()
            };
        }

        TMP_Text legacyState =
            legacyButton.GetComponentInChildren<TMP_Text>(true);
        RectTransform buttonRect = CreateRect(
            controlName + "FreshToggle",
            legacyRow.parent);
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = legacyRow.anchoredPosition;
        buttonRect.sizeDelta = new Vector2(900f, 150f);
        buttonRect.localScale = Vector3.one;
        buttonRect.SetSiblingIndex(legacyRow.GetSiblingIndex());

        Image background = buttonRect.gameObject.AddComponent<Image>();
        background.type = Image.Type.Sliced;
        background.color = Color.white;
        background.raycastTarget = true;

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.86f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.60f, 0.75f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(() => legacyButton.onClick.Invoke());
        buttonRect.gameObject.AddComponent<MenuPressScale>();

        TMP_Text label = CreateLabel(
            buttonRect,
            "Label",
            controlName,
            new Vector2(-255f, 2f),
            new Vector2(410f, 105f),
            48f);
        label.alignment = TextAlignmentOptions.Center;

        TMP_Text state = CreateLabel(
            buttonRect,
            "State",
            legacyState != null ? legacyState.text : "OFF",
            new Vector2(350f, 2f),
            new Vector2(190f, 105f),
            45f);
        state.alignment = TextAlignmentOptions.Center;

        legacyRow.gameObject.SetActive(false);
        return new FreshToggle
        {
            LegacyButton = legacyButton,
            LegacyState = legacyState,
            State = state,
            Background = background
        };
    }

    private static void RefreshToggle(FreshToggle toggle)
    {
        if (toggle == null || toggle.Background == null)
        {
            return;
        }

        string stateText = toggle.LegacyState != null
            ? toggle.LegacyState.text.Trim().ToUpperInvariant()
            : "OFF";
        bool enabled = stateText == "ON";
        if (toggle.State != null)
        {
            toggle.State.text = enabled ? "ON" : "OFF";
        }

        ApplyButtonSprite(
            toggle.Background,
            enabled ? MintButtonPath : BlueButtonPath);
    }

    private static void ApplyButtonSprite(
        Image background,
        string resourcePath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null && background.sprite != sprite)
        {
            background.sprite = sprite;
            background.type = Image.Type.Sliced;
        }

        background.color = Color.white;
        Material material =
            Resources.Load<Material>(resourcePath + "Satin");
        background.material =
            material != null && material.shader != null &&
            material.shader.isSupported
                ? material
                : null;
    }

    private static TMP_Text CreateLabel(
        Transform parent,
        string name,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;

        TextMeshProUGUI text =
            rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 32f;
        text.fontSizeMax = fontSize;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        BalloonDogTitanFont.Apply(text);

        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.01f, 0.16f, 0.42f, 0.34f);
        shadow.effectDistance = new Vector2(0f, -3f);
        shadow.useGraphicAlpha = true;
        return text;
    }

    private static TMP_Text FindChildText(
        Transform parent,
        string childName)
    {
        foreach (TMP_Text text in parent.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == childName)
            {
                return text;
            }
        }

        return null;
    }

    private static Button FindSettingsButton(string objectName)
    {
        RectTransform rect = FindSettingsRect(objectName);
        return rect != null ? rect.GetComponent<Button>() : null;
    }

    private static RectTransform FindSettingsRect(string objectName)
    {
        foreach (RectTransform candidate in
                 Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (candidate == null || candidate.name != objectName ||
                !candidate.gameObject.scene.IsValid() ||
                !IsInsideSettings(candidate))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private static bool IsInsideSettings(Transform candidate)
    {
        Transform current = candidate;
        while (current != null)
        {
            if (current.name == "ModernSettingsScreen" ||
                current.name == "SettingsPanel")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }
}
