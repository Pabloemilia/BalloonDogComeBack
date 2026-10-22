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
    private bool wasKeyboardPressed;
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
        wasKeyboardPressed = false;
        StopHorizontalMotion();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            StopHorizontalMotion();
            wasPointerPressed = wasKeyboardPressed = false;
            return;
        }

        bool keyboardPressed = HandleKeyboard();

        if (!TryGetPointerPosition(out Vector2 pointerPosition))
        {
            if (!keyboardPressed && (wasPointerPressed || wasKeyboardPressed))
            {
                StopHorizontalMotion();
            }
            wasKeyboardPressed = keyboardPressed;
            wasPointerPressed = false;
            return;
        }

        wasKeyboardPressed = keyboardPressed;

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
        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.x = CalculateHorizontalSpeed(playerRigidbody.position.x, velocity.x, Time.fixedDeltaTime);
        playerRigidbody.linearVelocity = velocity;
    }

    private float CalculateHorizontalSpeed(float currentX, float currentSpeed, float step)
    {
        step = Mathf.Max(step, .0001f);
        targetX = Mathf.Clamp(targetX, -horizontalLimit, horizontalLimit);
        float distance = targetX - currentX;
        if (Mathf.Abs(distance) <= .001f) return 0f;

        float direction = Mathf.Sign(distance);
        // Direction changes cancel the old sideways momentum immediately.
        if (currentSpeed * direction < 0f) currentSpeed = 0f;
        float acceleration = Mathf.Max(.1f, horizontalAcceleration);
        float stoppingSpeed = Mathf.Sqrt(2f * acceleration * Mathf.Abs(distance));
        float desiredSpeed = direction * Mathf.Min(maxHorizontalSpeed, stoppingSpeed);
        float speed = Mathf.MoveTowards(currentSpeed, desiredSpeed, acceleration * step);
        // Never cross the target or road edge, even during a fast drag.
        float travel = Mathf.Min(Mathf.Max(0f, speed * direction) * step, Mathf.Abs(distance));
        float nextX = Mathf.Clamp(currentX + direction * travel, -horizontalLimit, horizontalLimit);
        return (nextX - currentX) / step;
    }

    private void StopHorizontalMotion()
    {
        if (playerRigidbody == null) return;
        targetX = Mathf.Clamp(playerRigidbody.position.x, -horizontalLimit, horizontalLimit);
        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.x = 0f;
        playerRigidbody.linearVelocity = velocity;
    }

    private bool HandleKeyboard()
    {
        if (Keyboard.current == null)
        {
            return false;
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
                targetX + input * keyboardSpeed * Time.deltaTime,
                -horizontalLimit,
                horizontalLimit);
            return true;
        }
        return false;
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
        wasPointerPressed = wasKeyboardPressed = false;
        StopHorizontalMotion();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            wasPointerPressed = wasKeyboardPressed = false;
            StopHorizontalMotion();
        }
    }
}
