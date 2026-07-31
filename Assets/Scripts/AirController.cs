using System;
using UnityEngine;

public sealed class AirController : MonoBehaviour
{
    [Header("Air Settings")]
    [SerializeField, Min(1f)]
    private float maxAir = 100f;

    [SerializeField, Min(0f)]
    private float startingAir = 100f;

    [Header("References")]
    [SerializeField]
    private GameManager gameManager;

    private float currentAir;
    private bool ranOutOfAir;

    public float CurrentAir => currentAir;
    public float MaxAir => maxAir;

    public float NormalizedAir =>
        maxAir <= 0f ? 0f : currentAir / maxAir;

    public event Action<float, float> AirChanged;

    private void Awake()
    {
        currentAir = Mathf.Clamp(
            startingAir,
            0f,
            maxAir
        );
    }

    private void Start()
    {
        NotifyAirChanged();

        if (currentAir <= 0f)
        {
            TriggerOutOfAir();
        }
    }

    public bool HasAir(float requiredAmount)
    {
        return currentAir >= requiredAmount;
    }

    public void RemoveAir(float amount)
    {
        if (amount <= 0f || ranOutOfAir)
        {
            return;
        }

        SetAir(currentAir - amount);
    }

    public void AddAir(float amount)
    {
        if (amount <= 0f || ranOutOfAir)
        {
            return;
        }

        SetAir(currentAir + amount);
    }

    private void SetAir(float newAir)
    {
        currentAir = Mathf.Clamp(
            newAir,
            0f,
            maxAir
        );

        NotifyAirChanged();

        if (currentAir <= 0f)
        {
            TriggerOutOfAir();
        }
    }

    private void NotifyAirChanged()
    {
        AirChanged?.Invoke(currentAir, maxAir);
    }

    private void TriggerOutOfAir()
    {
        if (ranOutOfAir)
        {
            return;
        }

        ranOutOfAir = true;

        if (gameManager == null)
        {
            Debug.LogError(
                "AirController: GameManager atanmadı.",
                this
            );

            return;
        }

        gameManager.TriggerGameOver();
    }
}
