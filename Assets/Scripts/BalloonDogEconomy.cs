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
    private const string CaseCollectionResetKey = "BalloonDog.Skins.CaseEconomy.v1";
    private const string CasesOpenedKey = "BalloonDog.Cases.Opened.v1";
    public const int StartingCasePrice = 50;
    public const int MaximumCasePrice = 600;

    // These IDs match the fifteen existing prefab names in the player's switcher.
    private static readonly BalloonDogSkinDefinition[] SkinCatalog =
    {
        new BalloonDogSkinDefinition("classic", "CLASSIC", 0,
            new Color(.92f, .08f, .04f), new Color(1f, .38f, .10f)),
        ModelSkin("skin_caramel", "CARAMEL", new Color(.85f, .49f, .16f)),
        ModelSkin("skin_archleenio", "ARCHLEENIO", new Color(.52f, .24f, .85f)),
        ModelSkin("skin_aurora", "AURORA", new Color(.22f, .86f, .73f)),
        ModelSkin("skin_candy", "CANDY", new Color(1f, .25f, .60f)),
        ModelSkin("skin_clown", "CLOWN", new Color(1f, .75f, .15f)),
        ModelSkin("skin_cyber", "CYBER", new Color(.08f, .78f, 1f)),
        ModelSkin("skin_devil", "DEVIL", new Color(.70f, .05f, .12f)),
        ModelSkin("skin_dino", "DINO", new Color(.26f, .68f, .18f)),
        ModelSkin("skin_honey", "HONEY", new Color(1f, .65f, .08f)),
        ModelSkin("skin_khora", "KHORA", new Color(.70f, .36f, .78f)),
        ModelSkin("skin_NİNETAİL", "NINETAIL", new Color(1f, .53f, .24f)),
        ModelSkin("skin_pirate", "PIRATE", new Color(.30f, .21f, .15f)),
        ModelSkin("skin_rainbow", "RAINBOW", new Color(.65f, .58f, 1f)),
        ModelSkin("skin_traxex", "TRAXEX", new Color(.20f, .48f, .90f)),
        ModelSkin("skin_watermelon", "WATERMELON", new Color(.24f, .75f, .32f))
    };

    private static BalloonDogSkinDefinition ModelSkin(string id, string name, Color swatch) =>
        new BalloonDogSkinDefinition(id, name, 0, swatch, Color.white);

    public static int CasesOpened
    {
        get { EnsureInitialized(); return Mathf.Max(0, PlayerPrefs.GetInt(CasesOpenedKey, 0)); }
    }

    public static int CasePrice => CalculateCasePrice(CasesOpened);

    public static int CalculateCasePrice(int successfulOpenings) =>
        StartingCasePrice * (Mathf.Clamp(successfulOpenings, 0, MaximumCasePrice / StartingCasePrice - 1) + 1);

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

        if (AllSkinsOwned || tokenCost != CasePrice || Coins < tokenCost)
        {
            return false;
        }

        PlayerPrefs.SetInt(CoinsKey, Coins - tokenCost);
        PlayerPrefs.SetInt(OwnedSkinPrefix + unlockedSkin.Id, 1);
        PlayerPrefs.SetString(EquippedSkinKey, unlockedSkin.Id);
        PlayerPrefs.SetInt(CasesOpenedKey, (int)Math.Min(int.MaxValue, (long)CasesOpened + 1));
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
        bool changed = false;
        if (PlayerPrefs.GetInt(InitializedKey, 0) != 1)
        {
            PlayerPrefs.SetInt(InitializedKey, 1);
            if (!PlayerPrefs.HasKey(CoinsKey)) PlayerPrefs.SetInt(CoinsKey, 0);
            if (!PlayerPrefs.HasKey(GemsKey)) PlayerPrefs.SetInt(GemsKey, 0);
            PlayerPrefs.SetInt(OwnedSkinPrefix + "classic", 1);
            if (!PlayerPrefs.HasKey(EquippedSkinKey)) PlayerPrefs.SetString(EquippedSkinKey, "classic");
            changed = true;
        }

        // One-time collection reset requested when enabling paid case openings.
        // Future launches keep all newly earned skins and the next case price.
        if (PlayerPrefs.GetInt(CaseCollectionResetKey, 0) != 1)
        {
            foreach (BalloonDogSkinDefinition skin in SkinCatalog)
                PlayerPrefs.DeleteKey(OwnedSkinPrefix + skin.Id);
            foreach (string legacy in new[] { "mint", "sunset", "royal", "midnight", "candy" })
                PlayerPrefs.DeleteKey(OwnedSkinPrefix + legacy);
            PlayerPrefs.SetInt(OwnedSkinPrefix + "classic", 1);
            PlayerPrefs.SetString(EquippedSkinKey, "classic");
            PlayerPrefs.DeleteKey("BalloonDog.SelectedSkin.V5");
            PlayerPrefs.SetInt(CasesOpenedKey, 0);
            PlayerPrefs.SetInt(CaseCollectionResetKey, 1);
            changed = true;
        }
        if (changed) PlayerPrefs.Save();
    }
}
