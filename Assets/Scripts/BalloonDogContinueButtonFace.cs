using UnityEngine;
using UnityEngine.UI;

/// <summary>Draws only the rounded green face; source UVs never include the surrounding card.</summary>
public sealed class BalloonDogContinueButtonFace : MaskableGraphic, ICanvasRaycastFilter
{
    private Texture2D artwork;
    private float depth;
    private const float SourceX = 252f;
    private const float SourceY = 580f;
    private const float SourceWidth = 1034f;
    private const float SourceHeight = 200f;
    private const float Radius = 58f;
    public override Texture mainTexture => artwork != null ? artwork : Texture2D.whiteTexture;

    public void Configure(Texture2D texture)
    {
        artwork = texture;
        SetAllDirty();
    }
    public void SetDepth(float value)
    {
        if (Mathf.Approximately(value, depth)) return;
        depth = value;
        SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (artwork == null) return;
        Rect r = GetPixelAdjustedRect();
        Color tint = Color.Lerp(Color.white, new Color(.77f, .82f, .77f, 1f), depth) * color;
        AddPoint(vh, r, SourceWidth * .5f, SourceHeight * .5f, tint);
        const int segments = 12;
        for (int corner = 0; corner < 4; corner++)
        {
            // Source-space y runs downwards. Walk clockwise around the green face.
            float cx = corner == 0 || corner == 1 ? SourceWidth - Radius : Radius;
            float cy = corner == 0 || corner == 3 ? Radius : SourceHeight - Radius;
            float start = -90f + corner * 90f;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (start + i * 90f / segments) * Mathf.Deg2Rad;
                // The next corner sequence is TR, BR, BL, TL.
                AddPoint(vh, r, cx + Mathf.Cos(angle) * Radius,
                    cy + Mathf.Sin(angle) * Radius, tint);
            }
        }
        int count = 4 * (segments + 1);
        for (int i = 0; i < count; i++) vh.AddTriangle(0, i + 1, (i + 1) % count + 1);
    }
    private void AddPoint(VertexHelper vh, Rect r, float x, float y, Color tint)
    {
        Vector2 point = new Vector2(r.x + x / SourceWidth * r.width,
            r.y + (1f - y / SourceHeight) * r.height);
        Vector2 inset = point - r.center;
        inset.x *= 1f - .027f * depth;
        inset.y *= 1f - .06f * depth;
        point = r.center + inset + Vector2.down * (3.5f * depth);
        vh.AddVert(point, tint, new Vector2((SourceX + x) / 1536f,
            1f - (SourceY + y) / 1024f));
    }
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera camera)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, camera, out local)) return false;
        Rect r = GetPixelAdjustedRect();
        float x = (local.x - r.x) / r.width * SourceWidth;
        float y = (1f - (local.y - r.y) / r.height) * SourceHeight;
        if (x < 0f || y < 0f || x > SourceWidth || y > SourceHeight) return false;
        float dx = x - Mathf.Clamp(x, Radius, SourceWidth - Radius);
        float dy = y - Mathf.Clamp(y, Radius, SourceHeight - Radius);
        return dx * dx + dy * dy <= Radius * Radius;
    }
}
