using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Loads the licensed Fredoka source fonts from Resources and creates one
/// cached dynamic TMP font asset per weight. This keeps the pause menu fully
/// self-contained and avoids Inspector-only font wiring.
/// </summary>
public static class BalloonDogFredokaFont
{
    public enum Weight
    {
        Regular,
        Medium,
        SemiBold,
        Bold
    }

    private const string ResourceRoot = "Fonts/Fredoka/";
    private static readonly Dictionary<Weight, TMP_FontAsset> Assets =
        new Dictionary<Weight, TMP_FontAsset>();
    private static readonly HashSet<Weight> ReportedMissingWeights =
        new HashSet<Weight>();

    public static TMP_FontAsset Get(Weight weight)
    {
        if (Assets.TryGetValue(weight, out TMP_FontAsset cached) && cached != null)
        {
            return cached;
        }

        Font source = Resources.Load<Font>(ResourceRoot + GetSourceName(weight));
        if (source == null)
        {
            if (ReportedMissingWeights.Add(weight))
            {
                Debug.LogError(
                    $"Fredoka {weight} could not be loaded from Resources/{ResourceRoot}. " +
                    "Pause text will retain its current TMP fallback font.");
            }
            return null;
        }

        try
        {
            TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(
                source,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            created.name = $"Fredoka {weight} Runtime SDF";
            Assets[weight] = created;
            return created;
        }
        catch (System.Exception exception)
        {
            if (ReportedMissingWeights.Add(weight))
            {
                Debug.LogError($"Fredoka {weight} TMP asset creation failed: {exception.Message}");
            }
            return null;
        }
    }

    public static void Apply(TMP_Text text, Weight weight)
    {
        if (text == null)
        {
            return;
        }

        TMP_FontAsset font = Get(weight);
        if (font != null)
        {
            text.font = font;
        }

        // Weight is supplied by a real Fredoka source file, not synthesized by TMP.
        text.fontStyle = FontStyles.Normal;
        text.fontWeight = FontWeight.Regular;
    }

    private static string GetSourceName(Weight weight)
    {
        switch (weight)
        {
            case Weight.Medium:
                return "Fredoka-Medium";
            case Weight.SemiBold:
                return "Fredoka-SemiBold";
            case Weight.Bold:
                return "Fredoka-Bold";
            default:
                return "Fredoka-Regular";
        }
    }
}
