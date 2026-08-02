using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ComboDisplay : MonoBehaviour
{
    [SerializeField] private ComboController comboController;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private Image timerFill;

    public void Configure(ComboController controller, TMP_Text text, Image fill)
    {
        comboController = controller;
        comboText = text;
        timerFill = fill;
        Subscribe();
        UpdateDisplay(0, 0f);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (comboController != null)
        {
            comboController.ComboChanged -= UpdateDisplay;
        }
    }

    private void Subscribe()
    {
        if (comboController == null)
        {
            return;
        }

        comboController.ComboChanged -= UpdateDisplay;
        comboController.ComboChanged += UpdateDisplay;
    }

    private void UpdateDisplay(int combo, float remaining)
    {
        if (comboText != null)
        {
            comboText.gameObject.SetActive(combo > 1);
            comboText.text = combo > 1 ? $"x{combo} COMBO" : string.Empty;
            comboText.transform.localScale = combo > 1
                ? Vector3.one * (1f + Mathf.Min(combo, 8) * 0.025f)
                : Vector3.one;
        }

        if (timerFill != null)
        {
            timerFill.gameObject.SetActive(combo > 1);
            timerFill.fillAmount = remaining;
        }
    }
}
