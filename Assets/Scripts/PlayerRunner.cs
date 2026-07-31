using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerRunner : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float forwardSpeed = 6f;

    [SerializeField]
    private BalloonSizeController balloonSizeController;

    [SerializeField, Min(1f)]
    private float smallBalloonSpeedMultiplier = 1.4f;

    private Rigidbody playerRigidbody;
    private Coroutine slowCoroutine;
    private float currentForwardSpeed;

    public float CurrentForwardSpeed => currentForwardSpeed;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        currentForwardSpeed = forwardSpeed;
    }

    private void FixedUpdate()
    {
        float speed = currentForwardSpeed;

        if (balloonSizeController != null &&
            balloonSizeController.IsSmall)
        {
            speed *= smallBalloonSpeedMultiplier;
        }

        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.z = speed;

        playerRigidbody.linearVelocity = velocity;
    }

    public void ApplySlow(
        float speedMultiplier,
        float duration
    )
    {
        speedMultiplier = Mathf.Clamp01(speedMultiplier);
        duration = Mathf.Max(0f, duration);

        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }

        slowCoroutine = StartCoroutine(
            SlowRoutine(speedMultiplier, duration)
        );
    }

    private IEnumerator SlowRoutine(
        float speedMultiplier,
        float duration
    )
    {
        currentForwardSpeed =
            forwardSpeed * speedMultiplier;

        yield return new WaitForSeconds(duration);

        currentForwardSpeed = forwardSpeed;
        slowCoroutine = null;
    }

    private void OnDisable()
    {
        currentForwardSpeed = forwardSpeed;
    }
}