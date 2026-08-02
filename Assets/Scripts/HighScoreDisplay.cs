using TMPro;
using UnityEngine;

public sealed class HighScoreDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    public void Configure(TMP_Text text)
    {
        label = text;
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (label != null)
        {
            label.text = $"EN İYİ: {GameManager.BestScore}";
        }
    }
}
