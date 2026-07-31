using System;
using UnityEngine;

public sealed class AirController : MonoBehaviour
{
    [Header("Air Settings")]
    [SerializeField, Min(1f)] private float maxAir = 100f;
    [SerializeField, Min(0f)] private float startingAir = 100f;

    [Header("Optional Reference")]
    [SerializeField] private GameManager gameManager;

    private float currentAir;
    private bool ranOutOfAir;

    public float CurrentAir => currentAir;
    public float MaxAir => maxAir;
    public float NormalizedAir => maxAir <= 0f ? 0f : currentAir / maxAir;
    public bool IsEmpty => currentAir <= 0f;

    public event Action<float, float> AirChanged;

    private void Awake()
    {
        currentAir = Mathf.Clamp(startingAir, 0f, maxAir);
    }

    private void Start()
    {
        ResolveGameManager();
        NotifyAirChanged();

        if (currentAir <= 0f)
        {
            TriggerOutOfAir("HAVA BİTTİ");
        }
    }

    public void Configure(GameManager manager)
    {
        gameManager = manager;
    }

    public bool HasAir(float requiredAmount)
    {
        return currentAir >= Mathf.Max(0f, requiredAmount);
    }

    public bool TrySpendAir(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (!HasAir(amount) || ranOutOfAir)
        {
            return false;
        }

        RemoveAir(amount);
        return true;
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

    public void EmptyAir(string reason = "BALON PATLADI")
    {
        if (ranOutOfAir)
        {
            return;
        }

        currentAir = 0f;
        NotifyAirChanged();
        TriggerOutOfAir(reason);
    }

    private void SetAir(float newAir)
    {
        currentAir = Mathf.Clamp(newAir, 0f, maxAir);
        NotifyAirChanged();

        if (currentAir <= 0f)
        {
            TriggerOutOfAir("HAVA BİTTİ");
        }
    }

    private void NotifyAirChanged()
    {
        AirChanged?.Invoke(currentAir, maxAir);
    }

    private void TriggerOutOfAir(string reason)
    {
        if (ranOutOfAir)
        {
            return;
        }

        ranOutOfAir = true;
        ResolveGameManager();

        if (gameManager != null)
        {
            gameManager.TriggerGameOver(reason);
        }
        else
        {
            Debug.LogError("AirController: GameManager bulunamadı.", this);
        }
    }

    private void ResolveGameManager()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
    }
}
