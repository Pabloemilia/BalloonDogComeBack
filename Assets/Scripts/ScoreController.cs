using System;
using UnityEngine;

public sealed class ScoreController : MonoBehaviour
{
    [SerializeField, Min(1f)] private float pointsPerMeter = 10f;

    private float startingZ;
    private int bonusScore;
    private int lastReportedScore = -1;

    public int CurrentScore
    {
        get
        {
            float distance = Mathf.Max(0f, transform.position.z - startingZ);
            return Mathf.RoundToInt(distance * pointsPerMeter) + bonusScore;
        }
    }

    public event Action<int> ScoreChanged;

    private void Awake()
    {
        startingZ = transform.position.z;
    }

    private void Update()
    {
        int score = CurrentScore;
        if (score == lastReportedScore)
        {
            return;
        }

        lastReportedScore = score;
        ScoreChanged?.Invoke(score);
    }

    public void AddBonus(int amount)
    {
        bonusScore += Mathf.Max(0, amount);
        lastReportedScore = -1;
    }
}
