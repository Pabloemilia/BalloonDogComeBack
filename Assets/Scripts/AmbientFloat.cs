using UnityEngine;

public sealed class AmbientFloat : MonoBehaviour
{
    [SerializeField] private Vector3 movementAmplitude = new Vector3(0.4f, 0.25f, 0f);
    [SerializeField, Min(0.01f)] private float frequency = 0.2f;
    [SerializeField] private Vector3 rotationAmplitude = new Vector3(0f, 8f, 2f);
    [SerializeField] private float phase;

    private Vector3 startPosition;
    private Quaternion startRotation;

    public void Configure(Vector3 move, float moveFrequency, Vector3 rotate, float phaseOffset)
    {
        movementAmplitude = move;
        frequency = Mathf.Max(0.01f, moveFrequency);
        rotationAmplitude = rotate;
        phase = phaseOffset;
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    private void Awake()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    private void Update()
    {
        float time = Time.time * frequency * Mathf.PI * 2f + phase;
        float wave = Mathf.Sin(time);
        float waveB = Mathf.Cos(time * 0.73f);
        transform.localPosition = startPosition + Vector3.Scale(
            movementAmplitude,
            new Vector3(wave, waveB, wave));
        transform.localRotation = startRotation * Quaternion.Euler(rotationAmplitude * wave);
    }
}
