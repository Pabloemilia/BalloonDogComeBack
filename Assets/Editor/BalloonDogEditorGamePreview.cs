using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Transient Game-view preview: no preview objects are written to the authored scene.</summary>
[InitializeOnLoad]
public static class BalloonDogEditorGamePreview
{
    private static GameObject preview;
    private static readonly List<Renderer> hidden = new List<Renderer>();
    private static double nextCheck;
    private static string signature;

    static BalloonDogEditorGamePreview()
    {
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) Clear();
            if (state == PlayModeStateChange.EnteredEditMode) signature = null;
        };
        AssemblyReloadEvents.beforeAssemblyReload += Clear;
        EditorSceneManager.sceneClosing += (scene, removing) => Clear();
    }

    private static void Update()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + .5;
        PlayerRunner player = Object.FindAnyObjectByType<PlayerRunner>();
        if (player == null) { Clear(); return; }
        var anchors = new List<Transform>();
        string current = player.transform.position.ToString("F3");
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.InstanceID))
        {
            if ((t.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                !t.name.StartsWith("RoadTrapAnchor_")) continue;
            anchors.Add(t);
            current += t.GetInstanceID() + t.position.ToString("F3");
            foreach (Transform child in t)
                current += child.GetInstanceID() + child.localScale.ToString("F3");
        }
        if (preview != null && current == signature) return;
        Clear();
        signature = current;
        preview = new GameObject("__EditorGameViewPreview");
        preview.hideFlags = HideFlags.HideAndDontSave;
        foreach (Transform anchor in anchors)
        {
            foreach (Renderer renderer in anchor.GetComponentsInChildren<Renderer>())
            {
                hidden.Add(renderer);
                renderer.forceRenderingOff = true;
            }
            GameObject copy = Object.Instantiate(anchor.gameObject, preview.transform);
            copy.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            foreach (Renderer renderer in copy.GetComponentsInChildren<Renderer>())
                renderer.forceRenderingOff = false;
            foreach (Transform child in copy.transform)
                if (child.name.StartsWith("PackObstacle_"))
                { PurchasedRoadObstacles.ConfigureObstacle(child); break; }
        }
        var ui = preview.AddComponent<BalloonDogModernUI>();
        ui.BuildEditorMainPreview();
        preview.AddComponent<BalloonDogBalloonThemeRuntime>().ApplyEditorMainPreview();
        foreach (Transform t in preview.GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags = HideFlags.HideAndDontSave;
        foreach (RunnerCameraFollow camera in Object.FindObjectsByType<RunnerCameraFollow>(FindObjectsSortMode.None))
            camera.SnapToTarget();
        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    private static void Clear()
    {
        foreach (Renderer renderer in hidden)
            if (renderer != null) renderer.forceRenderingOff = false;
        hidden.Clear();
        if (preview != null) Object.DestroyImmediate(preview);
        preview = null;
    }
}
