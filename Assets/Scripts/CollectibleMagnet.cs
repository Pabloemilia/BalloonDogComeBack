using UnityEngine;

[RequireComponent(typeof(AirPickup))]
public sealed class CollectibleMagnet : MonoBehaviour
{
    [SerializeField, Min(0f)] private float activationDistance = 1.6f;
    [SerializeField, Min(0f)] private float pullSpeed = 4f;

    private Transform player;
    private AirController airController;
    private AirPickup pickup;

    private void Awake()
    {
        pickup = GetComponent<AirPickup>();
    }

    private void Update()
    {
        if (player == null)
        {
            AirController found = FindAnyObjectByType<AirController>();
            if (found == null)
            {
                return;
            }

            airController = found;
            player = found.transform;
        }

        if (airController != null && airController.NormalizedAir >= 0.995f)
        {
            return;
        }

        Vector3 target = player.position + Vector3.up * 0.7f;
        float distance = Vector3.Distance(transform.position, target);
        if (distance > activationDistance)
        {
            return;
        }

        float strength = 1f - Mathf.Clamp01(distance / activationDistance);
        float moveDelta = pullSpeed * (0.35f + strength) * Time.deltaTime;
        if (pickup != null)
        {
            pickup.MoveAnimatedBaseTowards(target, moveDelta);
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveDelta);
        }
    }
}
