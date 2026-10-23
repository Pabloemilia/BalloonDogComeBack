using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>A disposable visual snapshot; never changes the player's physics or skin transforms.</summary>
public sealed class BalloonDogDeathAnimation : MonoBehaviour
{
    public const float Duration = .65f;
    private const float PopAt = .22f;
    private sealed class HiddenVisual
    {
        public Renderer renderer;
        public bool wasHidden;
    }
    private sealed class Fragment
    {
        public Transform transform;
        public Vector3 velocity, axis;
        public Quaternion rotation;
        public float size, spin;
    }
    private readonly List<HiddenVisual> originals = new List<HiddenVisual>();
    private readonly List<Fragment> fragments = new List<Fragment>();
    private readonly List<Mesh> ownedMeshes = new List<Mesh>();
    private Transform effects, pose;
    private Material fragmentMaterial;
    private Action completed;
    private Bounds bounds;
    private Color color;
    private float elapsed;
    private bool popped;
    public bool IsPlaying { get; private set; }

    public bool Play(Transform player, Action onCompleted)
    {
        Restore();
        if (player == null) return false;
        var visible = new List<Renderer>();
        foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || renderer.forceRenderingOff) continue;
            var filter = renderer.GetComponent<MeshFilter>();
            bool mesh = renderer is MeshRenderer && filter != null && filter.sharedMesh != null;
            bool skinned = renderer is SkinnedMeshRenderer skin && skin.sharedMesh != null;
            if (!mesh && !skinned) continue;
            if (visible.Count == 0) bounds = renderer.bounds;
            else bounds.Encapsulate(renderer.bounds);
            visible.Add(renderer);
        }
        if (visible.Count == 0) return false;
        effects = new GameObject("BalloonDogDeathEffects").transform;
        effects.position = bounds.center;
        pose = new GameObject("DeathPose").transform;
        pose.SetParent(effects, false);
        foreach (Renderer renderer in visible)
        {
            var ghost = new GameObject("Death_" + renderer.name, typeof(MeshFilter), typeof(MeshRenderer));
            ghost.layer = renderer.gameObject.layer;
            ghost.transform.SetParent(pose, false);
            ghost.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
            ghost.transform.localScale = renderer.transform.lossyScale;
            Mesh mesh;
            if (renderer is SkinnedMeshRenderer skinned)
            {
                mesh = new Mesh { name = "DeathPoseSnapshot" };
                ownedMeshes.Add(mesh);
                skinned.BakeMesh(mesh);
            }
            else mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            ghost.GetComponent<MeshFilter>().sharedMesh = mesh;
            var copy = ghost.GetComponent<MeshRenderer>();
            copy.sharedMaterials = renderer.sharedMaterials;
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            copy.SetPropertyBlock(properties);
            copy.shadowCastingMode = ShadowCastingMode.Off;
            copy.receiveShadows = false;
            originals.Add(new HiddenVisual { renderer = renderer, wasHidden = renderer.forceRenderingOff });
            renderer.forceRenderingOff = true;
        }
        color = player.GetComponent<BalloonDogSkinSwitcher>() != null
            ? BalloonDogEconomy.EquippedSkin.PrimaryColor : new Color(1f, .15f, .12f);
        elapsed = 0f;
        popped = false;
        completed = onCompleted;
        IsPlaying = true;
        return true;
    }

    private void Update() => Advance(Time.unscaledDeltaTime);

    public void Advance(float unscaledSeconds)
    {
        if (!IsPlaying) return;
        elapsed += Mathf.Max(0f, unscaledSeconds);
        if (elapsed < PopAt)
        {
            float inflate = Mathf.Clamp01(elapsed / .1f);
            float squash = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - .1f) / (PopAt - .1f)));
            pose.localScale = new Vector3(
                Mathf.Lerp(1f + .1f * inflate, .86f, squash),
                Mathf.Lerp(1f + .08f * inflate, .12f, squash),
                Mathf.Lerp(1f + .1f * inflate, .86f, squash));
            pose.localPosition = Vector3.up * (.06f * inflate);
        }
        else
        {
            if (!popped) Pop();
            float age = elapsed - PopAt;
            float remaining = Mathf.Clamp01(1f - age / (Duration - PopAt));
            foreach (Fragment fragment in fragments)
            {
                fragment.transform.localPosition = fragment.velocity * age + Vector3.down * (2.5f * age * age);
                fragment.transform.localRotation = Quaternion.AngleAxis(fragment.spin * age, fragment.axis) * fragment.rotation;
                fragment.transform.localScale = Vector3.one * (fragment.size * Mathf.Sqrt(remaining));
            }
        }
        if (elapsed >= Duration) Finish();
    }

    private void Pop()
    {
        popped = true;
        pose.gameObject.SetActive(false);
        GameAudioController.PlayDeath();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) return;
        fragmentMaterial = new Material(shader) { name = "TransientBalloonFragments" };
        if (fragmentMaterial.HasProperty("_Smoothness")) fragmentMaterial.SetFloat("_Smoothness", .55f);
        var mesh = new Mesh { name = "BalloonFragment" };
        Vector3 a = new Vector3(-.5f, -.4f, 0f), b = new Vector3(.5f, -.3f, 0f), c = new Vector3(.1f, .6f, 0f);
        mesh.vertices = new[] { a, b, c, a, b, c };
        mesh.triangles = new[] { 0, 1, 2, 5, 4, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        ownedMeshes.Add(mesh);
        var random = new System.Random(37);
        float size = Mathf.Clamp(bounds.size.magnitude, .5f, 3f);
        for (int i = 0; i < 16; i++)
        {
            var bit = new GameObject("BalloonFragment_" + i, typeof(MeshFilter), typeof(MeshRenderer));
            bit.layer = originals[0].renderer != null ? originals[0].renderer.gameObject.layer : 0;
            bit.transform.SetParent(effects, false);
            bit.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = bit.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = fragmentMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var properties = new MaterialPropertyBlock();
            Color tint = Color.Lerp(color, Color.white, (float)random.NextDouble() * .4f);
            properties.SetColor("_BaseColor", tint);
            properties.SetColor("_Color", tint);
            renderer.SetPropertyBlock(properties);
            float angle = i * Mathf.PI * 2f / 16f;
            fragments.Add(new Fragment
            {
                transform = bit.transform,
                velocity = new Vector3(Mathf.Cos(angle), .5f + (float)random.NextDouble(), Mathf.Sin(angle) * .7f) * size * 1.8f,
                axis = new Vector3(.4f, 1f, .7f).normalized,
                rotation = Quaternion.Euler(i * 29f, i * 47f, i * 71f),
                size = size * Mathf.Lerp(.06f, .1f, (float)random.NextDouble()),
                spin = 240f + (float)random.NextDouble() * 240f
            });
        }
    }

    public void Finish()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        ClearEffects();
        Action callback = completed;
        completed = null;
        callback?.Invoke();
    }

    public void Restore()
    {
        IsPlaying = false;
        completed = null;
        foreach (HiddenVisual visual in originals)
            if (visual.renderer != null) visual.renderer.forceRenderingOff = visual.wasHidden;
        originals.Clear();
        ClearEffects();
    }

    private void ClearEffects()
    {
        if (effects != null) { effects.gameObject.SetActive(false); Dispose(effects.gameObject); }
        effects = pose = null;
        fragments.Clear();
        foreach (Mesh mesh in ownedMeshes) Dispose(mesh);
        ownedMeshes.Clear();
        if (fragmentMaterial != null) Dispose(fragmentMaterial);
        fragmentMaterial = null;
    }

    private static void Dispose(UnityEngine.Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
    private void OnDisable() => Restore();
    private void OnDestroy() => Restore();
}
