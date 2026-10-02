using UnityEditor;
using UnityEngine;

/// <summary>Removes objects left by the retired editor menu preview.</summary>
[InitializeOnLoad]
public static class BalloonDogEditorGamePreview
{
    static BalloonDogEditorGamePreview()
    {
        EditorApplication.delayCall += ClearRetiredPreview;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) ClearRetiredPreview();
        };
    }

    private static void ClearRetiredPreview()
    {
        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.name == "__EditorGameViewPreview" && obj.scene.IsValid())
                Object.DestroyImmediate(obj);
        }
        foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>())
        {
            if (!renderer.gameObject.scene.IsValid()) continue;
            for (Transform t = renderer.transform; t != null; t = t.parent)
            {
                if (!t.name.StartsWith("RoadTrapAnchor_")) continue;
                renderer.forceRenderingOff = false;
                break;
            }
        }
    }
}
