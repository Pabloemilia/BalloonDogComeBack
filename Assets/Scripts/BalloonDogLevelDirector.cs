using UnityEngine;

/// <summary>Uses the authored main map on every scene load; never generates a warmup map.</summary>
[DefaultExecutionOrder(-180)]
public sealed class BalloonDogLevelDirector : MonoBehaviour
{
    private const string RuntimeName = "__BalloonDogLevelDirector";
    public static int CurrentLevel => BalloonDogCampaign.CurrentLevel;
    public static float FinishZ { get; private set; } = BalloonDogMapFinale.FinishZ;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeDirector()
    {
        if (GameObject.Find(RuntimeName) != null) return;
        if (FindAnyObjectByType<PlayerRunner>() == null) return;
        new GameObject(RuntimeName).AddComponent<BalloonDogLevelDirector>();
    }

    public static void RebuildSelectedLevel()
    {
        // All runs use the scene's main map. Changing campaign labels must not
        // replace it with the retired procedurally generated course.
        FindAnyObjectByType<BalloonDogLevelDirector>()?.ConfigureMainMap();
    }

    private void Awake()
    {
        ConfigureMainMap();
    }

    private void ConfigureMainMap()
    {
        FinishZ = BalloonDogMapFinale.FinishZ;
        foreach (FinishLine finish in FindObjectsByType<FinishLine>(FindObjectsSortMode.None))
            FinishZ = Mathf.Max(FinishZ, finish.transform.position.z);
        foreach (BalloonDogCampaignFinish finish in
            FindObjectsByType<BalloonDogCampaignFinish>(FindObjectsSortMode.None))
            FinishZ = Mathf.Max(FinishZ, finish.transform.position.z);
        // Keep the authored player spawn, road, obstacles and camera intact.
    }
}
