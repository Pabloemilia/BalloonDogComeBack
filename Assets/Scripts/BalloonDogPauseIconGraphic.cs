using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Resolution-independent Pause Menu icons drawn directly by Unity UI.
/// This avoids font-glyph placeholders and keeps the icons sharp on mobile.
/// </summary>
[DisallowMultipleComponent]
public sealed class BalloonDogPauseIconGraphic : MaskableGraphic
{
    public enum IconType
    {
        Play,
        Restart,
        Settings,
        Home,
        Crown
    }

    [SerializeField] private IconType iconType;

    public void Configure(IconType type, Color iconColor)
    {
        iconType = type;
        color = iconColor;
        raycastTarget = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        Rect rect = GetPixelAdjustedRect();
        float scale = Mathf.Min(rect.width, rect.height);
        Vector2 center = rect.center;

        switch (iconType)
        {
            case IconType.Play:
                AddTriangle(
                    vertexHelper,
                    ToPoint(center, scale, -0.24f, -0.34f),
                    ToPoint(center, scale, -0.24f, 0.34f),
                    ToPoint(center, scale, 0.34f, 0f));
                break;
            case IconType.Restart:
                AddArc(vertexHelper, center, scale * 0.31f, scale * 0.105f, 36f, 302f, 28);
                AddTriangle(
                    vertexHelper,
                    ToPoint(center, scale, -0.36f, 0.24f),
                    ToPoint(center, scale, -0.04f, 0.32f),
                    ToPoint(center, scale, -0.24f, 0.02f));
                break;
            case IconType.Settings:
                AddGear(vertexHelper, center, scale);
                break;
            case IconType.Home:
                AddHome(vertexHelper, center, scale);
                break;
            case IconType.Crown:
                AddCrown(vertexHelper, center, scale);
                break;
        }
    }

    private void AddGear(VertexHelper vh, Vector2 center, float scale)
    {
        const int teeth = 8;
        for (int i = 0; i < teeth; i++)
        {
            float angle = i * Mathf.PI * 2f / teeth;
            Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 tangent = new Vector2(-radial.y, radial.x);
            Vector2 toothCenter = center + radial * scale * 0.34f;
            float halfWidth = scale * 0.105f;
            float halfDepth = scale * 0.105f;
            AddQuad(
                vh,
                toothCenter - tangent * halfWidth - radial * halfDepth,
                toothCenter - tangent * halfWidth + radial * halfDepth,
                toothCenter + tangent * halfWidth + radial * halfDepth,
                toothCenter + tangent * halfWidth - radial * halfDepth);
        }

        AddRing(vh, center, scale * 0.30f, scale * 0.13f, 36);
    }

    private void AddHome(VertexHelper vh, Vector2 center, float scale)
    {
        AddTriangle(
            vh,
            ToPoint(center, scale, -0.40f, 0.02f),
            ToPoint(center, scale, 0f, 0.40f),
            ToPoint(center, scale, 0.40f, 0.02f));

        float left = center.x - scale * 0.29f;
        float right = center.x + scale * 0.29f;
        float top = center.y + scale * 0.05f;
        float bottom = center.y - scale * 0.37f;
        float doorHalf = scale * 0.085f;
        float doorTop = center.y - scale * 0.10f;

        AddRect(vh, new Rect(left, bottom, right - left, doorTop - bottom));
        AddRect(vh, new Rect(left, doorTop, center.x - doorHalf - left, top - doorTop));
        AddRect(vh, new Rect(center.x + doorHalf, doorTop, right - center.x - doorHalf, top - doorTop));
        AddRect(vh, new Rect(center.x - doorHalf, top - scale * 0.08f, doorHalf * 2f, scale * 0.08f));
    }

    private void AddCrown(VertexHelper vh, Vector2 center, float scale)
    {
        AddQuad(
            vh,
            ToPoint(center, scale, -0.34f, -0.24f),
            ToPoint(center, scale, -0.40f, 0.05f),
            ToPoint(center, scale, 0.40f, 0.05f),
            ToPoint(center, scale, 0.34f, -0.24f));
        AddTriangle(
            vh,
            ToPoint(center, scale, -0.40f, 0.04f),
            ToPoint(center, scale, -0.38f, 0.30f),
            ToPoint(center, scale, -0.12f, 0.04f));
        AddTriangle(
            vh,
            ToPoint(center, scale, -0.20f, 0.04f),
            ToPoint(center, scale, 0f, 0.38f),
            ToPoint(center, scale, 0.20f, 0.04f));
        AddTriangle(
            vh,
            ToPoint(center, scale, 0.12f, 0.04f),
            ToPoint(center, scale, 0.38f, 0.30f),
            ToPoint(center, scale, 0.40f, 0.04f));
        AddRect(
            vh,
            new Rect(
                center.x - scale * 0.34f,
                center.y - scale * 0.34f,
                scale * 0.68f,
                scale * 0.13f));
    }

    private void AddRing(VertexHelper vh, Vector2 center, float outerRadius, float innerRadius, int segments)
    {
        AddArc(vh, center, outerRadius, outerRadius - innerRadius, 0f, 360f, segments);
    }

    private void AddArc(
        VertexHelper vh,
        Vector2 center,
        float radius,
        float thickness,
        float startDegrees,
        float endDegrees,
        int segments)
    {
        float innerRadius = Mathf.Max(0f, radius - thickness);
        int startVertex = vh.currentVertCount;
        for (int i = 0; i <= segments; i++)
        {
            float progress = i / (float)segments;
            float angle = Mathf.Lerp(startDegrees, endDegrees, progress) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            AddVertex(vh, center + direction * innerRadius);
            AddVertex(vh, center + direction * radius);
        }

        for (int i = 0; i < segments; i++)
        {
            int index = startVertex + i * 2;
            vh.AddTriangle(index, index + 1, index + 3);
            vh.AddTriangle(index, index + 3, index + 2);
        }
    }

    private void AddRect(VertexHelper vh, Rect rect)
    {
        AddQuad(
            vh,
            new Vector2(rect.xMin, rect.yMin),
            new Vector2(rect.xMin, rect.yMax),
            new Vector2(rect.xMax, rect.yMax),
            new Vector2(rect.xMax, rect.yMin));
    }

    private void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c)
    {
        int index = vh.currentVertCount;
        AddVertex(vh, a);
        AddVertex(vh, b);
        AddVertex(vh, c);
        vh.AddTriangle(index, index + 1, index + 2);
    }

    private void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        int index = vh.currentVertCount;
        AddVertex(vh, a);
        AddVertex(vh, b);
        AddVertex(vh, c);
        AddVertex(vh, d);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }

    private void AddVertex(VertexHelper vh, Vector2 point)
    {
        vh.AddVert(point, color, Vector2.zero);
    }

    private static Vector2 ToPoint(Vector2 center, float scale, float x, float y)
    {
        return center + new Vector2(x * scale, y * scale);
    }
}
