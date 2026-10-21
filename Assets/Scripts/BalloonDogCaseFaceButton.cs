using UnityEngine;
using UnityEngine.UI;

/// <summary>Presses the orange surface and its label without moving the decorative frame.</summary>
public sealed class BalloonDogCaseFaceButton : Button
{
    private BalloonDogCaseButtonFace face;
    private RectTransform label;
    private Vector2 labelOrigin;
    private float depth;
    private float targetDepth;

    public void Configure(BalloonDogCaseButtonFace surface, RectTransform text)
    {
        face = surface;
        label = text;
        labelOrigin = text.anchoredPosition;
        DoStateTransition(currentSelectionState, true);
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        targetDepth = state == SelectionState.Pressed && IsInteractable() ? 1f : 0f;
        // Never tint a second polygon over the orange artwork at rest.
        if (face != null) face.color = Color.white;
        if (instant) { depth = targetDepth; ApplyDepth(); }
    }

    private void Update()
    {
        depth = Mathf.MoveTowards(depth, targetDepth, Time.unscaledDeltaTime * 16f);
        ApplyDepth();
    }

    private void ApplyDepth()
    {
        if (face != null) face.SetDepth(depth);
        if (label == null) return;
        label.anchoredPosition = labelOrigin + Vector2.down * (5f * depth);
        label.localScale = Vector3.one * (1f - 0.025f * depth);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        depth = targetDepth = 0f;
        ApplyDepth();
    }
}
