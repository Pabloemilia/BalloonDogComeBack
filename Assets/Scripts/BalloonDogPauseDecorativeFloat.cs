using UnityEngine;

[DisallowMultipleComponent]
public sealed class BalloonDogPauseDecorativeFloat : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField, Min(0f)] private float horizontalAmplitude = 8f;
    [SerializeField, Min(0f)] private float verticalAmplitude = 12f;
    [SerializeField, Min(4f)] private float duration = 12f;
    [SerializeField, Range(0f, 1f)] private float normalizedPhase;
    [SerializeField, Range(0f, 2f)] private float rotationAmplitude = 1f;

    private Vector2 basePosition;
    private Quaternion baseRotation;
    private float elapsed;
    private bool initialized;

    public void Configure(
        float horizontal,
        float vertical,
        float cycleDuration,
        float phase,
        float rotation)
    {
        horizontalAmplitude = Mathf.Max(0f, horizontal);
        verticalAmplitude = Mathf.Max(0f, vertical);
        duration = Mathf.Max(4f, cycleDuration);
        normalizedPhase = Mathf.Repeat(phase, 1f);
        rotationAmplitude = Mathf.Clamp(rotation, 0f, 2f);
        Initialize();
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        elapsed = normalizedPhase * duration;
    }

    private void LateUpdate()
    {
        if (!initialized || target == null)
        {
            return;
        }

        elapsed = Mathf.Repeat(
            elapsed + Time.unscaledDeltaTime,
            duration);

        float angle = elapsed / duration * Mathf.PI * 2f;
        float horizontal = Mathf.Sin(angle) * horizontalAmplitude;
        float vertical =
            Mathf.Sin(angle + Mathf.PI * 0.5f) * verticalAmplitude;
        float rotation =
            Mathf.Sin(angle + Mathf.PI * 0.25f) * rotationAmplitude;

        target.anchoredPosition =
            basePosition + new Vector2(horizontal, vertical);
        target.localRotation =
            baseRotation * Quaternion.Euler(0f, 0f, rotation);
    }

    private void OnDisable()
    {
        if (!initialized || target == null)
        {
            return;
        }

        target.anchoredPosition = basePosition;
        target.localRotation = baseRotation;
    }

    private void Initialize()
    {
        target ??= transform as RectTransform;
        if (target == null)
        {
            return;
        }

        basePosition = target.anchoredPosition;
        baseRotation = target.localRotation;
        elapsed = normalizedPhase * duration;
        initialized = true;
    }
}
