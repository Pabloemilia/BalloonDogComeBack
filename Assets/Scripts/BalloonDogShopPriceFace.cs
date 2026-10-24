using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

/// <summary>Draws only the orange capsule face from an opaque shop atlas crop.</summary>
public sealed class BalloonDogShopPriceFace : Image
{
    public Rect FaceBounds
    {
        get
        {
            Rect r = GetPixelAdjustedRect();
            // Leave the original rim stationary and exclude blue atlas corners.
            return Rect.MinMaxRect(r.xMin + r.width * .025f, r.yMin + r.height * .10f,
                r.xMax - r.width * .025f, r.yMax - r.height * .10f);
        }
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (sprite == null) return;
        Rect full = GetPixelAdjustedRect();
        if (full.width <= 0f || full.height <= 0f) return;
        Rect face = FaceBounds;
        float radius = Mathf.Min(face.width, face.height) * .5f;
        Vector4 uv = DataUtility.GetOuterUV(sprite);
        AddVertex(mesh, face.center, full, uv);
        const int segments = 64;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 corner = new Vector2(direction.x >= 0f ? face.xMax - radius : face.xMin + radius,
                direction.y >= 0f ? face.yMax - radius : face.yMin + radius);
            AddVertex(mesh, corner + direction * radius, full, uv);
            if (i > 0) mesh.AddTriangle(0, i, i + 1);
        }
    }

    private void AddVertex(VertexHelper mesh, Vector2 point, Rect full, Vector4 uv)
    {
        Vector2 fraction = new Vector2((point.x - full.xMin) / full.width, (point.y - full.yMin) / full.height);
        mesh.AddVert(new Vector3(point.x, point.y, 0f), color,
            new Vector2(Mathf.Lerp(uv.x, uv.z, fraction.x), Mathf.Lerp(uv.y, uv.w, fraction.y)));
    }
}
