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
    [SerializeField, Min(0.1f)] private float resizeSpeed = 18f;

    [Header("One Tap Shrink")]
    [SerializeField, Range(0.1f, 1f)] private float forcedSmallMultiplier = 0.66f;
    [SerializeField, Min(0.1f)] private float smallStateDuration = 1.2f;
    [SerializeField, Min(0f)] private float cooldownAfterSmall = 1.6f;
    [SerializeField, Range(0f, 1f)] private float airLossFractionPerShrink = 0.2f;

    private CapsuleCollider playerCollider;
    private Vector3 originalVisualScale = Vector3.one;
    private Vector3 originalVisualLocalPosition;
    private Transform capturedVisual;
    private float originalColliderRadius;
    private float originalColliderHeight;
    private float currentSizeMultiplier;
    private bool forcedSmall;
    private float forcedSmallEndsAt;
    private float nextShrinkAllowedAt;

    public bool IsSmall => currentSizeMultiplier <= Mathf.Lerp(
        minimumSizeMultiplier,
        maximumSizeMultiplier,
        0.35f);

    public float CurrentSizeMultiplier => currentSizeMultiplier;
    public float MaximumSizeMultiplier => maximumSizeMultiplier;
    public bool IsShrinkRequested => forcedSmall;
    public bool IsForcedSmall => forcedSmall;
    public bool IsShrinkReady =>
        !forcedSmall && Time.unscaledTime >= nextShrinkAllowedAt;

    public float ShrinkCooldownRemaining =>
        Mathf.Max(0f, nextShrinkAllowedAt - Time.unscaledTime);

    private void Awake()
    {
        playerCollider = GetComponent<CapsuleCollider>();
        airController ??= GetComponent<AirController>();
        formController ??= GetComponent<PlayerFormController>();

        originalColliderRadius = playerCollider.radius;
        originalColliderHeight = playerCollider.height;

        if (balloonVisual == null)
        {
            balloonVisual = transform.Find("BalloonDogModel") ??
                            transform.Find("Bubble");
        }

        CaptureVisualScale();

        forcedSmallMultiplier = Mathf.Clamp(
            forcedSmallMultiplier,
            minimumSizeMultiplier,
            maximumSizeMultiplier);

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

    /// <summary>
    /// Tek basışta küçülür. Küçükken veya bekleme süresindeyken yeniden basmak
    /// daha fazla küçültmez. Süre bitince otomatik olarak normal hava boyuna döner.
    /// </summary>
    public bool ToggleShrinkState()
    {
        return TryActivateShrink();
    }

    public bool TryActivateShrink()
    {
        if (!CanChangeShrinkState() || !IsShrinkReady)
        {
            return false;
        }

        float now = Time.unscaledTime;
        forcedSmall = true;
        forcedSmallEndsAt = now + smallStateDuration;
        nextShrinkAllowedAt = forcedSmallEndsAt + cooldownAfterSmall;

        ApplyImmediateStepTowardTarget();
        SpendShrinkAir();
        PlayShrinkFeedback();
        return true;
    }

    /// <summary>
    /// Eski basılı-tutma butonuyla uyumludur. PointerUp tarafından gönderilen
    /// false isteği yok sayılır; böylece parmağı kaldırınca anında büyümez.
    /// </summary>
    public void SetShrinkRequested(bool requested)
    {
        if (requested)
        {
            TryActivateShrink();
        }
    }

    public void CancelShrinkImmediately()
    {
        forcedSmall = false;
        forcedSmallEndsAt = 0f;
        nextShrinkAllowedAt = 0f;
        currentSizeMultiplier = GetAirDrivenTargetSize();
        UpdateBalloonSize();
    }

    public void SnapToCurrentAir()
    {
        CancelShrinkImmediately();
    }

    private void Update()
    {
        if (forcedSmall && Time.unscaledTime >= forcedSmallEndsAt)
        {
            forcedSmall = false;
            PlayGrowFeedback();
        }

        float targetSize = forcedSmall
            ? Mathf.Clamp(
                forcedSmallMultiplier,
                minimumSizeMultiplier,
                maximumSizeMultiplier)
            : GetAirDrivenTargetSize();

        float deltaTime = Time.timeScale > 0f
            ? Time.deltaTime
            : Time.unscaledDeltaTime;

        currentSizeMultiplier = Mathf.MoveTowards(
            currentSizeMultiplier,
            targetSize,
            resizeSpeed * deltaTime);

        UpdateBalloonSize();
    }

    private bool CanChangeShrinkState()
    {
        return (formController == null || !formController.IsHelicopterActive) &&
               (GameManager.Instance == null || !GameManager.Instance.IsGameOver);
    }

    private void ApplyImmediateStepTowardTarget()
    {
        float target = Mathf.Clamp(
            forcedSmallMultiplier,
            minimumSizeMultiplier,
            maximumSizeMultiplier);

        currentSizeMultiplier = Mathf.MoveTowards(
            currentSizeMultiplier,
            target,
            0.26f);

        UpdateBalloonSize();
    }

    private void SpendShrinkAir()
    {
        if (airController == null || airLossFractionPerShrink <= 0f)
        {
            return;
        }

        float airToLose = airController.MaxAir * airLossFractionPerShrink;
        airController.RemoveAir(airToLose);
    }

    private void PlayShrinkFeedback()
    {
        CameraShakeController.ShakeGlobal(0.055f, 0.016f);

        RuntimeVfx.SpawnBurst(
            transform.position + Vector3.up * 0.9f,
            new Color(0.2f, 0.9f, 1f, 1f),
            11,
            2.2f,
            0.08f,
            0.34f);
    }

    private void PlayGrowFeedback()
    {
        RuntimeVfx.SpawnBurst(
            transform.position + Vector3.up * 0.9f,
            new Color(1f, 0.72f, 0.18f, 1f),
            8,
            1.8f,
            0.07f,
            0.25f);
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
        if (balloonVisual == null || capturedVisual == balloonVisual)
        {
            return;
        }

        capturedVisual = balloonVisual;
        originalVisualScale = balloonVisual.localScale;
        originalVisualLocalPosition = balloonVisual.localPosition;
    }

    private void UpdateBalloonSize()
    {
        if (balloonVisual != null)
        {
            // Çok küçük ölçekte modelin pivotu zeminin altında kalıyordu.
            // Görseli küçültürken yerel Y konumunu da orantılı biçimde yukarı al.
            float visibleMultiplier = Mathf.Max(currentSizeMultiplier, 0.66f);
            float positionRatio = maximumSizeMultiplier > 0.0001f
                ? visibleMultiplier / maximumSizeMultiplier
                : 1f;

            balloonVisual.localScale =
                originalVisualScale * visibleMultiplier;

            balloonVisual.localPosition = new Vector3(
                originalVisualLocalPosition.x,
                originalVisualLocalPosition.y * positionRatio,
                originalVisualLocalPosition.z);

            if (!balloonVisual.gameObject.activeSelf)
            {
                balloonVisual.gameObject.SetActive(true);
            }
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
        forcedSmall = false;
        forcedSmallEndsAt = 0f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minimumSizeMultiplier = Mathf.Max(0.1f, minimumSizeMultiplier);
        maximumSizeMultiplier = Mathf.Max(
            minimumSizeMultiplier,
            maximumSizeMultiplier);

        forcedSmallMultiplier = Mathf.Clamp(
            forcedSmallMultiplier,
            minimumSizeMultiplier,
            maximumSizeMultiplier);

        resizeSpeed = Mathf.Max(0.1f, resizeSpeed);
        smallStateDuration = Mathf.Max(0.1f, smallStateDuration);
        cooldownAfterSmall = Mathf.Max(0f, cooldownAfterSmall);
        airLossFractionPerShrink = Mathf.Clamp01(airLossFractionPerShrink);
    }
#endif
}
