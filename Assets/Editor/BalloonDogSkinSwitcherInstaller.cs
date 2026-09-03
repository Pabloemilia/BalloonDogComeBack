using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BalloonDogSkinSwitcherInstaller
{
    private const string SkinFolder = "Assets/Skins";
    private const string SkinRootName = "BalloonDog_SkinRoot";
    private const string ModelRootName = "BalloonDogModel";
    private const string StarterChildName = "default";
    private const string LegacyVisualName = "BalloonDog_SkinModel";
    private const string ActivePrefix = "BalloonDog_ActiveSkin_";
    private const string PreviewPrefix = "BalloonDog_Preview_";

    [MenuItem("Tools/Balloon Dog/Skin Sistemi V5/Sabit Konum Kur", priority = 1)]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Play modunu kapat",
                "Kurulumu sahneye kaydetmek icin once Play modunu kapat.",
                "Tamam");
            return;
        }

        if (!AssetDatabase.IsValidFolder(SkinFolder))
        {
            EditorUtility.DisplayDialog(
                "Assets/Skins bulunamadi",
                "Skin prefablarini Assets/Skins klasorune koyup tekrar dene.",
                "Tamam");
            return;
        }

        GameObject player = ResolvePlayer();
        if (player == null)
        {
            EditorUtility.DisplayDialog(
                "Player bulunamadi",
                "Hierarchy'deki player nesnesini secip menuyu tekrar calistir.",
                "Tamam");
            return;
        }

        GameObject modelRoot =
            FindDirectChildIgnoreCase(player.transform, ModelRootName);
        GameObject starterVisual = FindStarterVisual(player.transform, modelRoot);
        List<GameObject> skinPrefabs = FindSkinPrefabs();

        if (starterVisual == null)
        {
            EditorUtility.DisplayDialog(
                "Default skin bulunamadi",
                "player > BalloonDogModel > default nesnesi bulunamadi.",
                "Tamam");
            return;
        }

        if (skinPrefabs.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Prefab skin bulunamadi",
                "Assets/Skins klasorunde en az bir prefab olmali.",
                "Tamam");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Install Balloon Dog fixed-position skin system V5");

        Transform skinRoot = FindOrCreateSingleSkinRoot(player.transform);
        GameObject legacyVisual =
            FindDirectChildIgnoreCase(player.transform, LegacyVisualName);

        BalloonDogSkinSwitcher switcher = player.GetComponent<BalloonDogSkinSwitcher>();
        if (switcher == null)
        {
            switcher = Undo.AddComponent<BalloonDogSkinSwitcher>(player);
        }

        Undo.RecordObject(switcher, "Configure Balloon Dog skin system V5");
        switcher.EditorConfigure(
            starterVisual,
            skinRoot,
            legacyVisual,
            skinPrefabs);
        EditorUtility.SetDirty(switcher);

        ClearGeneratedObjects(player.transform, starterVisual, legacyVisual);
        ClearSkinRoot(skinRoot);

        if (modelRoot != null)
        {
            Undo.RecordObject(modelRoot, "Enable BalloonDogModel");
            modelRoot.SetActive(true);
            EditorUtility.SetDirty(modelRoot);
        }

        Undo.RecordObject(starterVisual, "Enable red default skin");
        starterVisual.SetActive(true);
        EditorUtility.SetDirty(starterVisual);

        if (legacyVisual != null && legacyVisual != starterVisual)
        {
            Undo.RecordObject(legacyVisual, "Disable old static skin visual");
            legacyVisual.SetActive(false);
            EditorUtility.SetDirty(legacyVisual);
        }

        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(skinRoot.gameObject);
        EditorSceneManager.MarkSceneDirty(player.scene);
        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);

        EditorUtility.DisplayDialog(
            "Skin Sistemi V5 hazir",
            "SkinRoot player altinda 0/0/0 - 0/0/0 - 1/1/1 olarak kilitlendi.\n" +
            "Kirmizi default'un boyutu ve ayak merkezi referans alinacak.\n" +
            "Baslangic: default\n" +
            "Prefab skin: " + skinPrefabs.Count + "\n\n" +
            "Play'e basip Skin Switcher menusunden Sonraki Skin ile test et.",
            "Tamam");
    }

    [MenuItem("Tools/Balloon Dog/Skin Sistemi V5/Sabit Konum Kur", true)]
    private static bool ValidateInstall()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static List<GameObject> FindSkinPrefabs()
    {
        List<GameObject> result = new List<GameObject>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { SkinFolder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null && !result.Contains(prefab))
            {
                result.Add(prefab);
            }
        }

        result.Sort(ComparePrefabs);
        return result;
    }

    private static int ComparePrefabs(GameObject left, GameObject right)
    {
        bool leftIsCaramel = IsCaramel(left.name);
        bool rightIsCaramel = IsCaramel(right.name);

        if (leftIsCaramel != rightIsCaramel)
        {
            return leftIsCaramel ? -1 : 1;
        }

        return string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCaramel(string value)
    {
        return value.IndexOf("caramel", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("karamel", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static GameObject FindStarterVisual(Transform player, GameObject modelRoot)
    {
        if (modelRoot != null)
        {
            GameObject defaultInsideModel =
                FindDescendantIgnoreCase(modelRoot.transform, StarterChildName);
            if (defaultInsideModel != null)
            {
                return defaultInsideModel;
            }
        }

        return FindDescendantIgnoreCase(player, StarterChildName);
    }

    private static Transform FindOrCreateSingleSkinRoot(Transform player)
    {
        List<Transform> matchingRoots = new List<Transform>();
        Transform[] descendants = player.GetComponentsInChildren<Transform>(true);

        foreach (Transform descendant in descendants)
        {
            if (descendant != player &&
                string.Equals(descendant.name, SkinRootName, StringComparison.OrdinalIgnoreCase))
            {
                matchingRoots.Add(descendant);
            }
        }

        Transform skinRoot = matchingRoots.Find(candidate => candidate.parent == player);
        if (skinRoot == null && matchingRoots.Count > 0)
        {
            skinRoot = matchingRoots[0];
        }

        if (skinRoot == null)
        {
            GameObject rootObject = new GameObject(SkinRootName);
            Undo.RegisterCreatedObjectUndo(rootObject, "Create Balloon Dog SkinRoot");
            skinRoot = rootObject.transform;
        }

        Undo.SetTransformParent(skinRoot, player, "Parent SkinRoot directly to player");

        foreach (Transform duplicateRoot in matchingRoots)
        {
            if (duplicateRoot != null && duplicateRoot != skinRoot)
            {
                Undo.DestroyObjectImmediate(duplicateRoot.gameObject);
            }
        }

        Undo.RecordObject(skinRoot, "Lock Balloon Dog SkinRoot transform");
        skinRoot.localPosition = Vector3.zero;
        skinRoot.localRotation = Quaternion.identity;
        skinRoot.localScale = Vector3.one;
        return skinRoot;
    }

    private static void ClearGeneratedObjects(
        Transform player,
        GameObject starterVisual,
        GameObject legacyVisual)
    {
        List<GameObject> generatedObjects = new List<GameObject>();
        Transform[] descendants = player.GetComponentsInChildren<Transform>(true);

        foreach (Transform descendant in descendants)
        {
            bool generated =
                descendant.name.StartsWith(ActivePrefix, StringComparison.Ordinal) ||
                descendant.name.StartsWith(PreviewPrefix, StringComparison.Ordinal);

            if (generated &&
                descendant.gameObject != starterVisual &&
                descendant.gameObject != legacyVisual)
            {
                generatedObjects.Add(descendant.gameObject);
            }
        }

        foreach (GameObject generatedObject in generatedObjects)
        {
            if (generatedObject != null)
            {
                Undo.DestroyObjectImmediate(generatedObject);
            }
        }
    }

    private static void ClearSkinRoot(Transform skinRoot)
    {
        for (int index = skinRoot.childCount - 1; index >= 0; index--)
        {
            Undo.DestroyObjectImmediate(skinRoot.GetChild(index).gameObject);
        }
    }

    private static GameObject ResolvePlayer()
    {
        GameObject selected = Selection.activeGameObject;

        if (IsSceneObject(selected))
        {
            Transform current = selected.transform;
            while (current != null)
            {
                if (IsPlayerCandidate(current.gameObject))
                {
                    return current.gameObject;
                }

                current = current.parent;
            }
        }

        GameObject nameMatch = null;

        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!IsSceneObject(candidate))
            {
                continue;
            }

            if (HasPlayerTag(candidate))
            {
                return candidate;
            }

            if (string.Equals(candidate.name, "player", StringComparison.OrdinalIgnoreCase))
            {
                nameMatch = candidate;
            }
        }

        return nameMatch;
    }

    private static bool IsPlayerCandidate(GameObject candidate)
    {
        return candidate != null &&
               (HasPlayerTag(candidate) ||
                string.Equals(candidate.name, "player", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasPlayerTag(GameObject candidate)
    {
        try
        {
            return candidate.CompareTag("Player");
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private static bool IsSceneObject(GameObject candidate)
    {
        return candidate != null &&
               candidate.scene.IsValid() &&
               !EditorUtility.IsPersistent(candidate);
    }

    private static GameObject FindDirectChildIgnoreCase(Transform parent, string childName)
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static GameObject FindDescendantIgnoreCase(Transform parent, string childName)
    {
        Transform[] descendants = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform descendant in descendants)
        {
            if (descendant != parent &&
                string.Equals(descendant.name, childName, StringComparison.OrdinalIgnoreCase))
            {
                return descendant.gameObject;
            }
        }

        return null;
    }
}
