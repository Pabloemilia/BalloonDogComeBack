using UnityEngine;

[DisallowMultipleComponent]
public sealed class BalloonDogFloatingLetter : MonoBehaviour
{
    private RectTransform rect;
    private Vector2 basePosition;
    private float amplitude = 12f;
    private float frequency = 1.3f;
    private float phase;
    private float rotationAmplitude = 2f;

    public void Configure(int index, float amplitudeValue, float frequencyValue, float phaseValue)
    {
        rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            basePosition = rect.anchoredPosition;
        }

        amplitude = amplitudeValue;
        frequency = frequencyValue;
        phase = phaseValue + index * 0.17f;
        rotationAmplitude = Mathf.Lerp(1.5f, 4f, Mathf.Abs(Mathf.Sin(index * 1.31f)));
    }

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            basePosition = rect.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        if (rect == null)
        {
            rect = GetComponent<RectTransform>();
        }

        if (rect != null)
        {
            basePosition = rect.anchoredPosition;
        }
    }

    private void LateUpdate()
    {
        if (rect == null)
        {
            rect = GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            basePosition = rect.anchoredPosition;
        }

        float t = Time.unscaledTime;
        float y = Mathf.Sin(t * frequency + phase) * amplitude;
        float x = Mathf.Sin(t * (frequency * 0.55f) + phase * 0.7f) * 2.5f;
        rect.anchoredPosition = basePosition + new Vector2(x, y);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * (frequency * 0.75f) + phase) * rotationAmplitude);
    }
}
