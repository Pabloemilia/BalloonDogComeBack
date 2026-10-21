using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Unscaled press feedback for result controls, with a visible short-tap impulse.</summary>
public sealed class BalloonDogResultPressFeedback : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private Button button;
    private RectTransform rect;
    private Vector3 scale;
    private Vector2 origin;
    private Graphic[] graphics;
    private Color[] colors;
    private bool held;
    private float until;
    private float depth;
    private void Awake()
    {
        button = GetComponent<Button>(); rect = GetComponent<RectTransform>();
        scale = rect.localScale; origin = rect.anchoredPosition;
        graphics = GetComponentsInChildren<Graphic>(true);
        colors = new Color[graphics.Length];
        for (int i = 0; i < graphics.Length; i++) colors[i] = graphics[i].color;
    }
    public void OnPointerDown(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || button == null || !button.IsInteractable()) return;
        held = true; until = Time.unscaledTime + .12f; depth = 1f; Apply();
    }
    public void OnPointerUp(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) held = false; }
    public void OnPointerExit(PointerEventData data) { held = false; until = 0f; }
    private void Update()
    {
        if (!held && Time.unscaledTime >= until) depth = Mathf.MoveTowards(depth, 0f, Time.unscaledDeltaTime * 9f);
        Apply();
    }
    private void Apply()
    {
        if (rect == null) return;
        rect.localScale = scale * (1f - .04f * depth);
        rect.anchoredPosition = origin + Vector2.down * (5f * depth);
        float brightness = 1f - .25f * depth;
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] == null) continue;
            Color c = colors[i]; graphics[i].color = new Color(c.r * brightness, c.g * brightness, c.b * brightness, c.a);
        }
    }
    private void OnDisable() { held = false; depth = until = 0f; Apply(); }
}
