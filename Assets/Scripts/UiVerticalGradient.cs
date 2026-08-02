using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public sealed class UiVerticalGradient : BaseMeshEffect
{
    [SerializeField] private Color topColor = new Color(0.03f, 0.25f, 0.42f, 1f);
    [SerializeField] private Color bottomColor = new Color(0.01f, 0.025f, 0.08f, 1f);

    public void Configure(Color top, Color bottom)
    {
        topColor = top;
        bottomColor = bottom;
        graphic?.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vertexHelper)
    {
        if (!IsActive() || vertexHelper.currentVertCount == 0)
        {
            return;
        }

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        UIVertex vertex = default;

        for (int i = 0; i < vertexHelper.currentVertCount; i++)
        {
            vertexHelper.PopulateUIVertex(ref vertex, i);
            minY = Mathf.Min(minY, vertex.position.y);
            maxY = Mathf.Max(maxY, vertex.position.y);
        }

        float height = Mathf.Max(0.0001f, maxY - minY);
        for (int i = 0; i < vertexHelper.currentVertCount; i++)
        {
            vertexHelper.PopulateUIVertex(ref vertex, i);
            float t = Mathf.InverseLerp(minY, maxY, vertex.position.y);
            vertex.color = Color.Lerp(bottomColor, topColor, t);
            vertexHelper.SetUIVertex(vertex, i);
        }
    }
}
