using UnityEngine;
using UnityEngine.UI;

/// <summary>A geometric chevron centred on the button face, independent of font baselines.</summary>
public sealed class BalloonDogPageChevron : MaskableGraphic
{
    public bool Previous { get; set; }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        float direction = Previous ? -1f : 1f;
        Vector2[] points = {
            new Vector2(-.5f, .5f), new Vector2(-.1f, .5f), new Vector2(.5f, 0f),
            new Vector2(.1f, 0f), new Vector2(-.1f, -.5f), new Vector2(-.5f, -.5f)
        };
        foreach (Vector2 point in points)
            mesh.AddVert(new Vector3(r.center.x + point.x * r.width * direction,
                r.center.y + point.y * r.height, 0f), color, Vector2.zero);
        mesh.AddTriangle(0, 1, 2);
        mesh.AddTriangle(2, 3, 0);
        mesh.AddTriangle(3, 2, 4);
        mesh.AddTriangle(4, 5, 3);
    }
}
