using UnityEngine;
using UnityEngine.UI;

/// <summary>Lightweight UI celebration: rotating rays, twinkling stars and drifting confetti.</summary>
public sealed class BalloonDogSkinRewardEffects : MaskableGraphic
{
    private float started;
    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
        started = Time.unscaledTime;
    }

    private void Update() { SetVerticesDirty(); }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        float time = Time.unscaledTime - started;
        Vector2 centre = new Vector2(0f, 80f);
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2f / 16f + time * .09f;
            Vector2 a = Direction(angle - .055f);
            Vector2 b = Direction(angle + .055f);
            int vertex = mesh.currentVertCount;
            Vertex(mesh, centre + a * 100f, new Color(1f, .86f, .42f, .20f));
            Vertex(mesh, centre + b * 100f, new Color(1f, .86f, .42f, .20f));
            Vertex(mesh, centre + b * 1050f, new Color(.30f, .82f, 1f, 0f));
            Vertex(mesh, centre + a * 1050f, new Color(.30f, .82f, 1f, 0f));
            mesh.AddTriangle(vertex, vertex + 1, vertex + 2);
            mesh.AddTriangle(vertex + 2, vertex + 3, vertex);
        }
        for (int i = 0; i < 44; i++)
        {
            float x = Mathf.Sin(i * 7.31f) * 490f + Mathf.Sin(time * .8f + i) * 17f;
            float y = Mathf.Repeat(i * 173.3f - time * (50f + i % 4 * 17f), 1860f) - 930f;
            Color tint = i % 3 == 0 ? new Color(1f, .75f, .1f, .85f)
                : i % 3 == 1 ? new Color(.25f, .90f, 1f, .7f) : new Color(1f, .35f, .70f, .7f);
            Quad(mesh, new Vector2(x, y), new Vector2(5f, 13f), time * 1.4f + i, tint);
        }
        for (int i = 0; i < 18; i++)
        {
            float angle = i * 2.39996f;
            Vector2 pos = centre + new Vector2(Mathf.Cos(angle) * (310f + i % 3 * 50f),
                Mathf.Sin(angle) * (380f + i % 4 * 40f));
            float pulse = .5f + .5f * Mathf.Sin(time * 2.5f + i * 1.7f);
            float radius = 9f + pulse * 12f;
            Color tint = new Color(1f, .93f, .60f, .25f + pulse * .7f);
            // Four-point star with a pinched waist.
            int first = mesh.currentVertCount;
            Vertex(mesh, pos, tint);
            for (int point = 0; point <= 8; point++)
            {
                float r = point % 2 == 0 ? radius : radius * .22f;
                Vertex(mesh, pos + Direction(point * Mathf.PI / 4f) * r, tint);
                if (point > 0) mesh.AddTriangle(first, first + point, first + point + 1);
            }
        }
    }

    private static Vector2 Direction(float radians) => new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    private static void Vertex(VertexHelper mesh, Vector2 pos, Color color) =>
        mesh.AddVert(new Vector3(pos.x, pos.y, 0f), color, Vector2.zero);

    private static void Quad(VertexHelper mesh, Vector2 pos, Vector2 halfSize, float angle, Color tint)
    {
        Vector2 x = Direction(angle) * halfSize.x;
        Vector2 y = Direction(angle + Mathf.PI * .5f) * halfSize.y;
        int first = mesh.currentVertCount;
        Vertex(mesh, pos - x - y, tint);
        Vertex(mesh, pos - x + y, tint);
        Vertex(mesh, pos + x + y, tint);
        Vertex(mesh, pos + x - y, tint);
        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first + 2, first + 3, first);
    }
}
