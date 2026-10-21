using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Compatibility entry point for the endless game.</summary>
[DefaultExecutionOrder(-180)]
public sealed class BalloonDogLevelDirector : MonoBehaviour
{
    public static int CurrentLevel => 1;
    public static float FinishZ => float.PositiveInfinity;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }
    private static void SceneLoaded(Scene scene, LoadSceneMode mode) => Install();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindAnyObjectByType<PlayerRunner>() == null || FindAnyObjectByType<BalloonDogEndlessRunner>() != null) return;
        new GameObject("BalloonDogEndless").AddComponent<BalloonDogEndlessRunner>();
    }
    public static void RebuildSelectedLevel() { }
}
