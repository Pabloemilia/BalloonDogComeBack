using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Immediate feedback, including taps released before the next Update.</summary>
public sealed class BalloonDogContinueFaceButton : Button
{
    private BalloonDogContinueButtonFace face;
    private float depth;
    private float visibleUntil;
    private bool held;
    public void Configure(BalloonDogContinueButtonFace graphic)
    {
        face = graphic;
        ResetFeedback();
    }
    protected override void DoStateTransition(SelectionState state, bool instant) { }
    public override void OnPointerDown(PointerEventData data)
    {
        base.OnPointerDown(data);
        if (data.button != PointerEventData.InputButton.Left || !IsActive() || !IsInteractable()) return;
        held = true;
        visibleUntil = Time.unscaledTime + .12f;
        depth = 1f;
        if (face != null) face.SetDepth(depth);
    }
    public override void OnPointerUp(PointerEventData data)
    {
        base.OnPointerUp(data);
        if (data.button == PointerEventData.InputButton.Left) held = false;
    }
    public override void OnPointerExit(PointerEventData data)
    {
        base.OnPointerExit(data);
        held = false;
        visibleUntil = 0f;
    }
    private void Update()
    {
        if (!held && Time.unscaledTime >= visibleUntil)
            depth = Mathf.MoveTowards(depth, 0f, Time.unscaledDeltaTime * 9f);
        if (face != null) face.SetDepth(depth);
    }
    private void ResetFeedback()
    {
        held = false; depth = visibleUntil = 0f;
        if (face != null) face.SetDepth(0f);
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        ResetFeedback();
    }
}
