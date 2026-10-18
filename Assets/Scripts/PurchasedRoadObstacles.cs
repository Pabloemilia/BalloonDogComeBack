using UnityEngine;

/// <summary>Compatibility entry point for preserving purchased obstacle placements.</summary>
public static class PurchasedRoadObstacles
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureSceneObstacles()
    {
        // Keep authored offsets, scale and rotation. The retired fitter changed
        // the layout on Play and could snap independently placed traps together.
        AuthoredObstaclePlacement.InstallScene();
    }

    internal static Bounds BoundsOf(MeshRenderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        foreach (MeshRenderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}
