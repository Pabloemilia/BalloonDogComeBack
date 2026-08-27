using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Applies a light runtime polish pass only to the Pause screen decorative
/// balloon-dog and cloud layer. The actual pause layout, buttons, title and
/// stats panel are intentionally left untouched.
/// </summary>
[DisallowMultipleComponent]
public sealed class BalloonDogPauseDecorationPolish : MonoBehaviour
{
    private const string RuntimeObjectName = "__BalloonDogPauseDecorationPolish";
    private readonly HashSet<int> processedLayers = new HashSet<int>();
    private Coroutine scanRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimePolish()
    {
        if (GameObject.Find(RuntimeObjectName) != null)
        {
            return;
        }

        GameObject runtimeObject = new GameObject(RuntimeObjectName);
        DontDestroyOnLoad(runtimeObject);
        runtimeObject.AddComponent<BalloonDogPauseDecorationPolish>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        RestartScan();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RestartScan();
    }

    private void RestartScan()
    {
        if (scanRoutine != null)
        {
            StopCoroutine(scanRoutine);
        }

        scanRoutine = StartCoroutine(FindAndPolishPauseDecorations());
    }

    private IEnumerator FindAndPolishPauseDecorations()
    {
        // BalloonDogModernUI builds its runtime hierarchy shortly after the
        // scene is loaded. Poll with unscaled time so this also works while
        // gameplay is paused.
        for (int attempt = 0; attempt < 80; attempt++)
        {
            Transform layer = FindDecorativeLayer();
            if (layer != null)
            {
                int instanceId = layer.gameObject.GetInstanceID();
                if (!processedLayers.Contains(instanceId))
                {
                    PolishLayer(layer);
                    processedLayers.Add(instanceId);
                }

                scanRoutine = null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.25f);
        }

        scanRoutine = null;
    }

    private static Transform FindDecorativeLayer()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject candidate in allObjects)
        {
            if (candidate == null || candidate.name != "DecorativeLayer")
            {
                continue;
            }

            Transform parent = candidate.transform.parent;
            if (parent != null && parent.name == "ModernPauseScreen")
            {
                return candidate.transform;
            }
        }

        return null;
    }

    private static void PolishLayer(Transform layer)
    {
        // Existing decorations: 20-35% smaller, slightly irregular and no
        // copy-paste alignment. Their motion continues through the existing
        // unscaled-time BalloonDogPauseDecorativeFloat component.
        ConfigureExisting(layer, "Cloud_01", 0.78f, -5f, 7f, 9f, 16.5f, 0.08f, 0.35f);
        ConfigureExisting(layer, "Cloud_02", 0.72f, 6f, 6f, 10f, 18.5f, 0.41f, 0.45f);
        ConfigureExisting(layer, "Cloud_03", 0.80f, -3f, 8f, 7f, 20.5f, 0.69f, 0.30f);
        ConfigureExisting(layer, "Cloud_04", 0.74f, 5f, 5f, 9f, 17.5f, 0.27f, 0.40f);

        ConfigureExisting(layer, "BalloonDog_01", 0.76f, -12f, 9f, 12f, 19.5f, 0.15f, 1.6f);
        ConfigureExisting(layer, "BalloonDog_02", 0.72f, 9f, 7f, 10f, 17.2f, 0.56f, 1.8f);
        ConfigureExisting(layer, "BalloonDog_03", 0.79f, -7f, 10f, 8f, 22.0f, 0.35f, 1.5f);
        ConfigureExisting(layer, "BalloonDog_04", 0.74f, 13f, 8f, 11f, 18.8f, 0.78f, 1.9f);

        // Add a few extra decorations rather than filling the screen with
        // duplicates. Placement intentionally avoids the central UI column.
        CloneDecoration(layer, "BalloonDog_02", "BalloonDog_05",
            new Vector2(-455f, 120f), 0.68f, 14f,
            7f, 9f, 20.8f, 0.24f, 1.7f, 0.88f);
        CloneDecoration(layer, "BalloonDog_01", "BalloonDog_06",
            new Vector2(455f, -145f), 0.63f, -10f,
            8f, 10f, 23.0f, 0.64f, 1.6f, 0.82f);

        CloneDecoration(layer, "Cloud_02", "Cloud_05",
            new Vector2(385f, 310f), 0.66f, -6f,
            5f, 8f, 19.8f, 0.16f, 0.35f, 0.88f);
        CloneDecoration(layer, "Cloud_04", "Cloud_06",
            new Vector2(-360f, -470f), 0.64f, 5f,
            6f, 7f, 21.4f, 0.53f, 0.32f, 0.84f);
        CloneDecoration(layer, "Cloud_01", "Cloud_07",
            new Vector2(420f, -820f), 0.58f, -4f,
            5f, 7f, 22.8f, 0.86f, 0.30f, 0.78f);
    }

    private static void ConfigureExisting(
        Transform layer,
        string name,
        float scale,
        float rotationDegrees,
        float horizontalAmplitude,
        float verticalAmplitude,
        float duration,
        float phase,
        float rotationAmplitude)
    {
        Transform target = layer.Find(name);
        if (target == null)
        {
            return;
        }

        target.localScale = Vector3.one * scale;
        target.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);

        BalloonDogPauseDecorativeFloat motion =
            target.GetComponent<BalloonDogPauseDecorativeFloat>();
        if (motion != null)
        {
            motion.Configure(
                horizontalAmplitude,
                verticalAmplitude,
                duration,
                phase,
                rotationAmplitude);
        }
    }

    private static void CloneDecoration(
        Transform layer,
        string sourceName,
        string cloneName,
        Vector2 position,
        float scale,
        float rotationDegrees,
        float horizontalAmplitude,
        float verticalAmplitude,
        float duration,
        float phase,
        float rotationAmplitude,
        float opacityMultiplier)
    {
        if (layer.Find(cloneName) != null)
        {
            return;
        }

        Transform source = layer.Find(sourceName);
        if (source == null)
        {
            return;
        }

        GameObject cloneObject = Instantiate(source.gameObject, layer, false);
        cloneObject.name = cloneName;

        RectTransform rect = cloneObject.transform as RectTransform;
        if (rect != null)
        {
            rect.anchoredPosition = position;
        }

        cloneObject.transform.localScale = Vector3.one * scale;
        cloneObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);

        Graphic[] graphics = cloneObject.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            Color color = graphic.color;
            color.a = Mathf.Clamp01(color.a * opacityMultiplier);
            graphic.color = color;
            graphic.raycastTarget = false;
        }

        BalloonDogPauseDecorativeFloat motion =
            cloneObject.GetComponent<BalloonDogPauseDecorativeFloat>();
        if (motion != null)
        {
            motion.Configure(
                horizontalAmplitude,
                verticalAmplitude,
                duration,
                phase,
                rotationAmplitude);
        }
    }
}
