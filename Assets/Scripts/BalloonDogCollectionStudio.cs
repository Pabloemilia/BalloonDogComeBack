using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>Six static model thumbnails, rendered by one isolated camera into a shared atlas.</summary>
public sealed class BalloonDogCollectionStudio : MonoBehaviour
{
    private sealed class Entry
    {
        public string SkinId;
        public RawImage Picture;
    }

    private const int PreviewLayer = 29;
    private readonly List<Entry> entries = new List<Entry>();
    private readonly List<Material> materials = new List<Material>();
    private readonly List<Mesh> bakedMeshes = new List<Mesh>();
    private BalloonDogSkinSwitcher switcher;
    private GameObject stage;
    private GameObject models;
    private Camera studioCamera;
    private RenderTexture target;
    private int page;
    private bool dirty = true;
    private float nextRetry;

    public void Register(string skinId, RawImage picture)
    {
        int slot = entries.Count % 6;
        picture.uvRect = new Rect((slot % 2) * .5f, (2 - slot / 2) / 3f, .5f, 1f / 3f);
        entries.Add(new Entry { SkinId = skinId, Picture = picture });
        MarkDirty();
    }

    public void SetPage(int value)
    {
        value = Mathf.Max(0, value);
        if (page == value) return;
        page = value;
        MarkDirty();
    }

    private void OnEnable()
    {
        BalloonDogEconomy.Changed += MarkDirty;
        RenderPipelineManager.endCameraRendering += AfterCamera;
        Camera.onPostRender += AfterBuiltInCamera;
        MarkDirty();
    }

    private void MarkDirty() { dirty = true; nextRetry = 0f; }

    private void LateUpdate()
    {
        if (!dirty || Time.realtimeSinceStartup < nextRetry) return;
        nextRetry = Time.realtimeSinceStartup + .3f;
        if (switcher == null) switcher = FindAnyObjectByType<BalloonDogSkinSwitcher>();
        if (switcher == null) return;
        dirty = !BuildPage();
    }

    private void EnsureStudio()
    {
        if (stage != null) return;
        stage = new GameObject("__BalloonDogCollectionStudio");
        stage.hideFlags = HideFlags.HideAndDontSave;
        stage.transform.position = new Vector3(20000f, -20000f, 20000f);
        GameObject cameraObject = new GameObject("CollectionAtlasCamera", typeof(Camera));
        cameraObject.transform.SetParent(stage.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -30f);
        studioCamera = cameraObject.GetComponent<Camera>();
        studioCamera.clearFlags = CameraClearFlags.SolidColor;
        studioCamera.backgroundColor = Color.clear;
        studioCamera.orthographic = true;
        studioCamera.orthographicSize = 5.25f;
        studioCamera.nearClipPlane = .1f;
        studioCamera.farClipPlane = 60f;
        studioCamera.cullingMask = 1 << PreviewLayer;
        studioCamera.allowHDR = false;
        studioCamera.allowMSAA = true;
        studioCamera.useOcclusionCulling = false;
        target = new RenderTexture(768, 672, 24, RenderTextureFormat.ARGB32)
        {
            name = "CollectionSkinAtlas", antiAliasing = 2,
            filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        target.Create();
        studioCamera.targetTexture = target;
        studioCamera.aspect = 768f / 672f;
        foreach (Entry entry in entries) entry.Picture.texture = target;
    }

    private bool BuildPage()
    {
        EnsureStudio();
        studioCamera.enabled = true;
        ClearPage();
        models = new GameObject("VisibleSkinModels");
        models.transform.SetParent(stage.transform, false);
        bool complete = true;
        for (int slot = 0; slot < 6; slot++)
        {
            int index = page * 6 + slot;
            if (index >= entries.Count) break;
            Entry entry = entries[index];
            GameObject source = switcher.GetSkinPreviewSource(entry.SkinId);
            if (source == null)
            {
                entry.Picture.color = Color.clear;
                complete = false;
                continue;
            }
            bool built = BuildModel(source, entry.SkinId, slot);
            entry.Picture.color = built ? Color.white : Color.clear;
            complete &= built;
        }
        return complete;
    }

    private bool BuildModel(GameObject source, string skinId, int slot)
    {
        Transform frame = new GameObject(skinId).transform;
        frame.SetParent(models.transform, false);
        frame.localPosition = new Vector3((slot % 2 - .5f) * 6f, (1 - slot / 2) * 3.5f, 0f);
        frame.localRotation = Quaternion.Euler(8f, 145f, 0f);
        bool owned = BalloonDogEconomy.IsOwned(skinId);
        Transform visual = CopyVisual(source.transform, frame, owned, skinId);
        if (skinId == "classic")
        {
            // The starter's parent carries part of its pose in the gameplay rig.
            visual.localRotation = Quaternion.Inverse(switcher.transform.rotation) * source.transform.rotation;
            Vector3 basis = switcher.transform.lossyScale;
            Vector3 scale = source.transform.lossyScale;
            visual.localScale = new Vector3(scale.x / NonZero(basis.x),
                scale.y / NonZero(basis.y), scale.z / NonZero(basis.z));
        }
        visual.localPosition = Vector3.zero;
        Renderer[] renderers = frame.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return false;
        Bounds bounds = ModelBounds(renderers);
        float scaleToFit = Mathf.Min(5.25f / Mathf.Max(.001f, bounds.size.x),
            2.95f / Mathf.Max(.001f, bounds.size.y), 8f / Mathf.Max(.001f, bounds.size.z));
        visual.localScale *= scaleToFit;
        bounds = ModelBounds(renderers);
        // Bounds are expressed in the camera's axes, so every shape is centred
        // and fitted independently, including horns, wings and tails.
        Vector3 offset = new Vector3(frame.localPosition.x - bounds.center.x,
            frame.localPosition.y - bounds.center.y, -bounds.center.z);
        visual.position += stage.transform.TransformVector(offset);
        return true;
    }

    private Transform CopyVisual(Transform source, Transform parent, bool owned, string skinId)
    {
        // Copy transforms and mesh data only. No prefab scripts, animators,
        // colliders or audio are instantiated or activated in the collection.
        GameObject copy = new GameObject(source.name);
        copy.layer = PreviewLayer;
        copy.transform.SetParent(parent, false);
        copy.transform.localPosition = source.localPosition;
        copy.transform.localRotation = source.localRotation;
        copy.transform.localScale = source.localScale;
        Renderer renderer = source.GetComponent<Renderer>();
        if (renderer != null && renderer.enabled && !(renderer is ParticleSystemRenderer) &&
            !(renderer is TrailRenderer) && !(renderer is LineRenderer))
        {
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer skinned)
            {
                mesh = new Mesh { name = "CollectionSkinSnapshot" };
                skinned.BakeMesh(mesh);
                bakedMeshes.Add(mesh);
            }
            else if (source.TryGetComponent(out MeshFilter filter)) mesh = filter.sharedMesh;
            if (mesh != null)
            {
                copy.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer output = copy.AddComponent<MeshRenderer>();
                output.shadowCastingMode = ShadowCastingMode.Off;
                output.receiveShadows = false;
                Material[] originals = renderer.sharedMaterials;
                Material[] copies = new Material[Mathf.Max(1, originals.Length)];
                for (int i = 0; i < copies.Length; i++)
                    copies[i] = CopyMaterial(i < originals.Length ? originals[i] : null, owned, skinId);
                output.sharedMaterials = copies;
            }
        }
        foreach (Transform child in source)
        {
            Transform childCopy = CopyVisual(child, copy.transform, owned, skinId);
            childCopy.gameObject.SetActive(child.gameObject.activeSelf);
        }
        return copy.transform;
    }

