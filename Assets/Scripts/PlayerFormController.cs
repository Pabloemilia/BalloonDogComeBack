using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AirController))]
public sealed class PlayerFormController : MonoBehaviour
{
    [Header("Double Tap")]
    [SerializeField, Min(0.1f)] private float doubleTapWindow = 0.32f;
    [SerializeField, Min(1f)] private float maximumTapDistance = 140f;

    [Header("Helicopter Settings")]
    [SerializeField, Min(1f)] private float flightHeight = 3.8f;
    [SerializeField, Min(0.1f)] private float verticalResponsiveness = 6f;
    [SerializeField, Min(0.1f)] private float maximumVerticalSpeed = 8f;
    [SerializeField, Min(0f)] private float airDrainPerSecond = 18f;
    [SerializeField] private Transform rotorVisual;

    private Rigidbody playerRigidbody;
    private AirController airController;
    private float lastTapTime = -10f;
    private Vector2 lastTapPosition;

    public bool IsHelicopterActive { get; private set; }

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        airController = GetComponent<AirController>();

        if (rotorVisual != null)
        {
            rotorVisual.gameObject.SetActive(false);
        }
    }

    public void ConfigureRotor(Transform rotor)
    {
        rotorVisual = rotor;

        if (rotorVisual != null)
        {
            rotorVisual.gameObject.SetActive(IsHelicopterActive);
        }
    }

    private void Update()
    {
        DetectDoubleTap();

        if (!IsHelicopterActive)
        {
            return;
        }

        bool gameRunning =
            GameManager.Instance == null ||
            !GameManager.Instance.IsGameOver;

        if (!gameRunning || airController.IsEmpty)
        {
            SetHelicopterActive(false);
            return;
        }

        airController.RemoveAir(airDrainPerSecond * Time.deltaTime);

        if (rotorVisual != null)
        {
            rotorVisual.Rotate(
                0f,
                720f * Time.deltaTime,
                0f,
                Space.Self);
        }
    }

    private void FixedUpdate()
    {
        if (!IsHelicopterActive)
        {
            return;
        }

        Vector3 velocity = playerRigidbody.linearVelocity;
        float heightDifference = flightHeight - playerRigidbody.position.y;

        velocity.y = Mathf.Clamp(
            heightDifference * verticalResponsiveness,
            -maximumVerticalSpeed,
            maximumVerticalSpeed);

        playerRigidbody.linearVelocity = velocity;
    }

    public void ToggleHelicopter()
    {
        bool gameRunning =
            GameManager.Instance == null ||
            !GameManager.Instance.IsGameOver;

        if (!gameRunning)
        {
            return;
        }

        if (!IsHelicopterActive && airController.IsEmpty)
        {
            return;
        }

        SetHelicopterActive(!IsHelicopterActive);
    }

    public void ForceBalloonForm()
    {
        SetHelicopterActive(false);
    }

    // Eski buton script'i projede kalsa bile derleme hatası vermesin.
    public void SetHelicopterRequested(bool requested)
    {
        if (requested != IsHelicopterActive)
        {
            SetHelicopterActive(requested);
        }
    }

    private void DetectDoubleTap()
    {
        if (!TryGetPointerDown(out Vector2 pointerPosition, out int pointerId))
        {
            return;
        }

        if (IsPointerOverInterface(pointerId))
        {
            lastTapTime = -10f;
            return;
        }

        float now = Time.unscaledTime;
        bool withinTime = now - lastTapTime <= doubleTapWindow;
        bool withinDistance = Vector2.Distance(
            pointerPosition,
            lastTapPosition) <= maximumTapDistance;

        if (withinTime && withinDistance)
        {
            lastTapTime = -10f;
            ToggleHelicopter();
            return;
        }

        lastTapTime = now;
        lastTapPosition = pointerPosition;
    }

    private static bool TryGetPointerDown(
        out Vector2 pointerPosition,
        out int pointerId)
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pointerPosition =
                Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId =
                Touchscreen.current.primaryTouch.touchId.ReadValue();
            return true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            pointerPosition = Mouse.current.position.ReadValue();
            pointerId = -1;
            return true;
        }

        pointerPosition = default;
        pointerId = -1;
        return false;
    }

    private static bool IsPointerOverInterface(int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return pointerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(pointerId)
            : EventSystem.current.IsPointerOverGameObject();
    }

    private void SetHelicopterActive(bool active)
    {
        if (IsHelicopterActive != active)
        {
            GameAudioController.PlayTransform();
            RuntimeVfx.SpawnBurst(
                transform.position + Vector3.up * 1.1f,
                active ? new Color(1f, 0.75f, 0.15f, 1f) : new Color(0.2f, 0.85f, 1f, 1f),
                14,
                2.6f,
                0.12f,
                0.5f);
        }
        IsHelicopterActive = active;
        playerRigidbody.useGravity = !active;

        if (!active)
        {
            Vector3 velocity = playerRigidbody.linearVelocity;
            velocity.y = Mathf.Min(velocity.y, 0f);
            playerRigidbody.linearVelocity = velocity;
        }

        if (rotorVisual != null)
        {
            rotorVisual.gameObject.SetActive(active);
        }
    }

    private void OnDisable()
    {
        IsHelicopterActive = false;

        if (playerRigidbody != null)
        {
            playerRigidbody.useGravity = true;
        }
    }
}
