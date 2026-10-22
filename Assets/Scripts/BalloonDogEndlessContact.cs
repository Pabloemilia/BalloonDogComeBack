using UnityEngine;

/// <summary>One reusable contact owner. Never destroys itself or its road chunk.</summary>
public sealed class BalloonDogEndlessContact : MonoBehaviour
{
    public bool IsPickup;
    private bool consumed;
    private bool heavyImpact;
    public void ConfigureHeavyImpact() => heavyImpact = true;
    public void ResetContact(bool active) { consumed=false;gameObject.SetActive(active); }
    private void OnTriggerEnter(Collider other)
    {
        // Damage proxies must confirm visible surface contact first.
        if(!IsPickup) return;
        ApplyContact(other);
    }
    public void ApplyVisibleHit(Collider other)
    {
        if(IsPickup) return;
        ApplyContact(other);
    }
    private void ApplyContact(Collider other)
    {
        if(other==null || !other.enabled || other.isTrigger) return;
        if(consumed || other.GetComponent<NearMissSensor>()!=null) return;
        AirController air=other.GetComponentInParent<AirController>();
        if(air==null || GameManager.Instance!=null && GameManager.Instance.IsGameOver) return;
        // A protected continue must neither take damage nor waste this contact.
        if(heavyImpact && air.IsDamageProtected) return;
        consumed=true;
        if(IsPickup)
        {
            air.AddAir(14);air.GetComponent<ScoreController>()?.AddBonus(100);
            air.GetComponent<ComboController>()?.RegisterSuccess(1,60);
            GameAudioController.PlayPickup();gameObject.SetActive(false);
        }
        else
        {
            air.GetComponent<ComboController>()?.BreakCombo();
            air.GetComponentInChildren<NearMissSensor>()?.RegisterHit(this);
            GameAudioController.PlayHit();
            if(heavyImpact)
            {
                air.RemoveAir(air.CurrentAir * .8f);
                air.GetComponent<PlayerRunner>()?.ApplyKnockback(6f,.3f);
                CameraShakeController.ShakeGlobal(.22f,.12f);
                // One hit per pass prevents stacked damage from the hammer's
                // multiple mesh proxies and the return through it after recoil.
                // The mechanism and its animation remain in the world.
                return;
            }
            air.RemoveAir(8);air.GetComponent<PlayerRunner>()?.ApplySlow(.75f,.25f);
            CameraShakeController.ShakeGlobal(.16f,.085f);
            // Hide after contact, including protected continues, to avoid repeat hits.
            gameObject.SetActive(false);
        }
    }
}
