using UnityEngine;

/// <summary>Fits complete animation envelopes inside fixed authored road anchors.</summary>
public static class PurchasedRoadObstacles
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureSceneObstacles()
    {
        foreach (Transform root in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (!root.name.StartsWith("PackObstacle_") || root.parent == null ||
                !root.parent.name.StartsWith("RoadTrapAnchor_")) continue;
            Transform anchor = root.parent;
            Transform visual = new GameObject("FittedVisual").transform;
            visual.SetParent(anchor, false);
            root.SetParent(visual, false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            foreach (ParticleSystem particles in root.GetComponentsInChildren<ParticleSystem>())
                particles.gameObject.SetActive(false);
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) continue;
            Animator[] animators = root.GetComponentsInChildren<Animator>();
            Bounds envelope = BoundsOf(renderers);
            // Include movement over the whole cycle, not only the first animation pose.
            foreach (Animator animator in animators)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                if (animator.runtimeAnimatorController == null) continue;
                foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                {
                    for (int frame = 0; frame <= 32; frame++)
                    {
                        clip.SampleAnimation(animator.gameObject, clip.length * frame / 32f);
                        envelope.Encapsulate(BoundsOf(renderers));
                    }
                }
                animator.Rebind();
                animator.Update(0f);
            }
            Vector3 center = anchor.InverseTransformPoint(envelope.center);
            float fit = Mathf.Min(1.5f / Mathf.Max(envelope.size.x, .01f),
                2.4f / Mathf.Max(envelope.size.y, .01f),
                3f / Mathf.Max(envelope.size.z, .01f));
            // A small extra margin covers interpolation between the sampled poses.
            fit *= .95f;
            visual.localScale = Vector3.one * fit;
            visual.localPosition = new Vector3(-center.x, -center.y + envelope.extents.y,
                -center.z) * fit;
            foreach (Collider collider in root.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            BoxCollider hitbox = anchor.gameObject.AddComponent<BoxCollider>();
            hitbox.isTrigger = true;
            anchor.gameObject.AddComponent<Obstacle>().ConfigureNormal();
            anchor.gameObject.AddComponent<PurchasedRoadObstacleBounds>().Configure(renderers, hitbox);
        }
    }

    internal static Bounds BoundsOf(MeshRenderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        foreach (MeshRenderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}
