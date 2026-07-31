using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AirDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private AirController airController;

    [SerializeField]
    private Slider airSlider;

    [SerializeField]
    private TMP_Text airText;

    private void OnEnable()
    {
        if (airController != null)
        {
            airController.AirChanged += UpdateDisplay;
        }
    }

    private void Start()
    {
        if (airController == null)
        {
            Debug.LogError(
                "AirDisplay: AirController atanmadı.",
                this
            );

            return;
        }

        if (airSlider != null)
        {
            airSlider.minValue = 0f;
            airSlider.maxValue =
                airController.MaxAir;
        }

        UpdateDisplay(
            airController.CurrentAir,
            airController.MaxAir
        );
    }

    private void OnDisable()
    {
        if (airController != null)
        {
            airController.AirChanged -= UpdateDisplay;
        }
    }

    private void UpdateDisplay(
        float currentAir,
        float maximumAir
    )
    {
        if (airSlider != null)
        {
            airSlider.maxValue = maximumAir;
            airSlider.value = currentAir;
        }

        if (airText != null)
        {
            airText.text =
                $"{currentAir:0} / {maximumAir:0}";
        }
    }
}