using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerRunner : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float forwardSpeed = 6f;
    [SerializeField] private BalloonSizeController balloonSizeController;
    [SerializeField, Min(1f)] private float smallBalloonSpeedMultiplier = 1.12f;

    private Rigidbody playerRigidbody;
    private Coroutine slowCoroutine;
    private float slowMultiplier = 1f;
    private bool movementEnabled = true;

    public float CurrentForwardSpeed => movementEnabled
        ? forwardSpeed * slowMultiplier
        : 0f;

    public bool MovementEnabled => movementEnabled;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();

        if (balloonSizeController == null)
        {
            balloonSizeController = GetComponent<BalloonSizeController>();
        }
    }

    private void FixedUpdate()
    {
        Vector3 velocity = playerRigidbody.linearVelocity;

        if (!movementEnabled)
        {
            velocity.x = 0f;
            velocity.z = 0f;
            playerRigidbody.linearVelocity = velocity;
            return;
        }

        float speed = CurrentForwardSpeed;

        if (balloonSizeController != null && balloonSizeController.IsSmall)
        {
            speed *= smallBalloonSpeedMultiplier;
        }

        velocity.z = speed;
        playerRigidbody.linearVelocity = velocity;
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;

        if (!enabled && playerRigidbody != null)
        {
            Vector3 velocity = playerRigidbody.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            playerRigidbody.linearVelocity = velocity;
        }
    }

    public void ApplySlow(float speedMultiplier, float duration)
    {
        if (!movementEnabled)
        {
            return;
        }

        speedMultiplier = Mathf.Clamp(speedMultiplier, 0.05f, 1f);
        duration = Mathf.Max(0f, duration);

        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }

        slowCoroutine = StartCoroutine(SlowRoutine(speedMultiplier, duration));
    }

    private IEnumerator SlowRoutine(float speedMultiplier, float duration)
    {
        slowMultiplier = speedMultiplier;
        yield return new WaitForSeconds(duration);
        slowMultiplier = 1f;
        slowCoroutine = null;
    }

    private void OnDisable()
    {
        slowMultiplier = 1f;
        movementEnabled = true;
    }
}
