using UnityEditor;
using UnityEngine;

/// <summary>Applies the requested adjustment only to the local editor save, on demand.</summary>
public static class BalloonDogLocalSaveAdjustment
{
    private const string MenuPath = "Balloon Dog/Developer/Reset My Skins + Add 100000 Coins";
    private const string AppliedKey = "BalloonDog.Editor.SkinResetAnd100000Coins.20261025.v1";

    [MenuItem(MenuPath)]
    public static void ApplyOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (PlayerPrefs.GetInt(AppliedKey, 0) == 1)
        {
            Debug.Log("This local skin reset and 100,000 coin grant has already been applied.");
            return;
        }

        // Initialize and finish any legacy wallet migrations before adding current-unit coins.
        int previousCoins = BalloonDogEconomy.Coins;
        foreach (BalloonDogSkinDefinition skin in BalloonDogEconomy.Skins)
            PlayerPrefs.DeleteKey("BalloonDog.Skin.Owned." + skin.Id);
        foreach (string legacy in new[] { "mint", "sunset", "royal", "midnight", "candy" })
            PlayerPrefs.DeleteKey("BalloonDog.Skin.Owned." + legacy);
        PlayerPrefs.SetInt("BalloonDog.Skin.Owned.classic", 1);
        PlayerPrefs.SetString("BalloonDog.Skin.Equipped", "classic");
        PlayerPrefs.DeleteKey("BalloonDog.SelectedSkin.V5");
        PlayerPrefs.SetInt("BalloonDog.Cases.Opened.v1", 0);
        PlayerPrefs.SetInt(AppliedKey, 1);
        // The wallet API saves the entire adjustment and refreshes any economy listeners.
        BalloonDogEconomy.AddCoins(100000);
        Debug.Log("Local editor save updated: only CLASSIC is owned, cases reset, coins " +
            previousCoins + " -> " + BalloonDogEconomy.Coins + ".");
    }

    [MenuItem(MenuPath, true)]
    private static bool CanApply() => !EditorApplication.isPlayingOrWillChangePlaymode;
}
