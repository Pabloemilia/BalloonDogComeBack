using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BalloonUiPolishRuntime : MonoBehaviour
{
    private const string RuntimeObjectName = "__BalloonUiPolishV14";

    private TMP_Text bestScoreText;
    private RectTransform bestScoreSpriteRoot;
    private string lastBestScoreDigits = string.Empty;

    private static readonly Dictionary<string, Sprite> SpriteCache =
        new Dictionary<string, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimePolisher()
    {
        string[] oldRuntimeNames =
        {
            "__BalloonUiPolishV12",
            "__BalloonUiPolishV13",
            RuntimeObjectName
        };

        foreach (string runtimeName in oldRuntimeNames)
        {
            GameObject oldRuntime = GameObject.Find(runtimeName);
            if (oldRuntime != null)
            {
                Destroy(oldRuntime);
            }
        }

        GameObject runtimeObject = new GameObject(RuntimeObjectName);
        runtimeObject.AddComponent<BalloonUiPolishRuntime>();
    }

    private IEnumerator Start()
    {
        for (int index = 0; index < 9; index++)
        {
            yield return null;
        }

        ApplyMainMenuSprites();
        ApplySettingsTypography();
        ReplaceDecorSquaresWithBalloons();
    }

    private void Update()
    {
        RefreshBestScoreSprites();
    }

    private void ApplyMainMenuSprites()
    {
        GameObject mainMenuPanel = FindNamedObject("MainMenuPanel");
        if (mainMenuPanel == null)
        {
            return;
        }

        TMP_Text[] texts =
            mainMenuPanel.GetComponentsInChildren<TMP_Text>(true);

        TMP_Text title = FindText(
            texts,
            new[] { "MenuTitle", "TitleText", "GameTitle" },
            new[] { "BALLOON" });

        TMP_Text subtitle = FindText(
            texts,
            new[] { "MenuSubtitle", "SubtitleText", "TaglineText" },
            new[] { "HAVANI", "FIRLAT" });

        TMP_Text footer = FindText(
            texts,
            new[] { "FooterText", "VersionText" },
            new[] { "PROTOTİP", "MOBİL RUNNER" });

        bestScoreText = FindText(
            texts,
            new[] { "BestScoreText", "HighScoreText" },
            new[] { "EN İYİ" });

        // Buton alanlarını biraz büyüt ki yazı taşmasın.
        ResizeButton("PlayButton", new Vector2(520f, 90f));
        ResizeButton("SettingsButton", new Vector2(440f, 76f));
        ResizeButton("ExitButton", new Vector2(360f, 68f));

        ReplaceTextWithSprite(
            title,
            "BalloonText/title",
            "V14BalloonTitle",
            0.93f,
            0.86f);

        ReplaceTextWithSprite(
            subtitle,
            "BalloonText/subtitle",
            "V14BalloonSubtitle",
            0.88f,
            0.62f);

        ReplaceTextWithSprite(
            footer,
            "BalloonText/footer",
            "V14BalloonFooter",
            0.82f,
            0.66f);

        ReplaceButtonLabel(
            "PlayButton",
            "BalloonText/play",
            "V14PlayLabel",
            0.54f);

        ReplaceButtonLabel(
            "SettingsButton",
            "BalloonText/settings",
            "V14SettingsLabel",
            0.48f);

        ReplaceButtonLabel(
            "ExitButton",
            "BalloonText/exit",
            "V14ExitLabel",
            0.46f);

        SetupBestScoreSpriteRoot();
    }

    private static void ResizeButton(string buttonName, Vector2 size)
    {
        GameObject buttonObject = FindNamedObject(buttonName);
        if (buttonObject == null)
        {
            return;
        }

        RectTransform rect = buttonObject.transform as RectTransform;
        if (rect == null)
        {
            return;
        }

        rect.sizeDelta = size;
    }

    private static void ReplaceButtonLabel(
        string buttonName,
        string resourcePath,
        string imageName,
        float maxHeightPercent)
    {
        GameObject buttonObject = FindNamedObject(buttonName);
        if (buttonObject == null)
        {
            return;
        }

        TMP_Text[] labels =
            buttonObject.GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text label in labels)
        {
            SetTextInvisible(label);
        }

        Image image =
            GetOrCreateImageChild(buttonObject.transform, imageName);

        image.sprite = LoadSprite(resourcePath);
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;
        FitImageToParent(image, 0.80f, maxHeightPercent);
        image.transform.SetAsLastSibling();
    }

    private static void ReplaceTextWithSprite(
        TMP_Text text,
        string resourcePath,
        string imageName,
        float maxWidthPercent,
        float maxHeightPercent)
    {
        if (text == null)
        {
            return;
        }

        SetTextInvisible(text);

        Image image =
            GetOrCreateImageChild(text.transform, imageName);

        image.sprite = LoadSprite(resourcePath);
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;
        FitImageToParent(image, maxWidthPercent, maxHeightPercent);
    }

    private void SetupBestScoreSpriteRoot()
    {
        if (bestScoreText == null)
        {
            return;
        }

        SetTextInvisible(bestScoreText);

        Transform existing =
            bestScoreText.transform.Find("V14BestScoreSprites");

        if (existing == null)
        {
            GameObject root = new GameObject(
                "V14BestScoreSprites",
                typeof(RectTransform));

            root.transform.SetParent(bestScoreText.transform, false);
            bestScoreSpriteRoot = root.GetComponent<RectTransform>();
        }
        else
        {
            bestScoreSpriteRoot = existing as RectTransform;
        }

        bestScoreSpriteRoot.anchorMin = Vector2.zero;
        bestScoreSpriteRoot.anchorMax = Vector2.one;
        bestScoreSpriteRoot.offsetMin = Vector2.zero;
        bestScoreSpriteRoot.offsetMax = Vector2.zero;
        bestScoreSpriteRoot.localScale = Vector3.one;

        lastBestScoreDigits = string.Empty;
        RefreshBestScoreSprites();
    }

    private void RefreshBestScoreSprites()
    {
        if (bestScoreText == null || bestScoreSpriteRoot == null)
        {
            return;
        }

        string digits = ExtractDigits(bestScoreText.text);
        if (digits == lastBestScoreDigits)
        {
            return;
        }

        lastBestScoreDigits = digits;

        for (int index = bestScoreSpriteRoot.childCount - 1; index >= 0; index--)
        {
            Destroy(bestScoreSpriteRoot.GetChild(index).gameObject);
        }

        List<Sprite> sprites = new List<Sprite>();
        Sprite prefix = LoadSprite("BalloonText/best_prefix");
        if (prefix != null)
        {
            sprites.Add(prefix);
        }

        foreach (char digit in digits)
        {
            Sprite digitSprite = LoadSprite($"BalloonText/digit_{digit}");
            if (digitSprite != null)
            {
                sprites.Add(digitSprite);
            }
        }

        if (sprites.Count == 0)
        {
            return;
        }

        float availableHeight = Mathf.Max(22f, bestScoreSpriteRoot.rect.height * 0.58f);
        float spacing = 2f;
        float totalWidth = 0f;
        float[] widths = new float[sprites.Count];

        for (int index = 0; index < sprites.Count; index++)
        {
            Sprite sprite = sprites[index];
            float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            float height = index == 0 ? availableHeight * 0.82f : availableHeight;
            widths[index] = height * aspect;
            totalWidth += widths[index];
            if (index > 0)
            {
                totalWidth += spacing;
            }
        }

        float scale = totalWidth > bestScoreSpriteRoot.rect.width && totalWidth > 0f
            ? bestScoreSpriteRoot.rect.width / totalWidth
            : 1f;

        float cursor = -totalWidth * scale * 0.5f;

        for (int index = 0; index < sprites.Count; index++)
        {
            GameObject imageObject = new GameObject(
                index == 0 ? "BestPrefix" : $"BestDigit_{index}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            imageObject.transform.SetParent(bestScoreSpriteRoot, false);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprites[index];
            image.preserveAspect = true;
            image.raycastTarget = false;

            float height =
                (index == 0 ? availableHeight * 0.82f : availableHeight) * scale;
            float width = widths[index] * scale;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(cursor, 0f);
            rect.sizeDelta = new Vector2(width, height);

            cursor += width + spacing * scale;
        }
    }

    private static void ReplaceDecorSquaresWithBalloons()
    {
        Sprite balloonSprite = LoadSprite("BalloonDecor/menu_balloon");
        if (balloonSprite == null)
        {
            return;
        }

        Image[] images = Resources.FindObjectsOfTypeAll<Image>();
        Color[] palette =
        {
            new Color(0.26f, 0.85f, 1f, 0.32f),
            new Color(1f, 0.55f, 0.74f, 0.28f),
            new Color(0.70f, 0.85f, 1f, 0.26f),
            new Color(0.38f, 0.96f, 0.90f, 0.24f),
            new Color(1f, 0.79f, 0.34f, 0.25f),
        };

        foreach (Image image in images)
        {
            if (image == null ||
                !image.gameObject.scene.IsValid() ||
                image.GetComponentInParent<Button>() != null)
            {
                continue;
            }

            if (image.name.Contains("V14") ||
                image.name.Contains("BalloonTitle") ||
                image.name.Contains("PlayLabel") ||
                image.name.Contains("SettingsLabel") ||
                image.name.Contains("ExitLabel"))
            {
                continue;
            }

            RectTransform rect = image.rectTransform;
            float width = rect.rect.width > 0f ? rect.rect.width : rect.sizeDelta.x;
            float height = rect.rect.height > 0f ? rect.rect.height : rect.sizeDelta.y;

            bool looksLikeDecor =
                image.color.a > 0.04f &&
                image.color.a < 0.60f &&
                width >= 22f &&
                width <= 180f &&
                height >= 22f &&
                height <= 180f &&
                Mathf.Abs(width - height) <= 30f;

            if (!looksLikeDecor)
            {
                continue;
            }

            image.sprite = balloonSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = palette[Mathf.Abs(image.gameObject.name.GetHashCode()) % palette.Length];

            float diameter = Mathf.Max(width, height);
            rect.sizeDelta = new Vector2(diameter * 0.82f, diameter * 1.02f);
        }
    }

    private static void ApplySettingsTypography()
    {
        GameObject settingsPanel = FindNamedObject("SettingsPanel");
        if (settingsPanel == null)
        {
            return;
        }

        TMP_Text[] texts = settingsPanel.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text == null)
            {
                continue;
            }

            text.fontStyle |= FontStyles.Bold;
            text.outlineColor = new Color32(3, 42, 72, 255);
            text.outlineWidth = 0.24f;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(
                Color.white,
                Color.white,
                new Color(0.65f, 0.92f, 1f, 1f),
                new Color(0.65f, 0.92f, 1f, 1f));
        }
    }

    private static void FitImageToParent(
        Image image,
        float maxWidthPercent,
        float maxHeightPercent)
    {
        if (image == null || image.sprite == null)
        {
            return;
        }

        RectTransform parentRect = image.transform.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        float parentWidth = Mathf.Max(1f, parentRect.rect.width);
        float parentHeight = Mathf.Max(1f, parentRect.rect.height);
        float maxWidth = parentWidth * Mathf.Clamp(maxWidthPercent, 0.1f, 1f);
        float maxHeight = parentHeight * Mathf.Clamp(maxHeightPercent, 0.1f, 1f);

        float aspect = image.sprite.rect.width / Mathf.Max(1f, image.sprite.rect.height);

        float width = maxWidth;
        float height = width / aspect;

        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspect;
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
    }

    private static TMP_Text FindText(
        TMP_Text[] texts,
        string[] preferredNames,
        string[] contentTokens)
    {
        foreach (string preferredName in preferredNames)
        {
            foreach (TMP_Text text in texts)
            {
                if (text != null && text.name == preferredName)
                {
                    return text;
                }
            }
        }

        foreach (TMP_Text text in texts)
        {
            if (text == null || string.IsNullOrEmpty(text.text))
            {
                continue;
            }

            string upper = text.text.ToUpperInvariant();
            foreach (string token in contentTokens)
            {
                if (upper.Contains(token))
                {
                    return text;
                }
            }
        }

        return null;
    }

    private static string ExtractDigits(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "0";
        }

        string result = string.Empty;
        foreach (char character in value)
        {
            if (char.IsDigit(character))
            {
                result += character;
            }
        }

        return string.IsNullOrEmpty(result) ? "0" : result;
    }

    private static void SetTextInvisible(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        Color color = text.color;
        color.a = 0f;
        text.color = color;
        text.raycastTarget = false;
    }

    private static Image GetOrCreateImageChild(
        Transform parent,
        string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            Image existingImage = existing.GetComponent<Image>();
            if (existingImage != null)
            {
                existing.gameObject.SetActive(true);
                return existingImage;
            }
        }

        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        imageObject.transform.SetParent(parent, false);
        return imageObject.GetComponent<Image>();
    }

    private static Sprite LoadSprite(string resourcePath)
    {
        if (SpriteCache.TryGetValue(resourcePath, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        SpriteCache[resourcePath] = sprite;
        return sprite;
    }

    private static GameObject FindNamedObject(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform candidate in transforms)
        {
            if (candidate == null ||
                candidate.name != objectName ||
                !candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            return candidate.gameObject;
        }

        return null;
    }
}
