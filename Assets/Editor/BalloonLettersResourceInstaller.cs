#if UNITY_EDITOR
using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class BalloonLettersResourceInstaller
{
    private const string SourceFolder = "Assets/Balloons/prefab/Baloons";
    private const string ResourcesFolder = "Assets/Resources";
    private const string TargetFolder = "Assets/Resources/BalloonLetters";
    private const string RequiredCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    static BalloonLettersResourceInstaller()
    {
        EditorApplication.delayCall += EnsureInstalled;
    }

    [MenuItem("Tools/Balloon Dog/Refresh Balloon Letter Resources")]
    public static void EnsureInstalled()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Balloons") ||
            !AssetDatabase.IsValidFolder(SourceFolder))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(ResourcesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        if (!AssetDatabase.IsValidFolder(TargetFolder))
        {
            AssetDatabase.CreateFolder(ResourcesFolder, "BalloonLetters");
        }

        bool copiedAny = false;
        foreach (char character in RequiredCharacters)
        {
            string fileName = "Balloon_" + character + ".prefab";
            string sourcePath = Path.Combine(SourceFolder, fileName).Replace('\\', '/');
            string targetPath = Path.Combine(TargetFolder, fileName).Replace('\\', '/');

            if (!File.Exists(sourcePath))
            {
                continue;
            }

            if (File.Exists(targetPath))
            {
                continue;
            }

            if (AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                copiedAny = true;
            }
        }

        if (copiedAny)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
