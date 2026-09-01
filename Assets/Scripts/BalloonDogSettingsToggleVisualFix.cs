using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps only the SFX and VIBRATION controls visually identical to MUSIC.
/// It also supports the older serialized Settings row hierarchy.
/// </summary>
public sealed class BalloonDogSettingsToggleVisualFix : MonoBehaviour
{
    private const string RuntimeName =
        "__BalloonDogSettingsToggleVisualFix";
    private const string BlueButtonPath =
        "PauseMenu/Buttons/PauseButtonBlueClean";
    private const string MintButtonPath =
        "PauseMenu/Buttons/PauseButtonMintClean";

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

        nextRefreshTime = Time.unscaledTime + 0.2f;
        MatchMusicButton("SFX");
        MatchMusicButton("VIBRATION");
    }

    private static void MatchMusicButton(string controlName)
    {
        Button musicButton = FindSettingsButton("MUSICToggle");
        Button targetButton = FindSettingsButton(controlName + "Toggle");
        if (musicButton == null || targetButton == null)
        {
            return;
        }

        RectTransform musicRect = musicButton.transform as RectTransform;
        RectTransform targetRect = targetButton.transform as RectTransform;
        if (musicRect == null || targetRect == null)
        {
            return;
        }

        targetRect.anchorMin = musicRect.anchorMin;
        targetRect.anchorMax = musicRect.anchorMax;
        targetRect.pivot = musicRect.pivot;
        targetRect.anchoredPosition = musicRect.anchoredPosition;
        targetRect.sizeDelta = musicRect.sizeDelta;
        targetRect.localScale = musicRect.localScale;
        targetRect.localRotation = musicRect.localRotation;
        targetRect.SetAsFirstSibling();

        RectTransform row = FindSettingsRect(controlName + "Row");
        if (row != null && targetRect.rect.width < 500f)
        {
            targetRect.anchorMin = new Vector2(0.5f, 0.5f);
            targetRect.anchorMax = new Vector2(0.5f, 0.5f);
            targetRect.pivot = new Vector2(0.5f, 0.5f);
            targetRect.anchoredPosition = Vector2.zero;
            targetRect.sizeDelta = new Vector2(900f, 150f);
        }

        if (row != null)
        {
            Image rowImage = row.GetComponent<Image>();
            if (rowImage != null)
            {
                rowImage.color = Color.clear;
                rowImage.raycastTarget = false;
            }

            foreach (Shadow decoration in row.GetComponents<Shadow>())
            {
                decoration.enabled = false;
            }
        }

        TMP_Text musicState =
            musicButton.GetComponentInChildren<TMP_Text>(true);
        TMP_Text targetState =
            targetButton.GetComponentInChildren<TMP_Text>(true);
        if (musicState != null && targetState != null)
        {
            RectTransform musicStateRect = musicState.rectTransform;
            RectTransform targetStateRect = targetState.rectTransform;
            targetStateRect.anchorMin = musicStateRect.anchorMin;
            targetStateRect.anchorMax = musicStateRect.anchorMax;
            targetStateRect.pivot = musicStateRect.pivot;
            targetStateRect.anchoredPosition =
                musicStateRect.anchoredPosition;
            targetStateRect.sizeDelta = musicStateRect.sizeDelta;
            targetStateRect.localScale = musicStateRect.localScale;
            targetState.font = musicState.font;
            targetState.fontSharedMaterial = musicState.fontSharedMaterial;
            targetState.fontSize = musicState.fontSize;
            targetState.fontStyle = musicState.fontStyle;
            targetState.alignment = musicState.alignment;
            targetState.color = musicState.color;

            if (row != null)
            {
                targetStateRect.anchorMin = new Vector2(0.5f, 0.5f);
                targetStateRect.anchorMax = new Vector2(0.5f, 0.5f);
                targetStateRect.pivot = new Vector2(0.5f, 0.5f);
                targetStateRect.anchoredPosition = new Vector2(260f, 0f);
                targetStateRect.sizeDelta = new Vector2(190f, 76f);
            }
        }

        bool enabled = targetState != null &&
            string.Equals(
                targetState.text.Trim(),
                "ON",
                System.StringComparison.OrdinalIgnoreCase);
        ApplyButtonSprite(
            targetButton,
            enabled ? MintButtonPath : BlueButtonPath);
    }

    private static void ApplyButtonSprite(Button button, string resourcePath)
    {
        Image image = button.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }

        image.color = Color.white;
        image.raycastTarget = true;
        Material material =
            Resources.Load<Material>(resourcePath + "Satin");
        image.material =
            material != null && material.shader != null &&
            material.shader.isSupported
                ? material
                : null;
        button.targetGraphic = image;
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
}
