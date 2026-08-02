using UnityEngine;

/// <summary>
/// Bir görsel pivotu yalnızca kendi yerel ekseni etrafında döndürür.
/// Konuma dokunmadığı için engel yol dışına yörünge çizmez.
/// Editör kurulumu, bu bileşeni modelin görünür merkezine yerleştirilmiş
/// StableSpinPivot nesnesine bağlar.
/// </summary>
public sealed class RotatingObstacle : MonoBehaviour
{
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float degreesPerSecond = 70f;
    [SerializeField] private bool rotateInWorldSpace;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private bool randomizeStartingAngle = true;

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
        rotationAxis = rotationAxis.sqrMagnitude > 0.0001f
            ? rotationAxis.normalized
            : Vector3.up;

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
        rotationAxis = rotationAxis.sqrMagnitude > 0.0001f
            ? rotationAxis.normalized
            : Vector3.up;
    }
#endif
}
