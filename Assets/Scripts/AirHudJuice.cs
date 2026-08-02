using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AirHudJuice : MonoBehaviour
{
    [SerializeField] private AirController airController;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Color healthyColor = new Color(0.15f, 0.82f, 1f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.2f, 0.12f);

    private float previousAir;

    public void Configure(AirController controller, Image fill, TMP_Text text)
    {
        airController = controller;
        fillImage = fill;
        valueText = text;
        Subscribe();
        if (airController != null)
        {
            HandleAirChanged(airController.CurrentAir, airController.MaxAir);
        }
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (airController != null)
        {
            airController.AirChanged -= HandleAirChanged;
        }
    }

    private void Subscribe()
    {
        if (airController == null)
        {
            return;
        }

        airController.AirChanged -= HandleAirChanged;
        airController.AirChanged += HandleAirChanged;
        previousAir = airController.CurrentAir;
    }

    private void HandleAirChanged(float current, float maximum)
    {
        float normalized = maximum <= 0f ? 0f : current / maximum;
        if (fillImage != null)
        {
            fillImage.color = Color.Lerp(dangerColor, healthyColor, normalized);
        }

        if (valueText != null)
        {
            valueText.text = $"HAVA %{normalized * 100f:0}";
            if (current < previousAir - 0.1f)
            {
                valueText.transform.localScale = Vector3.one * 1.12f;
            }
        }

        previousAir = current;
    }

    private void Update()
    {
        if (valueText != null)
        {
            valueText.transform.localScale = Vector3.Lerp(
                valueText.transform.localScale,
                Vector3.one,
                1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
        }
    }
}
