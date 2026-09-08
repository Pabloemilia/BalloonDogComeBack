using UnityEngine;
using UnityEngine.UI;

/// <summary>Three colour stops sampled from unobstructed areas of the reference.</summary>
public sealed class BalloonDogSettingsBackdrop : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        Color32[] colors = { new Color32(80, 197, 120, 255), new Color32(39, 176, 190, 255), new Color32(29, 112, 242, 255) };
        for (int i = 0; i < 3; i++)
        {
            float y = Mathf.Lerp(r.yMin, r.yMax, i * 0.5f);
            mesh.AddVert(new Vector3(r.xMin, y), colors[i], Vector2.zero);
            mesh.AddVert(new Vector3(r.xMax, y), colors[i], Vector2.zero);
            if (i > 0)
            {
                int v = i * 2;
                mesh.AddTriangle(v - 2, v, v - 1);
                mesh.AddTriangle(v - 1, v, v + 1);
            }
        }
    }
}
