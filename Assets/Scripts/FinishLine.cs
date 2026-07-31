using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class FinishLine : MonoBehaviour
{
    private bool completed;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed)
        {
            return;
        }

        ScoreController scoreController =
            other.GetComponentInParent<ScoreController>();

        if (scoreController == null)
        {
            return;
        }

        completed = true;

        LaunchMinigameController launcher =
            FindFirstObjectByType<LaunchMinigameController>();

        if (launcher != null)
        {
            launcher.BeginLaunchSequence();
            return;
        }

        GameManager manager = GameManager.Instance;
        if (manager == null)
        {
            manager = FindFirstObjectByType<GameManager>();
        }

        if (manager != null)
        {
            manager.TriggerLevelComplete(scoreController.CurrentScore);
        }
    }
}
