using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Loads Titan One from Resources and creates one cached dynamic TMP font
/// asset for the game UI.
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
                    ResourcePath + ". UI text will retain its TMP fallback font.");
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
            // Titan One has no U+20BA glyph. Bundle a fallback so prices render
            // the real Turkish lira sign on devices, without an OS font dependency.
            Font symbols = Resources.Load<Font>("Fonts/Audex-Regular");
            if (symbols != null)
            {
                TMP_FontAsset fallback = TMP_FontAsset.CreateFontAsset(
                    symbols, 96, 10, GlyphRenderMode.SDFAA, 512, 512,
                    AtlasPopulationMode.Dynamic, true);
                fallback.name = "Currency Symbols Runtime SDF";
                fallback.TryAddCharacters("₺");
                cachedAsset.fallbackFontAssetTable =
                    new System.Collections.Generic.List<TMP_FontAsset> { fallback };
            }
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
