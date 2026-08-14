using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BalloonDogPauseScreenAnimator : MonoBehaviour
{
    private enum AnimationState
    {
        Hidden,
        Showing,
        Visible,
        Hiding
    }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private RectTransform[] stagedElements;
    [SerializeField, Min(0.2f)] private float showDuration = 0.46f;
    [SerializeField, Min(0.1f)] private float hideDuration = 0.19f;
    [SerializeField, Min(0f)] private float stagger = 0.055f;
    [SerializeField, Min(0f)] private float entranceOffset = 34f;

    private Vector2[] basePositions;
    private Vector3[] baseScales;
    private Vector3 contentBaseScale = Vector3.one;
    private AnimationState state = AnimationState.Hidden;
    private Action hiddenCallback;
    private float elapsed;
    private bool configured;

    public bool IsTransitioning =>
        state == AnimationState.Showing ||
        state == AnimationState.Hiding;

    public void Configure(
        CanvasGroup group,
        RectTransform root,
        RectTransform[] elements)
    {
        canvasGroup = group;
        contentRoot = root;
        stagedElements = elements ?? Array.Empty<RectTransform>();
        basePositions = new Vector2[stagedElements.Length];
        baseScales = new Vector3[stagedElements.Length];

        for (int index = 0; index < stagedElements.Length; index++)
        {
            RectTransform element = stagedElements[index];
            if (element == null)
            {
                continue;
            }

            basePositions[index] = element.anchoredPosition;
            baseScales[index] = element.localScale;
        }

        if (contentRoot != null)
        {
            contentBaseScale = contentRoot.localScale;
        }

        configured = true;
        ApplyHiddenFrame();
    }

    public void PlayShow()
    {
        if (!configured)
        {
            return;
        }

        hiddenCallback = null;
        elapsed = 0f;
        state = AnimationState.Showing;
        ApplyHiddenFrame();
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void PlayHide(Action onHidden)
    {
        if (!configured || state == AnimationState.Hidden)
        {
            onHidden?.Invoke();
            return;
        }

        hiddenCallback = onHidden;
        elapsed = 0f;
        state = AnimationState.Hiding;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnEnable()
    {
        if (configured)
        {
            ApplyHiddenFrame();
        }
    }

    private void Update()
    {
        if (state == AnimationState.Showing)
        {
            UpdateShow();
        }
        else if (state == AnimationState.Hiding)
        {
            UpdateHide();
        }
    }

    private void UpdateShow()
    {
        elapsed += Time.unscaledDeltaTime;
        float normalized = Mathf.Clamp01(elapsed / showDuration);
        canvasGroup.alpha = EaseOutCubic(normalized);

        for (int index = 0; index < stagedElements.Length; index++)
        {
            RectTransform element = stagedElements[index];
            if (element == null)
            {
                continue;
            }

            float delay = stagger * index;
            float available = Mathf.Max(0.08f, showDuration - delay);
            float t = Mathf.Clamp01((elapsed - delay) / available);
            float eased = EaseOutCubic(t);
            float scale = Mathf.LerpUnclamped(0.92f, 1f, EaseOutBack(t));

            element.anchoredPosition = Vector2.LerpUnclamped(
                basePositions[index] + Vector2.down * entranceOffset,
                basePositions[index],
                eased);
            element.localScale = baseScales[index] * scale;
        }

        if (normalized < 1f)
        {
            return;
        }

        RestoreBaseTransforms();
        canvasGroup.alpha = 1f;
        state = AnimationState.Visible;
    }

    private void UpdateHide()
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / hideDuration);
        float eased = EaseInOutSine(t);
        canvasGroup.alpha = 1f - eased;

        if (contentRoot != null)
        {
            contentRoot.localScale = Vector3.LerpUnclamped(
                contentBaseScale,
                contentBaseScale * 0.985f,
                eased);
        }

        if (t < 1f)
        {
            return;
        }

        state = AnimationState.Hidden;
        RestoreBaseTransforms();
        Action callback = hiddenCallback;
        hiddenCallback = null;
        gameObject.SetActive(false);
        callback?.Invoke();
    }

    private void OnDisable()
    {
        if (!configured)
        {
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        RestoreBaseTransforms();
        state = AnimationState.Hidden;
    }

    private void ApplyHiddenFrame()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        RestoreBaseTransforms();

        for (int index = 0; index < stagedElements.Length; index++)
        {
            RectTransform element = stagedElements[index];
            if (element == null)
            {
                continue;
            }

            element.anchoredPosition =
                basePositions[index] + Vector2.down * entranceOffset;
            element.localScale = baseScales[index] * 0.92f;
        }
    }

    private void RestoreBaseTransforms()
    {
        if (contentRoot != null)
        {
            contentRoot.localScale = contentBaseScale;
        }

        if (stagedElements == null)
        {
            return;
        }

        for (int index = 0; index < stagedElements.Length; index++)
        {
            RectTransform element = stagedElements[index];
            if (element == null)
            {
                continue;
            }

            element.anchoredPosition = basePositions[index];
            element.localScale = baseScales[index];
        }
    }

    private static float EaseOutCubic(float t)
    {
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    private static float EaseOutBack(float t)
    {
        const float overshoot = 1.18f;
        float shifted = t - 1f;
        return 1f + shifted * shifted *
            ((overshoot + 1f) * shifted + overshoot);
    }

    private static float EaseInOutSine(float t)
    {
        return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
    }
}
