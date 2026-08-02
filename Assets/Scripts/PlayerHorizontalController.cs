using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerHorizontalController : MonoBehaviour
{
    [Header("Road Limits")]
    [SerializeField, Min(0f)] private float horizontalLimit = 3.7f;

    [Header("Drag Settings")]
    [SerializeField, Min(0.1f)] private float dragSensitivity = 10f;
    [SerializeField, Min(0.1f)] private float maxHorizontalSpeed = 17f;
    [SerializeField, Min(0.1f)] private float horizontalAcceleration = 95f;

    [Header("Desktop Test")]
    [SerializeField, Min(0.1f)] private float keyboardSpeed = 8f;

    private Rigidbody playerRigidbody;
    private float targetX;
    private bool wasPointerPressed;
    private Vector2 previousPointerPosition;

    public void ConfigureRoadLimit(float limit)
    {
        horizontalLimit = Mathf.Max(0f, limit);
        targetX = Mathf.Clamp(targetX, -horizontalLimit, horizontalLimit);
    }

    public void ConfigureControls(
        float limit,
        float sensitivity,
        float maximumSpeed,
        float acceleration)
    {
        ConfigureRoadLimit(limit);
        dragSensitivity = Mathf.Max(0.1f, sensitivity);
        maxHorizontalSpeed = Mathf.Max(0.1f, maximumSpeed);
        horizontalAcceleration = Mathf.Max(0.1f, acceleration);
    }

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        targetX = playerRigidbody.position.x;
    }

    private void OnEnable()
    {
        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody>();
        }

        targetX = Mathf.Clamp(playerRigidbody.position.x, -horizontalLimit, horizontalLimit);
        wasPointerPressed = false;
    }

    private void Update()
    {
        HandleKeyboard();

        if (!TryGetPointerPosition(out Vector2 pointerPosition))
        {
            wasPointerPressed = false;
            return;
        }

        if (!wasPointerPressed)
        {
            previousPointerPosition = pointerPosition;
            targetX = Mathf.Clamp(playerRigidbody.position.x, -horizontalLimit, horizontalLimit);
            wasPointerPressed = true;
            return;
        }

        float screenDeltaX = pointerPosition.x - previousPointerPosition.x;
        previousPointerPosition = pointerPosition;

        float normalizedDeltaX = screenDeltaX / Mathf.Max(Screen.width, 1);
        targetX = Mathf.Clamp(
            targetX + normalizedDeltaX * dragSensitivity,
            -horizontalLimit,
            horizontalLimit);
    }

    private void FixedUpdate()
    {
        float distanceToTarget = targetX - playerRigidbody.position.x;
        float targetHorizontalSpeed = Mathf.Clamp(
            distanceToTarget / Mathf.Max(Time.fixedDeltaTime, 0.0001f),
            -maxHorizontalSpeed,
            maxHorizontalSpeed);

        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.x = Mathf.MoveTowards(
            velocity.x,
            targetHorizontalSpeed,
            horizontalAcceleration * Time.fixedDeltaTime);

        playerRigidbody.linearVelocity = velocity;
    }

    private void HandleKeyboard()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        float input = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            input -= 1f;
        }
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            input += 1f;
        }

        if (Mathf.Abs(input) > 0.01f)
        {
            targetX = Mathf.Clamp(
                targetX + input * keyboardSpeed * Time.unscaledDeltaTime,
                -horizontalLimit,
                horizontalLimit);
        }
    }

    private static bool TryGetPointerPosition(out Vector2 pointerPosition)
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            pointerPosition = Mouse.current.position.ReadValue();
            return true;
        }

        pointerPosition = default;
        return false;
    }

    private void OnDisable()
    {
        wasPointerPressed = false;
        if (playerRigidbody != null)
        {
            Vector3 velocity = playerRigidbody.linearVelocity;
            velocity.x = 0f;
            playerRigidbody.linearVelocity = velocity;
        }
    }
}
