using System;
using UnityEngine;

public sealed class ComboController : MonoBehaviour
{
    [SerializeField, Min(1f)] private float comboTimeout = 3.25f;
    [SerializeField, Min(1)] private int maximumCombo = 12;

    private ScoreController scoreController;
    private float remainingTime;

    public int CurrentCombo { get; private set; }
    public event Action<int, float> ComboChanged;

    private void Awake()
    {
        scoreController = GetComponent<ScoreController>();
    }

    private void Update()
    {
        if (CurrentCombo <= 0)
        {
            return;
        }

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            BreakCombo();
            return;
        }

        ComboChanged?.Invoke(CurrentCombo, Mathf.Clamp01(remainingTime / comboTimeout));
    }

    public void RegisterSuccess(int comboGain = 1, int baseBonus = 50)
    {
        CurrentCombo = Mathf.Clamp(CurrentCombo + Mathf.Max(1, comboGain), 1, maximumCombo);
        remainingTime = comboTimeout;

        if (scoreController != null)
        {
            scoreController.AddBonus(Mathf.Max(0, baseBonus) * CurrentCombo);
        }

        ComboChanged?.Invoke(CurrentCombo, 1f);
    }

    public void BreakCombo()
    {
        if (CurrentCombo == 0)
        {
            return;
        }

        CurrentCombo = 0;
        remainingTime = 0f;
        ComboChanged?.Invoke(0, 0f);
    }
}
