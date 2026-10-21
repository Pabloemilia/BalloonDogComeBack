using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared tactile feedback for the four Extras cards, with a stable hit area.</summary>
public sealed class BalloonDogStoreCardButton : Button
{
    private RectTransform content;
    private Graphic[] graphics;
    private Color[] originalColors;
    private Vector2 origin;
    private Vector3 originalScale;
    private float depth;
    private float targetDepth;

    public void Configure(RectTransform visualContent)
    {
        content = visualContent;
        origin = content.anchoredPosition;
        originalScale = content.localScale;
        graphics = content.GetComponentsInChildren<Graphic>(true);
        originalColors = new Color[graphics.Length];
        for (int i = 0; i < graphics.Length; i++) originalColors[i] = graphics[i].color;
        DoStateTransition(currentSelectionState, true);
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        targetDepth = state == SelectionState.Pressed && IsInteractable() ? 1f : 0f;
        if (instant) { depth = targetDepth; ApplyFeedback(); }
    }

    private void Update()
    {
        depth = Mathf.MoveTowards(depth, targetDepth, Time.unscaledDeltaTime * 18f);
        ApplyFeedback();
    }

    private void ApplyFeedback()
    {
        if (content == null) return;
        content.localScale = originalScale * (1f - 0.045f * depth);
        content.anchoredPosition = origin + Vector2.down * (7f * depth);
        float brightness = Mathf.Lerp(1f, 0.78f, depth);
        for (int i = 0; i < graphics.Length; i++) {
            if (graphics[i] == null) continue;
            Color c = originalColors[i];
            graphics[i].color = new Color(c.r * brightness, c.g * brightness, c.b * brightness, c.a);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        depth = targetDepth = 0f;
        ApplyFeedback();
    }
}
