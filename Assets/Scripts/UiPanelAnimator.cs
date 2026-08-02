using UnityEngine;

public sealed class UiPanelAnimator : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float duration = 0.24f;
    [SerializeField] private Vector3 hiddenScale = new Vector3(0.88f, 0.88f, 1f);

    private float elapsed;
    private bool animating;

    private void OnEnable()
    {
        elapsed = 0f;
        animating = true;
        transform.localScale = hiddenScale;
    }

    private void Update()
    {
        if (!animating)
        {
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        t = 1f - Mathf.Pow(1f - t, 3f);
        transform.localScale = Vector3.LerpUnclamped(hiddenScale, Vector3.one, t);

        if (elapsed >= duration)
        {
            transform.localScale = Vector3.one;
            animating = false;
        }
    }

    private void OnDisable()
    {
        transform.localScale = Vector3.one;
        animating = false;
    }
}
