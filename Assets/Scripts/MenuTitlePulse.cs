using UnityEngine;

public sealed class MenuTitlePulse : MonoBehaviour
{
    [SerializeField] private float scaleAmount = 0.025f;
    [SerializeField] private float speed = 1.5f;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * speed) * scaleAmount;
        transform.localScale = baseScale * pulse;
    }
}
