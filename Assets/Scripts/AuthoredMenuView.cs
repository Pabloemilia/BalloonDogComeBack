using UnityEngine;
using UnityEngine.UI;

/// <summary>Authored editing artwork; gameplay creates its own interactive menu.</summary>
[ExecuteAlways]
[DefaultExecutionOrder(-10000)]
public sealed class AuthoredMenuView : MonoBehaviour
{
    private void Awake()
    {
        if (Application.isPlaying) gameObject.SetActive(false);
        else RefreshEditingView();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying) RefreshEditingView();
    }

    private void RefreshEditingView()
    {
        foreach (Text label in GetComponentsInChildren<Text>())
        {
            if (label.name == "CoinsLabel") label.text = BalloonDogEconomy.Coins.ToString("N0");
            if (label.name == "Best_Label") label.text = "BEST  " + GameManager.BestScore.ToString("N0");
            if (label.name == "Level") label.text = "LEVEL " + BalloonDogCampaign.CurrentLevel;
        }
        BalloonDogSkinSystem.ApplySelectedSkin();
    }
}
