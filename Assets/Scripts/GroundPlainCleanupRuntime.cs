using System.Collections;
using UnityEngine;

public sealed class GroundPlainCleanupRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntime()
    {
        GameObject runtime = new GameObject("__GroundPlainCleanupV15");
        runtime.AddComponent<GroundPlainCleanupRuntime>();
    }

    private IEnumerator Start()
    {
        for (int i = 0; i < 8; i++)
        {
            yield return null;
        }

        RemoveUnwantedPlains();
        Destroy(gameObject);
    }

    private static void RemoveUnwantedPlains()
    {
        Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.gameObject.scene.IsValid())
            {
                continue;
            }

            GameObject go = renderer.gameObject;
            string objectName = go.name != null
                ? go.name.ToUpperInvariant()
                : string.Empty;

            // En sık rahatsız eden zemin dekorlarını direkt kaldır.
            if (objectName.StartsWith("LAUNCHSTRIPE_") || objectName == "FINISHSTRIPE")
            {
                go.SetActive(false);
                continue;
            }

            if (!LooksLikeUnwantedGroundPlain(renderer))
            {
                continue;
            }

            go.SetActive(false);
        }
    }

    private static bool LooksLikeUnwantedGroundPlain(Renderer renderer)
    {
        Bounds bounds = renderer.bounds;

        // Yol üzerindeki ince, yayvan dekor plakaları hedefle.
        bool nearGround = bounds.center.y <= 0.35f;
        bool flatEnough = bounds.size.y <= 0.25f;
        bool wideEnough = bounds.size.x >= 1.2f || bounds.size.z >= 1.2f;

        if (!nearGround || !flatEnough || !wideEnough)
        {
            return false;
        }

        Material material = renderer.sharedMaterial;
        if (material == null || !material.HasProperty("_Color"))
        {
            return false;
        }

        Color color = material.color;

        // Turuncu / kırmızı tonlu zemin süsleri.
        bool reddish = color.r >= 0.65f && color.g <= 0.40f && color.b <= 0.35f;
        bool orangish = color.r >= 0.75f && color.g >= 0.25f && color.g <= 0.65f && color.b <= 0.25f;

        return reddish || orangish;
    }
}
