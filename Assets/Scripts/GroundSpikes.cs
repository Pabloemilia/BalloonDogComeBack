using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class GroundSpikes : MonoBehaviour
{
    private bool triggered;

    private void Reset()
    {
        Collider spikeCollider = GetComponent<Collider>();
        spikeCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || other == null)
        {
            return;
        }

        if (other.GetComponent<NearMissSensor>() != null)
        {
            return;
        }

        AirController airController = other.GetComponentInParent<AirController>();
        if (airController == null)
        {
            return;
        }

        PlayerFormController formController =
            other.GetComponentInParent<PlayerFormController>();

        if (formController != null && formController.IsHelicopterActive)
        {
            return;
        }

        triggered = true;
        airController.GetComponent<ComboController>()?.BreakCombo();
        airController.GetComponentInChildren<NearMissSensor>(true)?.RegisterHit(this);
        GameAudioController.PlayHit();
        airController.EmptyAir("BALON DİKENLERE ÇARPTI");
    }
}
