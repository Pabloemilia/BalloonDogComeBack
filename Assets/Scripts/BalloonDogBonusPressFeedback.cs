using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Keeps the supplied sprite sharp while giving the bonus button a pressed state.
[RequireComponent(typeof(Image))]
public sealed class BalloonDogBonusPressFeedback : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private Image image;
    private RectTransform rect;

    private void Awake()
    {
        image = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
    }

    private void OnDisable()
    {
        Restore();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rect.localScale = Vector3.one * 0.95f;
        image.color = new Color(0.62f, 0.69f, 0.64f, 1f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Restore();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Restore();
    }

    private void Restore()
    {
        if (rect != null) rect.localScale = Vector3.one;
        if (image != null) image.color = Color.white;
    }
}
