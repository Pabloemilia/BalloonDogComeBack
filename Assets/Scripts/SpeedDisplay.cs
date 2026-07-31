using TMPro;
using UnityEngine;

public sealed class SpeedDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Rigidbody playerRigidbody;

    [SerializeField]
    private TMP_Text speedText;

    [Header("Display Settings")]
    [SerializeField]
    private bool showTotalSpeed;

    [SerializeField]
    private bool useKilometersPerHour = true;

    [SerializeField, Min(0f)]
    private float smoothing = 8f;

    private float displayedSpeed;

    private void Update()
    {
        if (playerRigidbody == null || speedText == null)
        {
            return;
        }

        Vector3 velocity = playerRigidbody.linearVelocity;

        // Açık olursa sağ-sol hareketi de hıza dahil eder.
        // Kapalı olursa sadece ileri yönün hızını gösterir.
        float currentSpeed = showTotalSpeed
            ? velocity.magnitude
            : Mathf.Abs(velocity.z);

        // 1 m/s = 3.6 km/h
        if (useKilometersPerHour)
        {
            currentSpeed *= 3.6f;
        }

        float smoothAmount =
            1f - Mathf.Exp(-smoothing * Time.deltaTime);

        displayedSpeed = Mathf.Lerp(
            displayedSpeed,
            currentSpeed,
            smoothAmount
        );

        if (useKilometersPerHour)
        {
            speedText.text = $"{displayedSpeed:0} km/h";
        }
        else
        {
            speedText.text = $"{displayedSpeed:0.0} m/s";
        }
    }
}