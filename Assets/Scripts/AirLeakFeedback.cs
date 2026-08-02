using UnityEngine;

[RequireComponent(typeof(AirController))]
public sealed class AirLeakFeedback : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField, Min(0f)] private float minimumVisibleLoss = 0.25f;

    private AirController airController;
    private float previousAir;
    private bool initialized;

    public void Configure(Transform playerVisual)
    {
        visual = playerVisual;
    }

    private void Awake()
    {
        airController = GetComponent<AirController>();
    }

    private void OnEnable()
    {
        airController ??= GetComponent<AirController>();
        airController.AirChanged += HandleAirChanged;
    }

    private void Start()
    {
        previousAir = airController.CurrentAir;
        initialized = true;
    }

    private void OnDisable()
    {
        if (airController != null)
        {
            airController.AirChanged -= HandleAirChanged;
        }
    }

    private void HandleAirChanged(float current, float maximum)
    {
        if (!initialized)
        {
            previousAir = current;
            initialized = true;
            return;
        }

        float loss = previousAir - current;
        previousAir = current;

        if (loss < minimumVisibleLoss)
        {
            return;
        }

        Vector3 position = visual != null
            ? visual.position + Vector3.up * 0.5f - transform.forward * 0.35f
            : transform.position + Vector3.up * 0.5f;

        RuntimeVfx.SpawnBurst(
            position,
            new Color(0.55f, 0.9f, 1f, 1f),
            Mathf.Clamp(Mathf.RoundToInt(loss), 6, 24),
            1.8f,
            0.11f,
            0.45f);
    }
}
