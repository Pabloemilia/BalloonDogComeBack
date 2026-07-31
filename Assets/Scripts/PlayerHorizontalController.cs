using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerHorizontalController : MonoBehaviour
{
    [Header("Road Limits")]
    [SerializeField, Min(0f)] private float horizontalLimit = 2.1f;

    [Header("Drag Settings")]
    [SerializeField, Min(0.1f)] private float dragSensitivity = 7f;
    [SerializeField, Min(0.1f)] private float maxHorizontalSpeed = 10f;
    [SerializeField, Min(0.1f)] private float horizontalAcceleration = 50f;

    private Rigidbody playerRigidbody;
    private float targetX;
    private bool wasPointerPressed;
    private Vector2 previousPointerPosition;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        targetX = playerRigidbody.position.x;
    }

    private void Update()
    {
        if (!TryGetPointerPosition(out Vector2 pointerPosition))
        {
            wasPointerPressed = false;
            return;
        }

        if (!wasPointerPressed)
        {
            previousPointerPosition = pointerPosition;
            wasPointerPressed = true;
            return;
        }

        float screenDeltaX = pointerPosition.x - previousPointerPosition.x;
        previousPointerPosition = pointerPosition;

        float normalizedDeltaX = screenDeltaX / Mathf.Max(Screen.width, 1);
        targetX += normalizedDeltaX * dragSensitivity;
        targetX = Mathf.Clamp(targetX, -horizontalLimit, horizontalLimit);
    }

    private void FixedUpdate()
    {
        float distanceToTarget = targetX - playerRigidbody.position.x;
        float targetHorizontalSpeed = Mathf.Clamp(
            distanceToTarget / Time.fixedDeltaTime,
            -maxHorizontalSpeed,
            maxHorizontalSpeed);

        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.x = Mathf.MoveTowards(
            velocity.x,
            targetHorizontalSpeed,
            horizontalAcceleration * Time.fixedDeltaTime);

        playerRigidbody.linearVelocity = velocity;
    }

    private bool TryGetPointerPosition(out Vector2 pointerPosition)
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId))
            {
                pointerPosition = default;
                return false;
            }

            pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                pointerPosition = default;
                return false;
            }

            pointerPosition = Mouse.current.position.ReadValue();
            return true;
        }

        pointerPosition = default;
        return false;
    }
}
