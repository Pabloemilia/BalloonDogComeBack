using UnityEngine;

public sealed class RunnerCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField]
    private Transform target;

    [Header("Camera Settings")]
    [SerializeField]
    private Vector3 offset = new Vector3(0f, 4f, -7f);

    [SerializeField, Range(0f, 1f)]
    private float horizontalFollowAmount = 0.15f;

    [SerializeField, Min(0f)]
    private float followSmoothness = 8f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + offset;

        // Kamera, oyuncunun sağ-sol hareketini yalnızca
        // küçük miktarda takip eder.
        targetPosition.x =
            offset.x + target.position.x * horizontalFollowAmount;

        float smoothFactor =
            1f - Mathf.Exp(-followSmoothness * Time.deltaTime);

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothFactor
        );
    }
}