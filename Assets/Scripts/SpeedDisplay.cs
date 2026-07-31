using TMPro;
using UnityEngine;

public sealed class SpeedDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private TMP_Text speedText;

    [Header("Display Settings")]
    [SerializeField] private bool showTotalSpeed;
    [SerializeField] private bool useKilometersPerHour = true;
    [SerializeField, Min(0f)] private float smoothing = 8f;

    private float displayedSpeed;

    public void Configure(Rigidbody body, TMP_Text text)
    {
        playerRigidbody = body;
        speedText = text;
    }

    private void Update()
    {
        if (playerRigidbody == null || speedText == null)
        {
            return;
        }

        Vector3 velocity = playerRigidbody.linearVelocity;
        float currentSpeed = showTotalSpeed
            ? velocity.magnitude
            : Mathf.Abs(velocity.z);

        if (useKilometersPerHour)
        {
            currentSpeed *= 3.6f;
        }

        float smoothAmount = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        displayedSpeed = Mathf.Lerp(displayedSpeed, currentSpeed, smoothAmount);

        speedText.text = useKilometersPerHour
            ? $"{displayedSpeed:0} km/h"
            : $"{displayedSpeed:0.0} m/s";
    }
}
