using System;
using UnityEngine;

[Serializable]
public readonly struct BalloonDogSkinDefinition
{
    public BalloonDogSkinDefinition(
        string id,
        string displayName,
        int price,
        Color primaryColor,
        Color accentColor)
    {
        Id = id;
        DisplayName = displayName;
        Price = price;
        PrimaryColor = primaryColor;
        AccentColor = accentColor;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public int Price { get; }
    public Color PrimaryColor { get; }
    public Color AccentColor { get; }
}

/// <summary>
/// Local persistent coins, gems, ownership and equipped skin.
/// </summary>
public static class BalloonDogEconomy
{
    private const string InitializedKey = "BalloonDog.Economy.Initialized";
    private const string CoinsKey = "BalloonDog.Coins";
    private const string GemsKey = "BalloonDog.Gems";
    public const double MetresPerCoin = 10d;
    private const string EquippedSkinKey = "BalloonDog.Skin.Equipped";
    private const string OwnedSkinPrefix = "BalloonDog.Skin.Owned.";

    private static readonly BalloonDogSkinDefinition[] SkinCatalog =
    {
        new BalloonDogSkinDefinition(
            "classic", "CLASSIC", 0,
            new Color(0.92f, 0.08f, 0.04f, 1f),
            new Color(1f, 0.38f, 0.10f, 1f)),
        new BalloonDogSkinDefinition(
            "mint", "MINT POP", 200,
            new Color(0.08f, 0.90f, 0.58f, 1f),
            new Color(0.45f, 1f, 0.76f, 1f)),
        new BalloonDogSkinDefinition(
            "sunset", "SUNSET", 350,
            new Color(1f, 0.36f, 0.06f, 1f),
            new Color(1f, 0.70f, 0.13f, 1f)),
        new BalloonDogSkinDefinition(
            "royal", "ROYAL", 500,
            new Color(0.43f, 0.15f, 0.94f, 1f),
            new Color(0.80f, 0.42f, 1f, 1f)),
        new BalloonDogSkinDefinition(
            "midnight", "MIDNIGHT", 700,
            new Color(0.035f, 0.09f, 0.22f, 1f),
            new Color(0.10f, 0.72f, 0.98f, 1f)),
        new BalloonDogSkinDefinition(
            "candy", "CANDY", 900,
            new Color(1f, 0.15f, 0.55f, 1f),
            new Color(1f, 0.62f, 0.84f, 1f))
    };

    public static event Action Changed;

    public static BalloonDogSkinDefinition[] Skins => SkinCatalog;

    public static int Coins
    {
        get
        {
            EnsureInitialized();
            return Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, 0));
        }
    }

    public static int Gems
    {
        get { EnsureInitialized(); return Mathf.Max(0, PlayerPrefs.GetInt(GemsKey, 0)); }
    }

    public static string EquippedSkinId
    {
        get
        {
            EnsureInitialized();
            return PlayerPrefs.GetString(EquippedSkinKey, "classic");
        }
    }

    public static BalloonDogSkinDefinition EquippedSkin =>
        FindSkin(EquippedSkinId);

    public static bool AllSkinsOwned
    {
        get
        {
            EnsureInitialized();
            foreach (BalloonDogSkinDefinition skin in SkinCatalog)
            {
                if (!IsOwned(skin.Id))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public static BalloonDogSkinDefinition FindSkin(string id)
    {
        foreach (BalloonDogSkinDefinition skin in SkinCatalog)
        {
            if (string.Equals(skin.Id, id, StringComparison.Ordinal))
            {
                return skin;
            }
        }

        return SkinCatalog[0];
    }

    public static bool IsOwned(string skinId)
    {
        EnsureInitialized();
        return string.Equals(skinId, "classic", StringComparison.Ordinal) ||
               PlayerPrefs.GetInt(OwnedSkinPrefix + skinId, 0) == 1;
    }

    public static bool TryPurchase(string skinId)
    {
        EnsureInitialized();
        BalloonDogSkinDefinition skin = FindSkin(skinId);

        if (IsOwned(skin.Id))
        {
            Equip(skin.Id);
            return true;
        }

        int balance = Coins;
        if (skin.Price <= 0 || balance < skin.Price)
        {
            return false;
        }

        PlayerPrefs.SetInt(CoinsKey, balance - skin.Price);
        PlayerPrefs.SetInt(OwnedSkinPrefix + skin.Id, 1);
        PlayerPrefs.SetString(EquippedSkinKey, skin.Id);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Deterministic wheel reward: the player sees and receives the next
    /// unowned skin. There are no probabilities, duplicate rewards or hidden
    /// outcomes.
    /// </summary>
    public static BalloonDogSkinDefinition PeekNextLockedSkin()
    {
        EnsureInitialized();
        foreach (BalloonDogSkinDefinition skin in SkinCatalog)
        {
            if (!IsOwned(skin.Id))
            {
                return skin;
            }
        }

        return EquippedSkin;
    }

    public static bool TryUnlockNextSkin(
        int tokenCost,
        out BalloonDogSkinDefinition unlockedSkin)
    {
        EnsureInitialized();
        unlockedSkin = PeekNextLockedSkin();

        if (AllSkinsOwned || tokenCost < 0 || Coins < tokenCost)
        {
            return false;
        }

        PlayerPrefs.SetInt(CoinsKey, Coins - tokenCost);
        PlayerPrefs.SetInt(OwnedSkinPrefix + unlockedSkin.Id, 1);
        PlayerPrefs.SetString(EquippedSkinKey, unlockedSkin.Id);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }

    public static bool Equip(string skinId)
    {
        EnsureInitialized();
        BalloonDogSkinDefinition skin = FindSkin(skinId);

        if (!IsOwned(skin.Id))
        {
            return false;
        }

        PlayerPrefs.SetString(EquippedSkinKey, skin.Id);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }

    public static void AddCoins(int amount)
    {
        AddRewards(amount, 0);
    }

    public static void AddGems(int amount) => AddRewards(0, amount);

    public static void AddRewards(int coins, int gems)
    {
        EnsureInitialized();
        coins = Mathf.Max(0, coins);
        gems = Mathf.Max(0, gems);
        if (coins == 0 && gems == 0) return;
        PlayerPrefs.SetInt(CoinsKey, (int)Math.Min(int.MaxValue, (long)Coins + coins));
        PlayerPrefs.SetInt(GemsKey, (int)Math.Min(int.MaxValue, (long)Gems + gems));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static bool TrySpendCoins(int amount) => TrySpend(CoinsKey, Coins, amount);
    public static bool TrySpendGems(int amount) => TrySpend(GemsKey, Gems, amount);

    private static bool TrySpend(string key, int balance, int amount)
    {
        if (amount < 0 || amount > balance) return false;
        if (amount == 0) return true;
        PlayerPrefs.SetInt(key, balance - amount);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }

    public static int CalculateRunCoins(double metres) => DistanceReward(metres, MetresPerCoin);
    private static int DistanceReward(double metres, double interval)
    {
        if (double.IsNaN(metres) || double.IsInfinity(metres) || metres <= 0d) return 0;
        return (int)Math.Min(int.MaxValue, Math.Floor(metres / interval));
    }

    private static void EnsureInitialized()
    {
        if (PlayerPrefs.GetInt(InitializedKey, 0) == 1)
        {
            return;
        }

        PlayerPrefs.SetInt(InitializedKey, 1);
        if (!PlayerPrefs.HasKey(CoinsKey)) PlayerPrefs.SetInt(CoinsKey, 0);
        if (!PlayerPrefs.HasKey(GemsKey)) PlayerPrefs.SetInt(GemsKey, 0);
        PlayerPrefs.SetInt(OwnedSkinPrefix + "classic", 1);
        if (!PlayerPrefs.HasKey(EquippedSkinKey)) PlayerPrefs.SetString(EquippedSkinKey, "classic");
        PlayerPrefs.Save();
    }
}
