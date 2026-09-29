using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates lightweight Resources prefabs that reference the imported Downtown
/// Game Studio FBX files. It runs once after the package is imported or pulled.
/// </summary>
[InitializeOnLoad]
public static class BalloonDogObstacleResourceBaker
{
    private const string Source =
        "Assets/Downtown Game Studio/LOW POLY ASSETS - Animated Traps & Obstacles + VFX/Models/";
    private const string Output = "Assets/Resources/GeneratedObstacles";

    private static readonly string[] SourceNames =
    {
        "Obstacle 1 V1", "Obstacle 6 V1", "Obstacle 11 V1",
        "Obstacle 16 V1", "Obstacle 21 V1"
    };

    private static readonly string[] OutputNames =
    {
        "Obstacle01", "Obstacle06", "Obstacle11", "Obstacle16", "Obstacle21"
    };

    static BalloonDogObstacleResourceBaker()
    {
        EditorApplication.delayCall += BakeMissingPrefabs;
    }

    [MenuItem("Balloon Dog/Rebuild Obstacle Course Resources")]
    private static void RebuildAll()
    {
        EnsureOutputFolder();
        for (int i = 0; i < OutputNames.Length; i++)
            AssetDatabase.DeleteAsset(Output + "/" + OutputNames[i] + ".prefab");
        BakeMissingPrefabs();
    }

    private static void BakeMissingPrefabs()
    {
        EnsureOutputFolder();
        bool changed = false;
        for (int i = 0; i < SourceNames.Length; i++)
        {
            string outputPath = Output + "/" + OutputNames[i] + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null) continue;

            string sourcePath = Source + SourceNames[i] + ".fbx";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (model == null)
            {
                Debug.LogError("Balloon Dog obstacle FBX is missing: " + sourcePath);
                continue;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null) continue;
            instance.name = OutputNames[i];
            PrefabUtility.SaveAsPrefabAsset(instance, outputPath);
            Object.DestroyImmediate(instance);
            changed = true;
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Balloon Dog obstacle course resources are ready.");
        }
    }

    private static void EnsureOutputFolder()
    {
        if (!AssetDatabase.IsValidFolder(Output))
            AssetDatabase.CreateFolder("Assets/Resources", "GeneratedObstacles");
    }
}
