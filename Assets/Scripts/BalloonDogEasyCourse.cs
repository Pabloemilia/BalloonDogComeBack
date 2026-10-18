using UnityEngine;

/// <summary>Gentle course tuning; obstacle placement belongs to the authored scene.</summary>
public sealed class BalloonDogEasyCourse : MonoBehaviour
{
    private void Start()
    {
        PlayerRunner runner = FindAnyObjectByType<PlayerRunner>();
        if (runner == null) return;
        runner.ConfigureForwardSpeed(5f);
        runner.GetComponent<PlayerHorizontalController>()?.ConfigureRoadLimit(
            Mathf.Max(.5f, BalloonDogMapFinale.RoadWidth * .5f - .75f));
        runner.GetComponent<DifficultyDirector>()?.Configure(BalloonDogMapFinale.FinishZ, 1.05f);
        // This also supports newly dragged prefabs outside EasyTrap containers.
        AuthoredObstaclePlacement.InstallScene();
    }
}
