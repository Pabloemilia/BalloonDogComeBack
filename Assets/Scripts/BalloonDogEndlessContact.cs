using UnityEngine;

/// <summary>One reusable contact owner. Never destroys itself or its road chunk.</summary>
public sealed class BalloonDogEndlessContact : MonoBehaviour
{
    public bool IsPickup;
    private bool consumed;
    private bool heavyImpact;
    private bool lethalImpact;
    public void ConfigureHeavyImpact() => heavyImpact = true;
    public void ConfigureObstacle(int artId)
    {
        heavyImpact = false;
        lethalImpact = false;
        // Only the currently enabled catalog IDs are classified here.
        // Unused traps 3, 9, 12 and 23 are not added to the course.
        switch(artId)
        {
            case 1: case 2: case 5: case 6: case 7: case 10: case 11:
            case 13: case 14: case 16: case 17: case 18: case 19: case 20: case 21:
                lethalImpact = true;
                break;
            case 4: case 8: case 22: case 25:
                heavyImpact = true;
                break;
        }
    }
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
        if(!IsPickup && air.IsDamageProtected) return;
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
            if(lethalImpact)
            {
                air.EmptyAir("BALON PATLADI");
                return;
            }
            if(heavyImpact)
            {
                air.RemoveAir(air.CurrentAir * .8f);
                air.GetComponent<PlayerRunner>()?.ApplyKnockbackDistance(2f,.3f);
                CameraShakeController.ShakeGlobal(.22f,.12f);
                // One hit per pass prevents stacked damage from the hammer's
                // multiple mesh proxies and the return through it after recoil.
                // The mechanism and its animation remain in the world.
                return;
            }
            air.RemoveAir(8);air.GetComponent<PlayerRunner>()?.ApplySlow(.75f,.25f);
            CameraShakeController.ShakeGlobal(.16f,.085f);
            // Even unclassified mechanisms (such as the flame nozzle's solid
            // body) stay visible and animated. Only pickups disappear on contact.
        }
    }
}
