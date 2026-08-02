using UnityEngine;

/// <summary>
/// Yol engellerini zemine paralel olarak döndürür. Böylece engeller oyuncunun
/// karşısında dikey takla atmak yerine yolu yatay biçimde süpürür.
/// </summary>
public sealed class RotatingObstacle : MonoBehaviour
{
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float degreesPerSecond = 70f;
    [SerializeField] private bool rotateInWorldSpace = true;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private bool randomizeStartingAngle = true;
    [SerializeField] private bool forceHorizontalRoadRotation = true;

    private Quaternion initialLocalRotation;
    private Quaternion initialWorldRotation;
    private float currentAngle;

    public void Configure(Vector3 axis, float speed, bool worldSpace = false)
    {
        rotationAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;
        degreesPerSecond = speed;
        rotateInWorldSpace = worldSpace;
        CaptureInitialRotation();
    }

    public void ForceHorizontalRotation()
    {
        forceHorizontalRoadRotation = true;
        rotationAxis = Vector3.up;
        rotateInWorldSpace = true;
        CaptureInitialRotation();
    }

    public void RecalculatePivot()
    {
        CaptureInitialRotation();
    }

    private void Awake()
    {
        CaptureInitialRotation();
    }

    private void OnEnable()
    {
        CaptureInitialRotation();
    }

    private void CaptureInitialRotation()
    {
        if (forceHorizontalRoadRotation)
        {
            rotationAxis = Vector3.up;
            rotateInWorldSpace = true;
        }
        else
        {
            rotationAxis = rotationAxis.sqrMagnitude > 0.0001f
                ? rotationAxis.normalized
                : Vector3.up;
        }

        initialLocalRotation = transform.localRotation;
        initialWorldRotation = transform.rotation;
        currentAngle = randomizeStartingAngle && Application.isPlaying
            ? Random.Range(0f, 360f)
            : 0f;
    }

    private void Update()
    {
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        currentAngle = Mathf.Repeat(currentAngle + degreesPerSecond * deltaTime, 360f);
        Quaternion spin = Quaternion.AngleAxis(currentAngle, rotationAxis);

        if (rotateInWorldSpace)
        {
            transform.rotation = spin * initialWorldRotation;
        }
        else
        {
            transform.localRotation = initialLocalRotation * spin;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (forceHorizontalRoadRotation)
        {
            rotationAxis = Vector3.up;
            rotateInWorldSpace = true;
        }
        else
        {
            rotationAxis = rotationAxis.sqrMagnitude > 0.0001f
                ? rotationAxis.normalized
                : Vector3.up;
        }
    }
#endif
}
