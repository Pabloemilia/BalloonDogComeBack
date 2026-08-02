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

        float speedAmount = Mathf.Clamp01(new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude / 7f);
        float bob = Mathf.Sin(Time.time * runBobSpeed) * runBobAmount * speedAmount;
        visual.localPosition = baseLocalPosition + Vector3.up * bob;
    }
}
