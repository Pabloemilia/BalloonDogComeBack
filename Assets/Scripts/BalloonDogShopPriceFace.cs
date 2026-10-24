using UnityEngine;
using UnityEngine.UI;

/// <summary>A texture-free, feathered press tint inside the painted orange face.</summary>
public sealed class BalloonDogShopPriceFace : MaskableGraphic
{
    public Rect FaceBounds
    {
        get
        {
            Rect r = GetPixelAdjustedRect();
            // Keep the original painted rim, highlights and blue corners untouched.
            return Rect.MinMaxRect(r.xMin + r.width * .025f, r.yMin + r.height * .10f,
                r.xMax - r.width * .025f, r.yMax - r.height * .10f);
        }
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect full = GetPixelAdjustedRect();
        if (full.width <= 0f || full.height <= 0f) return;
        Rect face = FaceBounds;
        float feather = Mathf.Min(8f, face.height * .15f);
        Rect inner = Rect.MinMaxRect(face.xMin + feather, face.yMin + feather,
            face.xMax - feather, face.yMax - feather);
        mesh.AddVert(face.center, color, Vector2.zero);
        const int segments = 66; // Both endpoints of each semicircle, plus two straight sides.
        Color edge = color;
        edge.a = 0f;
        for (int i = 0; i < segments; i++)
        {
            mesh.AddVert(CapsulePoint(inner, i), color, Vector2.zero);
            mesh.AddVert(CapsulePoint(face, i), edge, Vector2.zero);
        }
        for (int i = 0; i < segments; i++)
        {
            int current = 1 + i * 2;
            int next = 1 + ((i + 1) % segments) * 2;
            mesh.AddTriangle(0, current, next);
            mesh.AddTriangle(current, current + 1, next + 1);
            mesh.AddTriangle(current, next + 1, next);
        }
    }

    private static Vector2 CapsulePoint(Rect bounds, int index)
    {
        float radius = Mathf.Min(bounds.width, bounds.height) * .5f;
        bool right = index <= 32;
        float angle = right ? -Mathf.PI * .5f + index * Mathf.PI / 32f
            : Mathf.PI * .5f + (index - 33) * Mathf.PI / 32f;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 centre = new Vector2(right ? bounds.xMax - radius : bounds.xMin + radius,
            bounds.center.y);
        return centre + direction * radius;
    }
}
