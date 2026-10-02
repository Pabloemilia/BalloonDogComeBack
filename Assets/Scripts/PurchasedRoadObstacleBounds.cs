using UnityEngine;

/// <summary>The hit area follows the visible animation rather than its entire swept volume.</summary>
public sealed class PurchasedRoadObstacleBounds : MonoBehaviour
{
    private MeshRenderer[] renderers;
    private BoxCollider hitbox;

    public void Configure(MeshRenderer[] meshes, BoxCollider collider)
    {
        renderers = meshes;
        hitbox = collider;
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (hitbox == null || !hitbox.enabled || renderers == null) return;
        Bounds bounds = PurchasedRoadObstacles.BoundsOf(renderers);
        hitbox.center = transform.InverseTransformPoint(bounds.center);
        Vector3 scale = transform.lossyScale;
        hitbox.size = new Vector3(bounds.size.x / Mathf.Max(Mathf.Abs(scale.x), .001f),
            bounds.size.y / Mathf.Max(Mathf.Abs(scale.y), .001f),
            bounds.size.z / Mathf.Max(Mathf.Abs(scale.z), .001f));
    }
}
