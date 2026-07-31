using TMPro;
using UnityEngine;

public sealed class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private ScoreController scoreController;
    [SerializeField] private TMP_Text scoreText;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (scoreController != null)
        {
            scoreController.ScoreChanged -= UpdateText;
        }
    }

    public void Configure(ScoreController controller, TMP_Text text)
    {
        if (isActiveAndEnabled && scoreController != null)
        {
            scoreController.ScoreChanged -= UpdateText;
        }

        scoreController = controller;
        scoreText = text;

        if (isActiveAndEnabled)
        {
            Subscribe();
            Refresh();
        }
    }

    private void Subscribe()
    {
        if (scoreController != null)
        {
            scoreController.ScoreChanged -= UpdateText;
            scoreController.ScoreChanged += UpdateText;
        }
    }

    private void Refresh()
    {
        if (scoreController != null)
        {
            UpdateText(scoreController.CurrentScore);
        }
    }

    private void UpdateText(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"SKOR {score}";
        }
    }
}
