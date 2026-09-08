using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolves monochrome SVG currentColor to the Graphic's UI tint.</summary>
public sealed class BalloonDogSettingsSvgTint : BaseMeshEffect
{
    public override void ModifyMesh(VertexHelper mesh)
    {
        if (!IsActive()) return;
        UIVertex vertex = default;
        for (int i = 0; i < mesh.currentVertCount; i++)
        {
            mesh.PopulateUIVertex(ref vertex, i);
            Color32 tint = graphic.color;
            // SVGImage already multiplied source alpha by Graphic alpha.
            tint.a = vertex.color.a;
            vertex.color = tint;
            mesh.SetUIVertex(vertex, i);
        }
    }
}
