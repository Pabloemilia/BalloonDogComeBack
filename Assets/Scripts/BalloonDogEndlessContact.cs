using UnityEngine;

/// <summary>One reusable contact owner. Never destroys itself or its road chunk.</summary>
public sealed class BalloonDogEndlessContact : MonoBehaviour
{
    public bool IsPickup;
    private bool consumed;
    public void ResetContact(bool active) { consumed=false;gameObject.SetActive(active); }
    private void OnTriggerEnter(Collider other)
    {
        if(consumed || other.GetComponent<NearMissSensor>()!=null) return;
        AirController air=other.GetComponentInParent<AirController>();
        if(air==null || GameManager.Instance!=null && GameManager.Instance.IsGameOver) return;
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
            air.RemoveAir(8);air.GetComponent<PlayerRunner>()?.ApplySlow(.75f,.25f);
            CameraShakeController.ShakeGlobal(.16f,.085f);
            // Hide after contact, including protected continues, to avoid repeat hits.
            gameObject.SetActive(false);
        }
    }
}
