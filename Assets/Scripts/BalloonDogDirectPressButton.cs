using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>One input owner: native graphics, direct pointer input, unscaled animation.</summary>
public sealed class BalloonDogDirectPressButton : MonoBehaviour
{
    private Button actionButton;
    private RectTransform hitRect;
    private RectTransform visual;
    private Graphic[] graphics;
    private Color[] baseColors;
    private Vector3 baseScale;
    private Vector2 basePosition;
    private Vector2 roundRadius;
    private bool insetFace;
    private Material panelPressMaterial;
    private bool ready;
    private bool captured;
    private bool cancelled;
    private bool showingPress;
    private bool waitForRelease;
    private float depth;
    private float visibleUntil;

    public void Configure(Button button, RectTransform visualRoot, Vector2 radius, bool inset)
    {
        actionButton = button;
        hitRect = (RectTransform)button.transform;
        visual = visualRoot;
        roundRadius = radius;
        insetFace = inset;
        // Disable event-driven click dispatch so a physical release cannot fire twice.
        button.transition = Selectable.Transition.None;
        button.enabled = false;
        baseScale = visual.localScale;
        basePosition = visual.anchoredPosition;
        graphics = visual.GetComponentsInChildren<Graphic>(true);
        baseColors = new Color[graphics.Length];
        for (int i = 0; i < graphics.Length; i++)
        {
            baseColors[i] = graphics[i].color;
            graphics[i].raycastTarget = false;
        }
        ReadPointer(out Vector2 ignored, out bool pressed, out bool down, out bool up);
        waitForRelease = pressed || down;
        ready = true;
        ResetVisual();
    }

    public void UsePanelPressMaterial(Material material)
    {
        panelPressMaterial = material;
        if (panelPressMaterial != null) panelPressMaterial.SetFloat("_Press", 0f);
    }

    private void Update()
    {
        if (!ready || actionButton == null) return;
        ReadPointer(out Vector2 position, out bool pressed, out bool down, out bool up);
        if (waitForRelease)
        {
            if (!pressed) waitForRelease = false;
            return;
        }
        bool inside = Contains(position);
        bool interactable = actionButton.interactable && actionButton.IsInteractable();
        if (down && interactable && inside && !captured)
        {
            captured = true; cancelled = false; showingPress = true;
            depth = 1f; visibleUntil = Time.unscaledTime + .12f;
            Apply();
        }
        if (captured && (!inside || !interactable))
        {
            cancelled = true; showingPress = false; visibleUntil = 0f;
        }
        if (captured && (up || !pressed))
        {
            bool click = !cancelled && inside && interactable;
            captured = false; showingPress = false;
            // Clear gesture state before an action changes scene or disables this object.
            if (click) actionButton.onClick.Invoke();
            if (!this || !isActiveAndEnabled) return;
        }
        if (!showingPress && Time.unscaledTime >= visibleUntil)
            depth = Mathf.MoveTowards(depth, 0f, Time.unscaledDeltaTime * 9f);
        Apply();
    }

    private bool Contains(Vector2 screen)
    {
        Canvas canvas = hitRect.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(hitRect, screen, camera, out local)) return false;
        Rect r = hitRect.rect;
        if (!r.Contains(local)) return false;
        if (roundRadius.x <= 0f || roundRadius.y <= 0f) return true;
        float x = (local.x - r.xMin) / r.width;
        float y = (local.y - r.yMin) / r.height;
        float dx = (x - Mathf.Clamp(x, roundRadius.x, 1f - roundRadius.x)) / roundRadius.x;
        float dy = (y - Mathf.Clamp(y, roundRadius.y, 1f - roundRadius.y)) / roundRadius.y;
        return dx * dx + dy * dy <= 1f;
    }

    private static void ReadPointer(out Vector2 position, out bool pressed, out bool down, out bool up)
    {
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.isPressed || touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame)
            {
                position = touch.position.ReadValue(); pressed = touch.press.isPressed;
                down = touch.press.wasPressedThisFrame; up = touch.press.wasReleasedThisFrame;
                return;
            }
        }
        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue(); pressed = Mouse.current.leftButton.isPressed;
            down = Mouse.current.leftButton.wasPressedThisFrame; up = Mouse.current.leftButton.wasReleasedThisFrame;
            return;
        }
        position = Vector2.negativeInfinity; pressed = down = up = false;
    }
    private void Apply()
    {
        if (!ready || visual == null) return;
        if (panelPressMaterial != null)
        {
            panelPressMaterial.SetFloat("_Press", depth);
            return;
        }
        visual.localScale = Vector3.Scale(baseScale, new Vector3(1f - (insetFace ? .027f : .04f) * depth,
            1f - (insetFace ? .06f : .04f) * depth, 1f));
        visual.anchoredPosition = basePosition + Vector2.down * ((insetFace ? 3.5f : 5f) * depth);
        float brightness = 1f - .27f * depth;
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] == null) continue;
            Color c = baseColors[i];
            graphics[i].color = new Color(c.r * brightness, c.g * brightness, c.b * brightness, c.a);
        }
    }
    private void ResetVisual()
    {
        captured = cancelled = showingPress = false;
        depth = visibleUntil = 0f;
        Apply();
    }
    private void OnDisable() { ResetVisual(); }
}