    private Material CopyMaterial(Material original, bool owned, string skinId)
    {
        Material copy = new Material(Resources.Load<Shader>("MenuRefresh/MenuStudioBalloon"))
            { hideFlags = HideFlags.HideAndDontSave };
        Color color = BalloonDogEconomy.FindSkin(skinId).PrimaryColor;
        if (original != null)
        {
            if (original.HasProperty("_BaseColor")) color = original.GetColor("_BaseColor");
            else if (original.HasProperty("_Color")) color = original.GetColor("_Color");
            string map = original.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            if (original.HasProperty(map) && original.GetTexture(map) != null)
            {
                copy.SetTexture("_BaseMap", original.GetTexture(map));
                copy.SetTextureScale("_BaseMap", original.GetTextureScale(map));
                copy.SetTextureOffset("_BaseMap", original.GetTextureOffset(map));
            }
        }
        copy.SetColor("_BaseColor", skinId == "classic" ? BalloonDogEconomy.FindSkin(skinId).PrimaryColor : color);
        copy.SetFloat("_Silhouette", owned ? 0f : 1f);
        materials.Add(copy);
        return copy;
    }

    private Bounds ModelBounds(Renderer[] renderers)
    {
        Bounds result = default;
        bool first = true;
        foreach (Renderer renderer in renderers)
        {
            Bounds b = renderer.bounds;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 point = stage.transform.InverseTransformPoint(b.center +
                    Vector3.Scale(b.extents, new Vector3(x, y, z)));
                if (first) { result = new Bounds(point, Vector3.zero); first = false; }
                else result.Encapsulate(point);
            }
        }
        return result;
    }

    private static float NonZero(float value) => Mathf.Abs(value) < .0001f ? 1f : value;

    // These are static thumbnails: render once after a page/ownership change,
    // then let the UI reuse the atlas without six live preview cameras.
    private void AfterCamera(ScriptableRenderContext context, Camera camera) => AfterBuiltInCamera(camera);
    private void AfterBuiltInCamera(Camera camera)
    {
        if (camera == studioCamera) studioCamera.enabled = false;
    }

    private void ClearPage()
    {
        if (models != null) { models.SetActive(false); Dispose(models); models = null; }
        foreach (Material material in materials) Dispose(material);
        materials.Clear();
        foreach (Mesh mesh in bakedMeshes) Dispose(mesh);
        bakedMeshes.Clear();
    }

    private void OnDisable()
    {
        BalloonDogEconomy.Changed -= MarkDirty;
        RenderPipelineManager.endCameraRendering -= AfterCamera;
        Camera.onPostRender -= AfterBuiltInCamera;
        foreach (Entry entry in entries)
            if (entry.Picture != null) { entry.Picture.texture = null; entry.Picture.color = Color.clear; }
        ClearPage();
        if (studioCamera != null) { studioCamera.enabled = false; studioCamera.targetTexture = null; }
        if (target != null) { target.Release(); Dispose(target); target = null; }
        if (stage != null) { stage.SetActive(false); Dispose(stage); stage = null; }
        studioCamera = null;
    }

    private static void Dispose(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
