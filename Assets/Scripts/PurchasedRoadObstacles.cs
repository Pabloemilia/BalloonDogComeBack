using UnityEngine;

/// <summary>Fits the purchased scene prefabs to one side of the nine metre road.</summary>
public static class PurchasedRoadObstacles
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureSceneObstacles()
    {
        foreach (Transform root in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (!root.name.StartsWith("PackObstacle_")) continue;
            Vector3 anchor = root.position;
            // Keep the authored animated parts, but prevent root motion moving the trap.
            foreach (Animator animator in root.GetComponentsInChildren<Animator>())
            {
                animator.applyRootMotion = false;
                animator.Rebind();
                animator.Update(0f);
            }
            foreach (ParticleSystem particles in root.GetComponentsInChildren<ParticleSystem>())
                particles.gameObject.SetActive(false);
            Renderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) continue;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float fit = Mathf.Min(2.2f / Mathf.Max(bounds.size.x, 0.01f),
                2.8f / Mathf.Max(bounds.size.y, 0.01f),
                3f / Mathf.Max(bounds.size.z, 0.01f));
            root.localScale *= fit;
            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            root.position += new Vector3(anchor.x - bounds.center.x, -bounds.min.y,
                anchor.z - bounds.center.z);
            // A static trigger belongs to the fitted prefab, not its animated moving parts.
            foreach (Collider collider in root.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            BoxCollider hitbox = root.gameObject.AddComponent<BoxCollider>();
            hitbox.center = root.InverseTransformPoint(bounds.center);
            Vector3 scale = root.lossyScale;
            hitbox.size = new Vector3(bounds.size.x / Mathf.Abs(scale.x),
                bounds.size.y / Mathf.Abs(scale.y), bounds.size.z / Mathf.Abs(scale.z));
            root.gameObject.AddComponent<Obstacle>().ConfigureNormal();
        }
    }
}
