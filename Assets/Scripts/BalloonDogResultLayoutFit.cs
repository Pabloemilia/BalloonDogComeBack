using UnityEngine;

// Keeps the reference composition and both actions inside the parent safe area.
[RequireComponent(typeof(RectTransform))]
public sealed class BalloonDogResultLayoutFit : MonoBehaviour
{
    private RectTransform rect;

    private void OnEnable()
    {
        rect = GetComponent<RectTransform>();
        Fit();
    }

    private void LateUpdate()
    {
        Fit();
    }

    private void Fit()
    {
        RectTransform parent = rect.parent as RectTransform;
        if (parent == null || parent.rect.width <= 0f || parent.rect.height <= 0f)
        {
            return;
        }
        float scale = Mathf.Min(parent.rect.width / 1080f, parent.rect.height / 2348f);
        rect.localScale = Vector3.one * scale;
    }
}
