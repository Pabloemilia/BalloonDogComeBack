using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AirDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AirController airController;
    [SerializeField] private Slider airSlider;
    [SerializeField] private TMP_Text airText;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (airController != null)
        {
            airController.AirChanged -= UpdateDisplay;
        }
    }

    public void Configure(
        AirController controller,
        Slider slider,
        TMP_Text text)
    {
        if (isActiveAndEnabled && airController != null)
        {
            airController.AirChanged -= UpdateDisplay;
        }

        airController = controller;
        airSlider = slider;
        airText = text;

        if (isActiveAndEnabled)
        {
            Subscribe();
            RefreshDisplay();
        }
    }

    private void Subscribe()
    {
        if (airController != null)
        {
            airController.AirChanged -= UpdateDisplay;
            airController.AirChanged += UpdateDisplay;
        }
    }

    private void RefreshDisplay()
    {
        if (airController == null)
        {
            return;
        }

        if (airSlider != null)
        {
            airSlider.minValue = 0f;
            airSlider.maxValue = airController.MaxAir;
        }

        UpdateDisplay(airController.CurrentAir, airController.MaxAir);
    }

    private void UpdateDisplay(float currentAir, float maximumAir)
    {
        if (airSlider != null)
        {
            airSlider.maxValue = maximumAir;
            airSlider.value = currentAir;
        }

        if (airText != null)
        {
            airText.text = $"HAVA {currentAir:0}/{maximumAir:0}";
        }
    }
}
