using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>Replays the package's dust once at each authored ground impact.</summary>
[DisallowMultipleComponent]
public sealed class BalloonDogTrapImpactVfx : MonoBehaviour
{
    private Animator source;
    private VisualEffect[] dust;
    private float impactPhase;
    private float previousTime;
    private int previousState;
    private bool hasSample;

    public static void Install(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        AnimationClip impactClip = null;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (clip != null && (clip.name == "Obstacle 8" || clip.name == "Obstacle 22"))
                impactClip = clip;
        if (impactClip == null || impactClip.length <= 0f) return;

        var effects = new List<VisualEffect>();
        foreach (VisualEffect effect in animator.GetComponentsInChildren<VisualEffect>(true))
            if (effect.visualEffectAsset != null && effect.visualEffectAsset.name == "SmokePoof")
                effects.Add(effect);
        if (effects.Count == 0) return;

        var controller = animator.GetComponent<BalloonDogTrapImpactVfx>() ??
                         animator.gameObject.AddComponent<BalloonDogTrapImpactVfx>();
        controller.source = animator;
        controller.dust = effects.ToArray();
        // Both purchased clips reach their -90 degree ground strike at 0.25s.
        // Normalized time keeps this aligned with animator speed and every loop.
        controller.impactPhase = .25f / impactClip.length;
        controller.ResetForReuse();
    }

    public void ResetForReuse()
    {
        hasSample = false;
        if (dust == null) return;
        foreach (VisualEffect effect in dust)
        {
            if (effect == null) continue;
            effect.initialEventName = "OnStop";
            effect.Reinit();
            effect.Stop();
        }
    }

    // Call after the pool has applied its random pose, so a spawn/teleport is
    // never mistaken for an impact that occurred earlier in that animation.
    public void Synchronize()
    {
        if (source == null || source.layerCount == 0) { hasSample = false; return; }
        AnimatorStateInfo state = source.GetCurrentAnimatorStateInfo(0);
        previousTime = state.normalizedTime;
        previousState = state.fullPathHash;
        hasSample = true;
    }

    private void LateUpdate()
    {
        if (source == null || dust == null || source.layerCount == 0) return;
        bool running = Time.deltaTime > 0f && source.isActiveAndEnabled;
        foreach (VisualEffect effect in dust)
            if (effect != null) effect.pause = !running;
        if (!running) { Synchronize(); return; }

        AnimatorStateInfo state = source.GetCurrentAnimatorStateInfo(0);
        if (hasSample && previousState == state.fullPathHash &&
            CrossedImpact(previousTime, state.normalizedTime, impactPhase))
        {
            foreach (VisualEffect effect in dust)
            {
                if (effect == null || !effect.isActiveAndEnabled) continue;
                effect.Reinit();
                effect.Play();
            }
        }
        previousTime = state.normalizedTime;
        previousState = state.fullPathHash;
        hasSample = true;
    }

    private static bool CrossedImpact(float previous, float current, float phase)
        => current > previous && Mathf.Floor(current - phase) > Mathf.Floor(previous - phase);

    private void OnDisable() => ResetForReuse();
}
