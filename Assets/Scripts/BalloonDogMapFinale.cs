using UnityEngine;

/// <summary>Legacy road-bounds compatibility. No finish gate, bonus flight or completion screen is built.</summary>
public sealed class BalloonDogMapFinale : MonoBehaviour
{
    public static float FinishZ { get; private set; } = 65f;
    public static float BonusStartZ { get; private set; } = 68f;
    public static float RoadWidth { get; private set; } = 6f;

    public static void RefreshCourseLayout()
    {
        Renderer road = GameObject.Find("Road")?.GetComponent<Renderer>();
        if (road == null) { FinishZ = 65f; BonusStartZ = 68f; RoadWidth = 6f; return; }
        Bounds bounds = road.bounds;
        BonusStartZ = bounds.max.z;
        FinishZ = BonusStartZ - 3f;
        RoadWidth = bounds.size.x;
    }

    // Kept for older editor tools; these no longer drive gameplay.
    public static int MultiplierForAir(float air) => Mathf.Clamp(1 + Mathf.FloorToInt(Mathf.Clamp01(air) * 10f), 1, 10);
    public static float LandingZForAir(float air) => BonusStartZ + (MultiplierForAir(air) - .5f) * 6f;
    private void Awake() { RefreshCourseLayout(); enabled = false; }
}
