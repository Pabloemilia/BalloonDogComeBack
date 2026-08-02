using UnityEngine;

public sealed class MenuFloatingBubble : MonoBehaviour
{
    [SerializeField] private float verticalDistance = 22f;
    [SerializeField] private float horizontalDistance = 10f;
    [SerializeField] private float speed = 0.55f;
    [SerializeField] private float phase;

    private RectTransform rectTransform;
    private Vector2 startPosition;

    public void Configure(float vertical, float horizontal, float animationSpeed, float phaseOffset)
    {
        verticalDistance = vertical;
        horizontalDistance = horizontal;
        speed = animationSpeed;
        phase = phaseOffset;
    }

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            startPosition = rectTransform.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        if (rectTransform != null)
        {
            startPosition = rectTransform.anchoredPosition;
        }
    }

    private void Update()
    {
        if (rectTransform == null)
        {
            return;
        }

        float time = Time.unscaledTime * speed + phase;
        rectTransform.anchoredPosition = startPosition + new Vector2(
            Mathf.Sin(time * 0.8f) * horizontalDistance,
            Mathf.Sin(time) * verticalDistance);
    }
}
