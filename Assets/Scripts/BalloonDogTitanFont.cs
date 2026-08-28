using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Loads Titan One from Resources and creates one cached dynamic TMP font
/// asset for the runtime-built Pause Menu.
/// </summary>
public static class BalloonDogTitanFont
{
    private const string ResourcePath = "Fonts/TitanOne/TitanOne-Regular";
    private static TMP_FontAsset cachedAsset;
    private static bool missingReported;

    public static TMP_FontAsset Get()
    {
        if (cachedAsset != null)
        {
            return cachedAsset;
        }

        Font source = Resources.Load<Font>(ResourcePath);
        if (source == null)
        {
            if (!missingReported)
            {
                Debug.LogError(
                    "Titan One could not be loaded from Resources/" +
                    ResourcePath + ". Pause text will retain its TMP fallback font.");
                missingReported = true;
            }
            return null;
        }

        try
        {
            cachedAsset = TMP_FontAsset.CreateFontAsset(
                source,
                96,
                10,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            cachedAsset.name = "Titan One Runtime SDF";
            return cachedAsset;
        }
        catch (System.Exception exception)
        {
            if (!missingReported)
            {
                Debug.LogError(
                    "Titan One TMP asset creation failed: " + exception.Message);
                missingReported = true;
            }
            return null;
        }
    }

    public static void Apply(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        TMP_FontAsset font = Get();
        if (font != null)
        {
            text.font = font;
        }

        text.fontStyle = FontStyles.Normal;
        text.fontWeight = FontWeight.Regular;
    }
}
