using System.IO;
using UnityEditor;
using Unity.VectorGraphics.Editor;

/// <summary>Configures only the six supplied Settings SVGs as tessellated uGUI sprites.</summary>
[InitializeOnLoad]
public sealed class BalloonDogSettingsAssetImporter : AssetPostprocessor
{
    private const string Folder = "Assets/Resources/Settings/Icons/";
    static BalloonDogSettingsAssetImporter()
    {
        EditorApplication.delayCall += RepairExistingImports;
    }
    private void OnPreprocessAsset()
    {
        if (assetPath.StartsWith(Folder) && assetPath.EndsWith(".svg") && assetImporter is SVGImporter svg)
            Configure(svg);
    }
    private static void Configure(SVGImporter svg)
    {
        svg.SvgType = SVGType.UISVGImage;
        svg.PreserveSVGImageAspect = true;
        svg.GeneratePhysicsShape = false;
        svg.AdvancedMode = true;
        svg.MaxCordDeviationEnabled = true;
        svg.MaxCordDeviation = 0.01f;
    }
    private static void RepairExistingImports()
    {
        if (!Directory.Exists(Folder)) return;
        foreach (string path in Directory.GetFiles(Folder, "*.svg"))
        {
            var svg = AssetImporter.GetAtPath(path.Replace('\\', '/')) as SVGImporter;
            if (svg == null || (svg.SvgType == SVGType.UISVGImage && svg.PreserveSVGImageAspect &&
                !svg.GeneratePhysicsShape && svg.AdvancedMode && svg.MaxCordDeviationEnabled &&
                svg.MaxCordDeviation == 0.01f)) continue;
            Configure(svg);
            svg.SaveAndReimport();
        }
    }
}
