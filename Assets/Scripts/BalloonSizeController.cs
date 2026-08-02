using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(AirController))]
public sealed class BalloonSizeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform balloonVisual;
    [SerializeField] private PlayerFormController formController;
    [SerializeField] private AirController airController;

    [Header("Air Driven Size")]
    [SerializeField, Min(0.1f)] private float minimumSizeMultiplier = 0.55f;
    [SerializeField, Min(0.1f)] private float maximumSizeMultiplier = 1.2f;
    [SerializeField, Min(0.1f)] private float resizeSpeed = 3.8f;

    [Header("Shrink Button")]
    [SerializeField, Min(0f)] private float shrinkAirDrainPerSecond = 24f;

    private CapsuleCollider playerCollider;
    private Vector3 originalVisualScale = Vector3.one;
    private float originalColliderRadius;
    private float originalColliderHeight;
    private float currentSizeMultiplier;
    private bool shrinkRequested;

    public bool IsSmall => currentSizeMultiplier <= Mathf.Lerp(
        minimumSizeMultiplier,
        maximumSizeMultiplier,
        0.35f);

    public float CurrentSizeMultiplier => currentSizeMultiplier;
    public bool IsShrinkRequested => shrinkRequested;

    private void Awake()
    {
        playerCollider = GetComponent<CapsuleCollider>();
        airController ??= GetComponent<AirController>();
        formController ??= GetComponent<PlayerFormController>();

        originalColliderRadius = playerCollider.radius;
        originalColliderHeight = playerCollider.height;

        if (balloonVisual == null)
        {
            balloonVisual = transform.Find("BalloonDogModel") ?? transform.Find("Bubble");
        }

        CaptureVisualScale();

        // Menü Time.timeScale = 0 iken küçük görünmemesi için ilk karede tam boy başlar.
        currentSizeMultiplier = maximumSizeMultiplier;
        UpdateBalloonSize();
    }

    private void Start()
    {
        SnapToCurrentAir();
    }

    public void Configure(
        Transform visual,
        PlayerFormController playerForm,
        AirController controller)
    {
        balloonVisual = visual;
        formController = playerForm;
        airController = controller;
        CaptureVisualScale();
        SnapToCurrentAir();
    }

    public void Configure(Transform visual, PlayerFormController playerForm)
    {
        Configure(visual, playerForm, GetComponent<AirController>());
    }

    public void SetShrinkRequested(bool requested)
    {
        shrinkRequested = requested;
    }

    public void SnapToCurrentAir()
    {
        currentSizeMultiplier = GetAirDrivenTargetSize();
        UpdateBalloonSize();
    }

    private void Update()
    {
        bool canShrink =
            shrinkRequested &&
            airController != null &&
            !airController.IsEmpty &&
            (formController == null || !formController.IsHelicopterActive) &&
            (GameManager.Instance == null || !GameManager.Instance.IsGameOver);

        if (canShrink)
        {
            airController.RemoveAir(shrinkAirDrainPerSecond * Time.deltaTime);
        }

        float targetSize = GetAirDrivenTargetSize();
        float deltaTime = Time.timeScale > 0f
            ? Time.deltaTime
            : Time.unscaledDeltaTime;

        currentSizeMultiplier = Mathf.MoveTowards(
            currentSizeMultiplier,
            targetSize,
            resizeSpeed * deltaTime);

        UpdateBalloonSize();
    }

    private float GetAirDrivenTargetSize()
    {
        float normalizedAir = airController != null
            ? airController.NormalizedAir
            : 1f;

        return Mathf.Lerp(
            minimumSizeMultiplier,
            maximumSizeMultiplier,
            normalizedAir);
    }

    private void CaptureVisualScale()
    {
        if (balloonVisual != null)
        {
            originalVisualScale = balloonVisual.localScale;
        }
    }

    private void UpdateBalloonSize()
    {
        if (balloonVisual != null)
        {
            balloonVisual.localScale = originalVisualScale * currentSizeMultiplier;
        }

        if (playerCollider == null)
        {
            return;
        }

        playerCollider.radius = Mathf.Max(
            0.12f,
            originalColliderRadius * currentSizeMultiplier);

        playerCollider.height = Mathf.Max(
            playerCollider.radius * 2f,
            originalColliderHeight * currentSizeMultiplier);
    }

    private void OnDisable()
    {
        shrinkRequested = false;
    }
}
