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

        // Air loss no longer emits particles behind the player.

    }
}
