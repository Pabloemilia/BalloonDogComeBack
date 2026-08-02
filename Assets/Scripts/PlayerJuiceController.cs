using UnityEngine;

/// <summary>
/// Oyuncu modeline sağ-sol yatış, hafif koşu sekmesi ve helikopter titreşimi verir.
/// Fizik gövdesini değil yalnızca görsel modeli hareket ettirir.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerJuiceController : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField, Range(0f, 25f)] private float maximumLeanAngle = 13f;
    [SerializeField, Min(0f)] private float leanSmoothness = 9f;
    [SerializeField, Min(0f)] private float runBobAmount = 0.055f;
    [SerializeField, Min(0f)] private float runBobSpeed = 11f;
    [SerializeField, Min(0f)] private float helicopterWobble = 2.2f;

    private Rigidbody body;
    private PlayerFormController formController;
    private BalloonSizeController sizeController;
    private CapsuleCollider playerCollider;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    public void Configure(Transform playerVisual)
    {
        visual = playerVisual;
        CaptureBasePose();
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        formController = GetComponent<PlayerFormController>();
        sizeController = GetComponent<BalloonSizeController>();
        playerCollider = GetComponent<CapsuleCollider>();
        CaptureBasePose();
    }

    private void CaptureBasePose()
    {
        if (visual == null)
        {
            return;
        }

        baseLocalPosition = visual.localPosition;
        baseLocalRotation = visual.localRotation;
    }

    private void LateUpdate()
    {
        if (visual == null || body == null)
        {
            return;
        }

        float horizontal = Mathf.Clamp(body.linearVelocity.x / 8f, -1f, 1f);
        float lean = -horizontal * maximumLeanAngle;
        float wobble = formController != null && formController.IsHelicopterActive
            ? Mathf.Sin(Time.time * 15f) * helicopterWobble
            : 0f;

        Quaternion targetRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, lean + wobble);
        float blend = 1f - Mathf.Exp(-leanSmoothness * Time.deltaTime);
        visual.localRotation = Quaternion.Slerp(visual.localRotation, targetRotation, blend);

        float speedAmount = Mathf.Clamp01(
            new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude / 7f);

        float bob = Mathf.Sin(Time.time * runBobSpeed) *
                    runBobAmount *
                    speedAmount;

        visual.localPosition = baseLocalPosition + Vector3.up * bob;

        // BalloonSizeController boyutu Update'ta değiştiriyor; eski kod ise
        // LateUpdate'ta konumu tekrar eski değere çekerek küçük modeli zeminin
        // altına gömüyordu. Görselin gerçek renderer altını her kare zemine oturt.
        KeepVisualAboveGround();
    }

    private void KeepVisualAboveGround()
    {
        if (visual == null || playerCollider == null)
        {
            return;
        }

        Renderer[] renderers =
            visual.GetComponentsInChildren<Renderer>(true);

        bool hasBounds = false;
        Bounds combinedBounds = default;

        foreach (Renderer visualRenderer in renderers)
        {
            if (visualRenderer == null ||
                !visualRenderer.enabled ||
                visualRenderer is TrailRenderer)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = visualRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(visualRenderer.bounds);
            }
        }

        if (!hasBounds)
        {
            return;
        }

        // Hem büyük hem küçük formda zemine basması için oyuncu collider'ının
        // gerçek alt noktasını referans al.
        float desiredBottomY = playerCollider.bounds.min.y + 0.03f;
        float deltaWorldY = desiredBottomY - combinedBounds.min.y;

        if (Mathf.Abs(deltaWorldY) <= 0.0005f)
        {
            return;
        }

        Vector3 localDelta =
            transform.InverseTransformVector(
                new Vector3(0f, deltaWorldY, 0f));

        visual.localPosition +=
            new Vector3(0f, localDelta.y, 0f);
    }
}
