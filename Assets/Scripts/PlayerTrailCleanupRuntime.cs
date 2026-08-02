using System.Collections;
using UnityEngine;

public sealed class PlayerTrailCleanupRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateCleanup()
    {
        GameObject cleanupObject =
            new GameObject("__PlayerTrailCleanupV13");

        cleanupObject.AddComponent<PlayerTrailCleanupRuntime>();
    }

    private IEnumerator Start()
    {
        for (int index = 0; index < 6; index++)
        {
            yield return null;
        }

        PlayerRunner runner =
            FindAnyObjectByType<PlayerRunner>();

        if (runner == null)
        {
            Destroy(gameObject);
            yield break;
        }

        TrailRenderer[] trails =
            runner.GetComponentsInChildren<TrailRenderer>(true);

        foreach (TrailRenderer trail in trails)
        {
            if (trail == null)
            {
                continue;
            }

            trail.emitting = false;
            trail.Clear();
            trail.enabled = false;

            if (trail.gameObject.name.Contains("Trail"))
            {
                trail.gameObject.SetActive(false);
            }
        }

        Transform namedTrail =
            runner.transform.Find("V5BalloonTrail");

        if (namedTrail != null)
        {
            namedTrail.gameObject.SetActive(false);
        }

        Destroy(gameObject);
    }
}
