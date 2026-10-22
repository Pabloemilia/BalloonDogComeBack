using System.Collections.Generic;
using UnityEngine;

// Every runner owns this guard, including runners created after a scene reload.
// Keep it alive so newly selected skins cannot reintroduce a trail.
[DisallowMultipleComponent]
[DefaultExecutionOrder(32001)]
public sealed class PlayerTrailCleanupRuntime : MonoBehaviour
{
    private readonly List<TrailRenderer> trails = new List<TrailRenderer>();

    private void OnEnable() => RemoveTrails();
    private void LateUpdate() => RemoveTrails();

    private void RemoveTrails()
    {
        GetComponentsInChildren<TrailRenderer>(true, trails);
        foreach (TrailRenderer trail in trails)
        {
            if (trail == null) continue;
            trail.emitting = false;
            trail.enabled = false;
            trail.Clear();
            // Remove only the renderer: a skin may share this object with its mesh.
            Destroy(trail);
        }
        trails.Clear();
    }
}
