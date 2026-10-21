using System;
using UnityEngine;

public sealed class ScoreController : MonoBehaviour
{
    [SerializeField, Min(1f)] private float pointsPerMeter = 10f;

    private float startingZ;
    private int bonusScore;
    private double originDistance;
    public double DistanceTravelled => System.Math.Max(0d, originDistance + transform.position.z - startingZ);
    public void ShiftOrigin(float distance) { originDistance += distance; }
    private int lastReportedScore = -1;

    public int CurrentScore
    {
        get
        {
            return (int)System.Math.Min(int.MaxValue, System.Math.Round(DistanceTravelled * pointsPerMeter) + bonusScore);
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
        bonusScore = (int)System.Math.Min(int.MaxValue, (long)bonusScore + Mathf.Max(0, amount));
        lastReportedScore = -1;
    }
}
