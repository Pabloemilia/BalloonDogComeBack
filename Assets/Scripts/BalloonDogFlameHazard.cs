using UnityEngine;
using UnityEngine.VFX;

/// <summary>Lethal contact for the dense core of the purchased flame jet.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(210)]
public sealed class BalloonDogFlameHazard : MonoBehaviour
{
    // Flamethrower.vfx emits along local +Z at 10-15 units/s; the fire
    // systems live for 0.25-0.45s. Use the slower core, excluding trailing smoke.
    private const float JetSpeed = 10f;
    private const float JetLifetime = .45f;
    private const float JetRadius = .35f;
    private VisualEffect effect;
    private Renderer visual;
    private FlameControl source;
    private readonly Collider[] candidates = new Collider[32];

    public static void Install(GameObject view)
    {
        foreach (VisualEffect flame in view.GetComponentsInChildren<VisualEffect>(true))
        {
            if (flame.visualEffectAsset == null || flame.visualEffectAsset.name != "Flamethrower") continue;
            FlameControl timer = flame.GetComponentInParent<FlameControl>();
            if (timer == null) continue;
            var hazard = flame.GetComponent<BalloonDogFlameHazard>() ??
                         flame.gameObject.AddComponent<BalloonDogFlameHazard>();
            hazard.effect = flame;
            hazard.visual = flame.GetComponent<Renderer>();
            hazard.source = timer;
        }
    }

    private bool TryGetJet(out Vector3 start, out Vector3 end, out float radius)
    {
        start = end = Vector3.zero;
        radius = 0f;
        if (source == null || !source.isActiveAndEnabled || effect == null || !effect.isActiveAndEnabled
            || effect.visualEffectAsset == null || visual == null || !visual.enabled || visual.forceRenderingOff)
            return false;
        float oldest = Mathf.Min(JetLifetime, Time.time - source.EmissionStartedAt);
        float youngest = source.IsEmitting ? 0f : Time.time - source.EmissionStoppedAt;
        if (!TryGetAgeRange(oldest, youngest, out float from, out float to)) return false;
        // Follow the nozzle's animated transform, exact authored scale and origin shifts.
        start = transform.TransformPoint(Vector3.forward * (from * JetSpeed));
        end = transform.TransformPoint(Vector3.forward * (to * JetSpeed));
        Vector3 scale = transform.lossyScale;
        radius = JetRadius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        Vector3 direction = end - start;
        float length = direction.magnitude;
        radius = Mathf.Min(radius, length * .5f);
        if (length <= .0001f) return false;
        direction /= length;
        // Keep rounded capsule caps inside the live interval, including startup.
        start += direction * radius;
        end -= direction * radius;
        return radius > 0f;
    }

    private static bool TryGetAgeRange(float oldest, float youngest, out float from, out float to)
    {
        // A jet grows from its nozzle, then its tail travels outward after Stop.
        // Never leave a full-length invisible collider behind a fading flame.
        from = Mathf.Max(.015f, youngest);
        to = Mathf.Min(JetLifetime, oldest);
        return to > from;
    }

    private void FixedUpdate()
    {
        if (Time.timeScale <= 0f || GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (!TryGetJet(out Vector3 start, out Vector3 end, out float radius)) return;
        int count = Physics.OverlapCapsuleNonAlloc(start, end, radius, candidates, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++) CheckContact(candidates[i]);
    }

    private void CheckContact(Collider other)
    {
        if (other == null || !other.enabled || other.isTrigger || Time.timeScale <= 0f
            || GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        AirController air = other.GetComponentInParent<AirController>();
        if (air == null || air.IsEmpty || air.IsDamageProtected) return;
        PlayerRunner runner = air.GetComponent<PlayerRunner>();
        if (runner != null && (!runner.enabled || !runner.MovementEnabled)) return;
        GameAudioController.PlayHit();
        // Independent of solid-part contact consumption. Fire is always lethal,
        // and the nozzle/effect remains in the world after the hit.
        air.EmptyAir("ALEVDE YANDIN");
    }

    private void LateUpdate()
    {
        if (effect != null)
            effect.pause = Time.timeScale <= 0f || GameManager.Instance != null && GameManager.Instance.IsGameOver;
    }
}
