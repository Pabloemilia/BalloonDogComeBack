using UnityEngine;
using UnityEngine.UI;

/// <summary>Editable capsule and accent graphics visible in the Settings reference.</summary>
public sealed class BalloonDogSettingsPill : MaskableGraphic
{
    public Color Top = Color.white;
    public Color Bottom = Color.white;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        float radius = Mathf.Min(r.height, r.width) * 0.5f;
        const int segments = 64;
        mesh.AddVert(r.center, Color.Lerp(Bottom, Top, 0.5f), Vector2.zero);
        for (int i = 0; i <= segments; i++)
        {
            float a = 2f * Mathf.PI * i / segments;
            float c = Mathf.Cos(a);
            Vector2 p = r.center + new Vector2((c >= 0f ? 1f : -1f) * (r.width * 0.5f - radius) + c * radius, Mathf.Sin(a) * radius);
            mesh.AddVert(p, Color.Lerp(Bottom, Top, Mathf.InverseLerp(r.yMin, r.yMax, p.y)), Vector2.zero);
            if (i > 0) mesh.AddTriangle(0, i, i + 1);
        }
    }
}
