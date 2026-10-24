using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A separate, collider-free studio preview of the equipped player visual.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class BalloonDogMenuShowcase : MonoBehaviour
{
    // Reuse the equipped model renderer inside the capsule reward panel.
    public bool CompactPreview { get; set; }
    private const int PreviewLayer = 30;
    private readonly List<Material> skinMaterials = new List<Material>();
    private readonly List<Mesh> bakedMeshes = new List<Mesh>();
    private readonly List<Material> stageMaterials = new List<Material>();
    private RawImage picture;
    private GameObject stage;
    private Transform dogPivot;
    private Transform dog;
    private Transform platform;
    private Mesh platformMesh;
    private Camera studioCamera;
    private Camera worldCamera;
    private bool worldCameraWasEnabled;
    private int worldCameraCullingMask;
    private RenderTexture target;
    private BalloonDogSkinSwitcher switcher;
    private bool dirty = true;
    private float nextRetry;
    private float frameAspect;
    private string displayedEconomySkin;
    private GameObject displayedVisual;

    private void OnEnable()
    {
        BuildStudio();
        BalloonDogEconomy.Changed += MarkDirty;
        BindPlayer();
        dirty = true;
        nextRetry = 0f;
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group != null) { group.alpha = 1f; group.interactable = true; }
        if (Application.isPlaying && !CompactPreview) PrepareWorldCamera();
    }

    private void PrepareWorldCamera()
    {
        worldCamera = Camera.main;
        if (worldCamera == null) return;
        worldCameraWasEnabled = worldCamera.enabled;
        worldCameraCullingMask = worldCamera.cullingMask;
        // The studio renders only to a texture. Keep a display camera active
        // so Unity does not draw "Display 1 / No cameras rendering" over the UI.
        worldCamera.cullingMask = 0;
        worldCamera.enabled = true;
    }

    private void RestoreWorldCamera()
    {
        if (worldCamera == null) return;
        worldCamera.cullingMask = worldCameraCullingMask;
        worldCamera.enabled = worldCameraWasEnabled;
        worldCamera = null;
    }

    private void BindPlayer()
    {
        if (switcher != null) return;
        switcher = FindAnyObjectByType<BalloonDogSkinSwitcher>();
        if (switcher != null) switcher.SkinChanged += HandleSkinChanged;
    }

    private void MarkDirty() { dirty = true; }
    private void HandleSkinChanged(int index, GameObject visual) { dirty = true; }

    public IEnumerator LeaveMenu()
    {
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        RestoreWorldCamera();
        float elapsed = 0f;
        while (this != null && elapsed < .24f && isActiveAndEnabled)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.SmoothStep(0f, 1f, elapsed / .24f);
            yield return null;
        }
    }

    private void LateUpdate()
    {
        if (stage == null || picture == null) return;
        BindPlayer();
        GameObject visual = switcher != null ? switcher.ActiveSkinInstance : null;
        string economySkin = BalloonDogEconomy.EquippedSkinId;
        if (visual != displayedVisual || displayedEconomySkin != economySkin) dirty = true;
        if (dirty && Time.realtimeSinceStartup >= nextRetry)
        {
            nextRetry = Time.realtimeSinceStartup + .15f;
            // Defer to LateUpdate so every skin/economy subscriber has finished.
            if (BuildEquippedDog(visual))
            {
                displayedVisual = visual;
                displayedEconomySkin = economySkin;
                dirty = false;
            }
        }
        ResizeTarget();
        float time = Application.isPlaying ? Time.unscaledTime : 0f;
        if (dogPivot != null)
            dogPivot.localRotation = Quaternion.Euler(0f, 145f + Mathf.Sin(time * .65f) * 8f, 0f);
    }

    private void BuildStudio()
    {
        Image backdrop = GetComponent<Image>();
        Sprite sky = Resources.Load<Sprite>("MenuRefresh/MenuCloudSky");
        if (backdrop != null && sky != null)
        {
            backdrop.sprite = sky;
            backdrop.type = Image.Type.Simple;
            backdrop.preserveAspect = false;
            backdrop.color = Color.white;
        }
        UiVerticalGradient gradient = GetComponent<UiVerticalGradient>();
        if (gradient != null) gradient.enabled = false;
        foreach (string name in new[] { "FigmaPattern", "FigmaMainButtons", "FigmaMiscButtons", "PrivacyLinkButton" })
        {
            Transform old = transform.Find(name);
            if (old != null) old.gameObject.SetActive(false);
        }

        Transform existing = transform.Find("LiveSkinShowcase");
        if (existing != null) picture = existing.GetComponent<RawImage>();
        else
        {
            GameObject go = MakeUi("LiveSkinShowcase", typeof(RawImage));
            picture = go.GetComponent<RawImage>();
        }
        picture.raycastTarget = false;
        picture.color = Color.white;
        picture.transform.SetAsFirstSibling();
        RectTransform frame = picture.rectTransform;
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.one;
        frame.offsetMin = CompactPreview ? Vector2.zero : new Vector2(70f, 560f);
        frame.offsetMax = CompactPreview ? Vector2.zero : new Vector2(-70f, -770f);

        stage = new GameObject("__BalloonDogMenuStudio");
        stage.hideFlags = HideFlags.HideAndDontSave;
        stage.transform.position = new Vector3(10000f, -10000f, 10000f);
        GameObject cameraObject = new GameObject("MenuStudioCamera", typeof(Camera));
        cameraObject.transform.SetParent(stage.transform, false);
        studioCamera = cameraObject.GetComponent<Camera>();
        studioCamera.clearFlags = CameraClearFlags.SolidColor;
        studioCamera.backgroundColor = Color.clear;
        studioCamera.cullingMask = 1 << PreviewLayer;
        studioCamera.orthographic = true;
        studioCamera.orthographicSize = 2.2f;
        studioCamera.nearClipPlane = .1f;
        studioCamera.farClipPlane = 30f;
        studioCamera.allowHDR = false;
        studioCamera.allowMSAA = true;
        studioCamera.depth = -100f;
        studioCamera.enabled = false;
        cameraObject.transform.localPosition = new Vector3(5f, 3.5f, -7f);
        cameraObject.transform.LookAt(stage.transform.position + Vector3.up * 1.35f);

        BuildPlatform();
        dogPivot = new GameObject("EquippedDogTurntable").transform;
        dogPivot.SetParent(stage.transform, false);
    }

    private void BuildPlatform()
    {
        Texture2D artwork = Resources.Load<Texture2D>("MenuRefresh/MenuPlatform");
        Shader shader = Resources.Load<Shader>("MenuRefresh/MenuPlatformArtwork");
        if (artwork == null || shader == null) return;
        GameObject go = new GameObject("ApprovedPlatformArtwork", typeof(MeshFilter), typeof(MeshRenderer));
        go.layer = PreviewLayer;
        go.transform.SetParent(stage.transform, false);
        platform = go.transform;
        // A camera-facing plate preserves the approved drawing's perspective
        // and contour while the dog remains a live, independently rotating model.
        platform.rotation = studioCamera.transform.rotation;
        float width = 4.5f * (CompactPreview ? .52f : 1f);
        float height = width * artwork.height / artwork.width;
        Vector3 feet = stage.transform.position + Vector3.up * .075f;
        // The centre of the turquoise standing surface lies at UV (.5, .60).
        // Put the plate behind all dog geometry, with that point aligned to its feet.
        platform.position = feet + studioCamera.transform.forward * 6f
            - studioCamera.transform.up * (height * .10f);
        platformMesh = new Mesh { name = "ApprovedPlatformQuad", hideFlags = HideFlags.HideAndDontSave };
        platformMesh.vertices = new[] {
            new Vector3(-width * .5f, -height * .5f, 0f),
            new Vector3(-width * .5f, height * .5f, 0f),
            new Vector3(width * .5f, height * .5f, 0f),
            new Vector3(width * .5f, -height * .5f, 0f)
        };
        platformMesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        platformMesh.triangles = new[] { 0, 1, 2, 2, 3, 0 };
        platformMesh.RecalculateBounds();
        go.GetComponent<MeshFilter>().sharedMesh = platformMesh;
        Material material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        material.SetTexture("_MainTex", artwork);
        stageMaterials.Add(material);
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private bool BuildEquippedDog(GameObject visual)
    {
        PlayerRunner player = FindAnyObjectByType<PlayerRunner>();
        Transform origin = player != null ? player.transform : null;
        if (visual == null && player != null)
        {
            Transform model = FindChild(player.transform, "BalloonDogModel");
            if (model != null) visual = model.gameObject;
        }
        if (visual == null) return false;
        if (origin == null) origin = visual.transform;
        ClearDog();
        dog = new GameObject("EquippedSkinVisual").transform;
        dog.SetParent(dogPivot, false);
        dogPivot.localRotation = Quaternion.identity;
        int count = 0;
        foreach (Renderer source in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (!source.enabled || !source.gameObject.activeInHierarchy || source is ParticleSystemRenderer ||
                source is TrailRenderer || source is LineRenderer) continue;
            Mesh mesh = null;
            if (source is SkinnedMeshRenderer skinned)
            {
                mesh = new Mesh { name = "MenuSkinSnapshot" };
                skinned.BakeMesh(mesh);
                bakedMeshes.Add(mesh);
            }
            else if (source.TryGetComponent(out MeshFilter filter)) mesh = filter.sharedMesh;
            if (mesh == null) continue;
            // Copy only mesh renderers. Player scripts, animators, physics,
            // audio, prefabs' Awake callbacks and shared materials stay untouched.
            GameObject part = new GameObject(source.name, typeof(MeshFilter), typeof(MeshRenderer));
            part.layer = PreviewLayer;
            part.transform.SetParent(dog, false);
            part.transform.localPosition = origin.InverseTransformPoint(source.transform.position);
            part.transform.localRotation = Quaternion.Inverse(origin.rotation) * source.transform.rotation;
            Vector3 scale = source.transform.lossyScale;
            Vector3 basis = origin.lossyScale;
            part.transform.localScale = new Vector3(scale.x / basis.x, scale.y / basis.y, scale.z / basis.z);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            Material[] originals = source.sharedMaterials;
            Material[] copies = new Material[Mathf.Max(1, originals.Length)];
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            source.GetPropertyBlock(properties);
            for (int i = 0; i < copies.Length; i++)
            {
                Material original = i < originals.Length ? originals[i] : null;
                Color color = Color.white;
                if (original != null)
                {
                    if (original.HasProperty("_BaseColor")) color = original.GetColor("_BaseColor");
                    else if (original.HasProperty("_Color")) color = original.GetColor("_Color");
                }
                Color blockColor = properties.GetColor("_BaseColor");
                if (blockColor.a <= 0f) blockColor = properties.GetColor("_Color");
                if (blockColor.a > 0f) color = blockColor;
                if (switcher == null || switcher.CurrentSkinIndex == 0)
                    color = BalloonDogEconomy.EquippedSkin.PrimaryColor;
                copies[i] = MakeMaterial(original, color);
                skinMaterials.Add(copies[i]);
            }
            part.GetComponent<Renderer>().sharedMaterials = copies;
            count++;
        }
        if (count == 0) { ClearDog(); return false; }
        Bounds bounds = DogBounds();
        float dimension = Mathf.Max(bounds.size.y, bounds.size.x * .85f, bounds.size.z * .85f);
        dog.localScale = Vector3.one * (2.8f / Mathf.Max(.01f, dimension));
        bounds = DogBounds();
        Vector3 localFeet = stage.transform.InverseTransformPoint(
            new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
        dog.localPosition -= localFeet;
        dog.localPosition += Vector3.up * .075f;
        dogPivot.localRotation = Quaternion.Euler(0f, 145f, 0f);
        FitCamera();
        return true;
    }

    private Material MakeMaterial(Material original, Color color)
    {
        Shader shader = Resources.Load<Shader>("MenuRefresh/MenuStudioBalloon");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        material.SetColor("_BaseColor", color);
        if (original != null)
        {
            string textureKey = original.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            if (original.HasProperty(textureKey) && original.GetTexture(textureKey) != null)
            {
                material.SetTexture("_BaseMap", original.GetTexture(textureKey));
                material.SetTextureScale("_BaseMap", original.GetTextureScale(textureKey));
                material.SetTextureOffset("_BaseMap", original.GetTextureOffset(textureKey));
            }
        }
        return material;
    }

    private Bounds DogBounds()
    {
        Renderer[] renderers = dog.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private void ResizeTarget()
    {
        float aspect = picture.rectTransform.rect.width / Mathf.Max(1f, picture.rectTransform.rect.height);
        aspect = Mathf.Clamp(aspect, .1f, 10f);
        if (target != null && Mathf.Abs(aspect - frameAspect) < .015f) return;
        ReleaseTarget();
        frameAspect = aspect;
        int width = 896;
        int height = Mathf.Max(128, Mathf.RoundToInt(width / aspect));
        if (height > 1400) { height = 1400; width = Mathf.RoundToInt(height * aspect); }
        target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            name = "EquippedSkinMenuPreview", antiAliasing = 4,
            filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        target.Create();
        studioCamera.targetTexture = target;
        studioCamera.aspect = (float)width / height;
        studioCamera.enabled = true;
        picture.texture = target;
        FitCamera();
    }

    private void FitCamera()
    {
        if (studioCamera == null || stage == null) return;
        float maxX = 0f, maxY = 0f;
        foreach (Renderer renderer in stage.GetComponentsInChildren<Renderer>())
        {
            Bounds b = renderer.bounds;
            Matrix4x4 toWorld = Matrix4x4.identity;
            // Fit the actual plate corners rather than its inflated world AABB.
            // This keeps the drawing's transparent margins inside the preview.
            if (renderer.transform == platform && platformMesh != null)
            {
                b = platformMesh.bounds;
                toWorld = platform.localToWorldMatrix;
            }
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 point = studioCamera.transform.InverseTransformPoint(toWorld.MultiplyPoint3x4(
                    b.center + Vector3.Scale(b.extents, new Vector3(x, y, z))));
                maxX = Mathf.Max(maxX, Mathf.Abs(point.x));
                maxY = Mathf.Max(maxY, Mathf.Abs(point.y));
            }
        }
        studioCamera.orthographicSize = Mathf.Max(maxY, maxX / Mathf.Max(.1f, studioCamera.aspect)) * 1.12f;
    }

    private GameObject MakeUi(string name, System.Type graphic)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), graphic);
        if (!Application.isPlaying) go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.SetParent(transform, false);
        return go;
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            Transform found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void ClearDog()
    {
        if (dog != null) { dog.gameObject.SetActive(false); Dispose(dog.gameObject); dog = null; }
        foreach (Material material in skinMaterials) Dispose(material);
        skinMaterials.Clear();
        foreach (Mesh mesh in bakedMeshes) Dispose(mesh);
        bakedMeshes.Clear();
    }

    private void ReleaseTarget()
    {
        if (studioCamera != null) studioCamera.targetTexture = null;
        if (picture != null) picture.texture = null;
        if (target != null) { target.Release(); Dispose(target); target = null; }
    }

    private void OnDisable()
    {
        BalloonDogEconomy.Changed -= MarkDirty;
        if (switcher != null) switcher.SkinChanged -= HandleSkinChanged;
        switcher = null;
        RestoreWorldCamera();
        if (studioCamera != null) studioCamera.enabled = false;
        ReleaseTarget();
        ClearDog();
        foreach (Material material in stageMaterials) Dispose(material);
        stageMaterials.Clear();
        if (stage != null) { stage.SetActive(false); Dispose(stage); stage = null; }
        platform = null;
        if (platformMesh != null) { Dispose(platformMesh); platformMesh = null; }
    }

    private void OnDestroy()
    {
        if (picture != null) Dispose(picture.gameObject);
    }

    private static void Dispose(Object item)
    {
        if (item == null) return;
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }
}
