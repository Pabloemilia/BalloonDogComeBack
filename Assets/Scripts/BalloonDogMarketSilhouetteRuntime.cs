using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Replaces the temporary block-built market icons with a thumbnail rendered
/// from the actual Balloon Dog model already used by the player.
/// </summary>
public sealed class BalloonDogMarketSilhouetteRuntime : MonoBehaviour
{
    private const string RuntimeObjectName = "__BalloonDogMarketSilhouetteRuntime";
    private const string ModelResourcePath = "Models/BalloonDog/BalloonDog";
    private const int PreviewLayer = 31;

    private static RenderTexture silhouetteTexture;
    private float nextRefreshTime;
    private bool renderAttempted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntime()
    {
        if (GameObject.Find(RuntimeObjectName) != null)
        {
            return;
        }

        GameObject runtime = new GameObject(RuntimeObjectName);
        DontDestroyOnLoad(runtime);
        runtime.AddComponent<BalloonDogMarketSilhouetteRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + 0.25f;

        GameObject market = GameObject.Find("ModernMarketScreen");
        if (market == null || !market.activeInHierarchy)
        {
            return;
        }

        if (silhouetteTexture == null && !renderAttempted)
        {
            renderAttempted = true;
            silhouetteTexture = RenderActualBalloonDog();
        }

        if (silhouetteTexture != null)
        {
            ApplyToMarketCards(market);
        }
    }

    private void OnDestroy()
    {
        if (silhouetteTexture != null)
        {
            silhouetteTexture.Release();
            Destroy(silhouetteTexture);
            silhouetteTexture = null;
        }
    }

    private static void ApplyToMarketCards(GameObject market)
    {
        foreach (Transform candidate in market.GetComponentsInChildren<Transform>(true))
        {
            if (candidate == null || !candidate.name.StartsWith("MarketSkinSlot_"))
            {
                continue;
            }

            RawImage preview = candidate.GetComponentInChildren<RawImage>(true);
            if (preview == null || preview.name != "BalloonDogModelSilhouette")
            {
                GameObject previewObject = new GameObject(
                    "BalloonDogModelSilhouette",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(RawImage));
                previewObject.transform.SetParent(candidate, false);
                preview = previewObject.GetComponent<RawImage>();

                RectTransform rect = preview.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 27f);
                rect.sizeDelta = new Vector2(205f, 116f);

                Transform question = candidate.Find("MysteryMark");
                if (question != null)
                {
                    rect.SetSiblingIndex(question.GetSiblingIndex());
                }
            }

            preview.texture = silhouetteTexture;
            preview.color = new Color(0.01f, 0.035f, 0.075f, 0.78f);
            preview.raycastTarget = false;
            preview.uvRect = new Rect(0f, 0f, 1f, 1f);

            foreach (Image oldPart in candidate.GetComponentsInChildren<Image>(true))
            {
                if (oldPart != null && oldPart.name.StartsWith("DogSilhouette"))
                {
                    oldPart.enabled = false;
                }
            }
        }
    }

    private static RenderTexture RenderActualBalloonDog()
    {
        GameObject modelAsset = Resources.Load<GameObject>(ModelResourcePath);
        if (modelAsset == null)
        {
            Debug.LogWarning("Balloon Dog market silhouette could not load the player model.");
            return null;
        }

        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Unlit/Color");

        if (shader == null)
        {
            Debug.LogWarning("Balloon Dog market silhouette could not find an unlit shader.");
            return null;
        }

        GameObject stage = new GameObject("__BalloonDogSilhouetteStage");
        stage.hideFlags = HideFlags.HideAndDontSave;
        stage.transform.position = new Vector3(0f, -10000f, 0f);

        GameObject model = Instantiate(modelAsset, stage.transform);
        model.name = "BalloonDogSilhouetteModel";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        model.transform.localScale = Vector3.one;
        SetLayerRecursively(model.transform, PreviewLayer);

        Material silhouetteMaterial = new Material(shader);
        silhouetteMaterial.hideFlags = HideFlags.HideAndDontSave;
        if (silhouetteMaterial.HasProperty("_BaseColor"))
        {
            silhouetteMaterial.SetColor("_BaseColor", Color.white);
        }
        if (silhouetteMaterial.HasProperty("_Color"))
        {
            silhouetteMaterial.SetColor("_Color", Color.white);
        }

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer is TrailRenderer)
            {
                continue;
            }

            Material[] materials = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
            for (int index = 0; index < materials.Length; index++)
            {
                materials[index] = silhouetteMaterial;
            }

            renderer.sharedMaterials = materials;
            renderer.enabled = true;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        if (!hasBounds)
        {
            Destroy(stage);
            Destroy(silhouetteMaterial);
            return null;
        }

        RenderTexture texture = new RenderTexture(512, 300, 16, RenderTextureFormat.ARGB32)
        {
            name = "BalloonDogMarketSilhouette",
            antiAliasing = 4,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.Create();

        GameObject cameraObject = new GameObject("SilhouetteCamera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        cameraObject.transform.SetParent(stage.transform, true);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.cullingMask = 1 << PreviewLayer;
        camera.orthographic = true;
        camera.allowHDR = false;
        camera.allowMSAA = true;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        camera.targetTexture = texture;

        float largestHorizontal = Mathf.Max(bounds.size.x, bounds.size.z);
        float sizeFromWidth = largestHorizontal / (2f * (512f / 300f));
        camera.orthographicSize = Mathf.Max(bounds.extents.y, sizeFromWidth) * 1.18f;

        float distance = Mathf.Max(bounds.size.magnitude * 1.8f, 8f);
        Vector3 viewDirection = new Vector3(0.72f, 0.16f, -1f).normalized;
        camera.transform.position = bounds.center + viewDirection * distance;
        camera.transform.LookAt(bounds.center + Vector3.up * bounds.extents.y * 0.03f);
        camera.Render();
        camera.targetTexture = null;

        Destroy(stage);
        Destroy(silhouetteMaterial);
        return texture;
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
        {
            SetLayerRecursively(child, layer);
        }
    }
}
