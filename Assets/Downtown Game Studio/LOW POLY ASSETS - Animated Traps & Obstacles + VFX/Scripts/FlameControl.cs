using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Make sure this script is placed on the "FlameSpawner" and that "Flame1", "Flame2" (...) are set as children of "FlameSpawner".
/// </summary>

public class FlameControl : MonoBehaviour
{
    public VisualEffect[] flameSpawners;
    private bool isPlaying = false;

    public bool IsEmitting { get; private set; }
    public float EmissionStartedAt { get; private set; }
    public float EmissionStoppedAt { get; private set; }

    void OnEnable()
    {
        CancelInvoke(nameof(TurnOnOffFire));
        flameSpawners = gameObject.GetComponentsInChildren<VisualEffect>(true);
        isPlaying = false;
        IsEmitting = true;
        EmissionStartedAt = Time.time;
        EmissionStoppedAt = float.NegativeInfinity;
        foreach (VisualEffect effect in flameSpawners)
        {
            // Pool reuse must clear particles left at the previous road position.
            // Child VFX OnEnable may run after this parent callback. Its own
            // initial event must agree with the emitting state in either order.
            effect.initialEventName = "OnPlay";
            effect.Reinit();
            effect.Play();
        }
        InvokeRepeating("TurnOnOffFire", 1, 2); //Controls the spawning of fire. 
    }

    private void TurnOnOffFire()
    {
        isPlaying = !isPlaying;

        if (IsEmitting != isPlaying)
        {
            IsEmitting = isPlaying;
            if (IsEmitting) EmissionStartedAt = Time.time;
            else EmissionStoppedAt = Time.time;
        }

        foreach (VisualEffect effect in flameSpawners)
        {
            if (isPlaying)
            {
                effect.Play();
            }
            else
            {
                effect.Stop();
            }
        }
    }

    void OnDisable()
    {
        CancelInvoke(nameof(TurnOnOffFire));
        IsEmitting = false;
        EmissionStoppedAt = Time.time;
        if (flameSpawners == null) return;
        foreach (VisualEffect effect in flameSpawners)
        {
            if (effect == null) continue;
            effect.Reinit();
            effect.Stop();
        }
    }
}
