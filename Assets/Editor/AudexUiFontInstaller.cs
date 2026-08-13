#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AudexUiFontInstaller
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string FontsFolder = "Assets/Resources/Fonts";
    private const string TargetPath = "Assets/Resources/Fonts/Audex-Regular.ttf";

    static AudexUiFontInstaller()
    {
        EditorApplication.delayCall += EnsureAudexInResources;
    }

    [MenuItem("Tools/Balloon Dog/Install Audex UI Font")]
    public static void EnsureAudexInResources()
    {
        if (AssetDatabase.LoadAssetAtPath<Font>(TargetPath) != null)
        {
            return;
        }

        string sourcePath = FindAudexFont();
        if (string.IsNullOrEmpty(sourcePath))
        {
            Debug.LogError("[BalloonDog UI] Audex-Regular.ttf bulunamadı. Fontu Assets içine import edip Tools > Balloon Dog > Install Audex UI Font çalıştır.");
            return;
        }

        EnsureFolder("Assets", "Resources");
        EnsureFolder(ResourcesFolder, "Fonts");

        if (sourcePath == TargetPath)
        {
            AssetDatabase.ImportAsset(TargetPath, ImportAssetOptions.ForceUpdate);
            return;
        }

        if (File.Exists(TargetPath))
        {
            AssetDatabase.DeleteAsset(TargetPath);
        }

        if (!AssetDatabase.CopyAsset(sourcePath, TargetPath))
        {
            Debug.LogError("[BalloonDog UI] Audex font Resources klasörüne kopyalanamadı: " + sourcePath);
            return;
        }

        AssetDatabase.ImportAsset(TargetPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BalloonDog UI] Audex-Regular.ttf aktif UI fontu olarak kuruldu.");
    }

    private static string FindAudexFont()
    {
        string[] exact = AssetDatabase.FindAssets("Audex-Regular t:Font");
        foreach (string guid in exact)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path) &&
                path.EndsWith(".ttf", System.StringComparison.OrdinalIgnoreCase) &&
                !path.Contains("/Resources/Fonts/"))
            {
                return path;
            }
        }

        string[] broad = AssetDatabase.FindAssets("Audex t:Font");
        foreach (string guid in broad)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path) &&
                path.EndsWith(".ttf", System.StringComparison.OrdinalIgnoreCase) &&
                !path.ToLowerInvariant().Contains("italic") &&
                !path.Contains("/Resources/Fonts/"))
            {
                return path;
            }
        }

        return string.Empty;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string fullPath = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
