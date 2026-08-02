using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class FinishLine : MonoBehaviour
{
    [SerializeField, Min(0f)] private float crossingTolerance = 0.35f;

    private bool completed;
    private ScoreController playerScore;
    private LaunchMinigameController launcher;

    private void Reset()
    {
        Collider finishCollider = GetComponent<Collider>();
        finishCollider.isTrigger = true;
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (completed)
        {
            return;
        }

        ResolveReferences();

        if (playerScore != null &&
            playerScore.transform.position.z >= transform.position.z - crossingTolerance)
        {
            Complete(playerScore);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryComplete(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryComplete(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryComplete(collision.collider);
    }

    private void TryComplete(Collider other)
    {
        if (completed || other == null)
        {
            return;
        }

        if (other.GetComponent<NearMissSensor>() != null)
        {
            return;
        }

        ScoreController scoreController = other.GetComponentInParent<ScoreController>();
        if (scoreController != null)
        {
            Complete(scoreController);
        }
    }

    private void Complete(ScoreController scoreController)
    {
        if (completed)
        {
            return;
        }

        completed = true;
        playerScore = scoreController;
        GameAudioController.PlayFinish();
        RuntimeVfx.SpawnBurst(
            transform.position + Vector3.up * 1.5f,
            new Color(1f, 0.82f, 0.15f, 1f),
            34,
            5f,
            0.16f,
            0.9f);

        PlayerRunner runner = scoreController.GetComponent<PlayerRunner>();
        if (runner != null)
        {
            runner.SetMovementEnabled(false);
        }

        PlayerHorizontalController horizontal =
            scoreController.GetComponent<PlayerHorizontalController>();
        if (horizontal != null)
        {
            horizontal.enabled = false;
        }

        ResolveReferences();

        if (launcher != null)
        {
            launcher.BeginLaunchSequence();
            return;
        }

        GameManager manager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
        if (manager != null)
        {
            manager.TriggerLevelComplete(scoreController.CurrentScore);
        }
    }

    private void ResolveReferences()
    {
        if (playerScore == null)
        {
            playerScore = FindAnyObjectByType<ScoreController>();
        }

        if (launcher == null)
        {
            launcher = FindAnyObjectByType<LaunchMinigameController>();
        }
    }
}
