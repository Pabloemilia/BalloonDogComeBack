using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

/// <summary>Orange-only mesh and hit shape measured on the 1821 x 864 source artwork.</summary>
public sealed class BalloonDogCaseButtonFace : Image
{
    // Top-left pixel coordinates: omit the gold rim and the central question-mark badge.
    private static readonly Vector2[] Outline = {
        new Vector2(178,470), new Vector2(184,403), new Vector2(205,350),
        new Vector2(248,312), new Vector2(309,289), new Vector2(655,289),
        new Vector2(697,327), new Vector2(751,350), new Vector2(1069,350),
        new Vector2(1124,327), new Vector2(1166,289), new Vector2(1509,289),
        new Vector2(1571,312), new Vector2(1614,350), new Vector2(1637,403),
        new Vector2(1643,470), new Vector2(1633,536), new Vector2(1606,582),
        new Vector2(1563,614), new Vector2(1503,631), new Vector2(318,631),
        new Vector2(258,614), new Vector2(215,582), new Vector2(188,536)
    };
    private float depth;
    public void SetDepth(float value)
    {
        if (Mathf.Approximately(depth, value)) return;
        depth = value;
        SetVerticesDirty();
    }

    private Rect ArtRect()
    {
        Rect r = GetPixelAdjustedRect();
        float aspect = sprite != null ? sprite.rect.width / sprite.rect.height : 1821f / 864f;
        if (r.width / r.height > aspect) {
            float w = r.height * aspect; r.x += (r.width - w) * 0.5f; r.width = w;
        } else {
            float h = r.width / aspect; r.y += (r.height - h) * 0.5f; r.height = h;
        }
        return r;
    }

    public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out local)) return false;
        Rect r = ArtRect();
        Vector2 p = new Vector2((local.x - r.x) / r.width * 1821f,
            (1f - (local.y - r.y) / r.height) * 864f);
        bool inside = false;
        for (int i = 0, j = Outline.Length - 1; i < Outline.Length; j = i++) {
            Vector2 a = Outline[i], b = Outline[j];
            if ((a.y > p.y) != (b.y > p.y) &&
                p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
        }
        return inside;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (sprite == null) return;
        Rect r = ArtRect();
        Vector4 uv = DataUtility.GetOuterUV(sprite);
        var points = new List<Vector2>();
        Color tint = color;
        for (int i = 0; i < Outline.Length; i++) {
            Vector2 p = Outline[i];
            Vector2 unit = new Vector2(p.x / 1821f, 1f - p.y / 864f);
            Vector2 pos = new Vector2(r.x + unit.x * r.width, r.y + unit.y * r.height);
            Vector2 center = new Vector2(r.center.x, r.y + (1f - 460f / 864f) * r.height);
            pos = center + (pos - center) * (1f - 0.025f * depth) + Vector2.down * (5f * depth);
            points.Add(pos);
            vh.AddVert(pos, tint, new Vector2(Mathf.Lerp(uv.x, uv.z, unit.x), Mathf.Lerp(uv.y, uv.w, unit.y)));
        }
        // Ear clipping supports the concave notch beneath the stationary badge.
        var indices = new List<int>();
        for (int i = 0; i < points.Count; i++) indices.Add(i);
        float area = 0f;
        for (int i = 0; i < points.Count; i++) area += Cross(points[i], points[(i + 1) % points.Count]);
        float sign = Mathf.Sign(area);
        int guard = points.Count * points.Count;
        while (indices.Count > 2 && guard-- > 0) {
            bool clipped = false;
            for (int i = 0; i < indices.Count; i++) {
                int a = indices[(i + indices.Count - 1) % indices.Count], b = indices[i], c = indices[(i + 1) % indices.Count];
                if (Cross(points[b] - points[a], points[c] - points[b]) * sign <= 0f) continue;
                bool occupied = false;
                foreach (int k in indices) {
                    if (k == a || k == b || k == c) continue;
                    Vector2 p = points[k];
                    if (Cross(points[b]-points[a], p-points[a])*sign >= 0f &&
                        Cross(points[c]-points[b], p-points[b])*sign >= 0f &&
                        Cross(points[a]-points[c], p-points[c])*sign >= 0f) { occupied = true; break; }
                }
                if (occupied) continue;
                vh.AddTriangle(a, b, c);
                indices.RemoveAt(i); clipped = true; break;
            }
            if (!clipped) break;
        }
    }
    private static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }
}
