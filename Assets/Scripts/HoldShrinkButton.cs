using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// V8: Basılı tutma yerine tek dokunuşlu küçül/büyü düğmesi.
/// Her dokunuştan sonra kısa bekleme süresine girer.
/// </summary>
public sealed class HoldShrinkButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private BalloonSizeController sizeController;
    [SerializeField, Min(0.1f)] private float cooldownDuration = 1.5f;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private TMP_Text cooldownText;

    private Button button;
    private float cooldownRemaining;

    public bool IsCoolingDown => cooldownRemaining > 0f;

    private void Awake()
    {
        button = GetComponent<Button>();
        RefreshVisuals();
    }

    public void Configure(BalloonSizeController controller)
    {
        sizeController = controller;
    }

    public void ConfigureVisuals(Image fillImage, TMP_Text timerText)
    {
        cooldownFill = fillImage;
        cooldownText = timerText;
        RefreshVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (cooldownRemaining > 0f || sizeController == null)
        {
            return;
        }

        if (!sizeController.ToggleShrinkState())
        {
            return;
        }

        cooldownRemaining = cooldownDuration;
        RefreshVisuals();
    }

    private void Update()
    {
        if (cooldownRemaining <= 0f)
        {
            return;
        }

        cooldownRemaining = Mathf.Max(
            0f,
            cooldownRemaining - Time.unscaledDeltaTime);
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        float normalized = cooldownDuration <= 0f
            ? 0f
            : Mathf.Clamp01(cooldownRemaining / cooldownDuration);

        if (cooldownFill != null)
        {
            cooldownFill.fillAmount = normalized;
            cooldownFill.gameObject.SetActive(normalized > 0.001f);
        }

        if (cooldownText != null)
        {
            cooldownText.text = cooldownRemaining > 0.05f
                ? Mathf.CeilToInt(cooldownRemaining).ToString()
                : string.Empty;
        }

        if (button != null)
        {
            button.interactable = cooldownRemaining <= 0f;
        }
    }

    private void OnDisable()
    {
        cooldownRemaining = 0f;
        RefreshVisuals();
    }
}
