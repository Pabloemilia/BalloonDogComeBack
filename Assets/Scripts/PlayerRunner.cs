using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerRunner : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float forwardSpeed = 6f;

    private Rigidbody playerRigidbody;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Vector3 currentVelocity = playerRigidbody.linearVelocity;
        currentVelocity.z = forwardSpeed;

        playerRigidbody.linearVelocity = currentVelocity;
    }
}