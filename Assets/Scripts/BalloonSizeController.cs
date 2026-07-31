using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CapsuleCollider))]
public sealed class BalloonSizeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform balloonVisual;

    [Header("Size Settings")]
    [SerializeField, Min(0.1f)]
    private float smallSize = 0.6f;

    [SerializeField, Min(0.1f)]
    private float largeSize = 1.3f;

    [SerializeField, Min(0.1f)]
    private float resizeSpeed = 4f;

    private CapsuleCollider playerCollider;

    private Vector3 originalVisualScale;
    private float originalColliderRadius;
    private float originalColliderHeight;

    private float currentSize = 1f;

    public bool IsSmall => currentSize <= smallSize + 0.001f;

    private void Awake()
    {
        playerCollider = GetComponent<CapsuleCollider>();

        if (balloonVisual == null)
        {
            Debug.LogError(
                "Balloon visual atanmadı.",
                this
            );

            enabled = false;
            return;
        }

        originalVisualScale = balloonVisual.localScale;
        originalColliderRadius = playerCollider.radius;
        originalColliderHeight = playerCollider.height;
    }

    private void Update()
    {
        bool isPressed = IsScreenPressed();

        float targetSize = isPressed
            ? smallSize
            : largeSize;

        currentSize = Mathf.MoveTowards(
            currentSize,
            targetSize,
            resizeSpeed * Time.deltaTime
        );

        UpdateBalloonSize();
    }

    private bool IsScreenPressed()
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            return true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.isPressed)
        {
            return true;
        }

        return false;
    }

    private void UpdateBalloonSize()
    {
        balloonVisual.localScale =
            originalVisualScale * currentSize;

        playerCollider.radius =
            originalColliderRadius * currentSize;

        playerCollider.height = Mathf.Max(
            playerCollider.radius * 2f,
            originalColliderHeight * currentSize
        );
    }
}