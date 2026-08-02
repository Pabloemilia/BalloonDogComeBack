using UnityEngine;

/// <summary>
/// Engeli başlangıç konumundan taşır, fakat yol sınırlarını asla aşmaz.
/// Hareket sinüs tabanlı olduğu için kare hızından bağımsız ve yumuşaktır.
/// </summary>
public sealed class MovingObstacle : MonoBehaviour
{
    public enum MotionMode
    {
        Horizontal,
        Vertical,
        ForwardBackward
    }

    [SerializeField] private MotionMode motionMode = MotionMode.Horizontal;
    [SerializeField, Min(0f)] private float distance = 1.4f;
    [SerializeField, Min(0.05f)] private float cyclesPerSecond = 0.35f;
    [SerializeField] private float phaseOffset;
    [SerializeField, Min(0f)] private float roadHalfWidth = 4.1f;

    private Vector3 startLocalPosition;

    public void Configure(MotionMode mode, float moveDistance, float frequency, float phase, float halfWidth)
    {
        motionMode = mode;
        distance = Mathf.Max(0f, moveDistance);
        cyclesPerSecond = Mathf.Max(0.05f, frequency);
        phaseOffset = phase;
        roadHalfWidth = Mathf.Max(0f, halfWidth);
        startLocalPosition = transform.localPosition;
    }

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        startLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        float wave = Mathf.Sin((Time.time * cyclesPerSecond + phaseOffset) * Mathf.PI * 2f);
        Vector3 position = startLocalPosition;

        switch (motionMode)
        {
            case MotionMode.Horizontal:
                position.x = Mathf.Clamp(
                    startLocalPosition.x + wave * distance,
                    -roadHalfWidth,
                    roadHalfWidth);
                break;

            case MotionMode.Vertical:
                position.y = Mathf.Max(0.1f, startLocalPosition.y + (wave + 1f) * 0.5f * distance);
                break;

            case MotionMode.ForwardBackward:
                position.z = startLocalPosition.z + wave * distance;
                break;
        }

        transform.localPosition = position;
    }
}
