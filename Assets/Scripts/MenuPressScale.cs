using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MenuPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField, Range(0.8f, 1f)] private float pressedScale = 0.94f;
    [SerializeField, Min(1f)] private float returnSpeed = 14f;

    private Vector3 targetScale = Vector3.one;

    private void OnEnable()
    {
        transform.localScale = Vector3.one;
        targetScale = Vector3.one;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            1f - Mathf.Exp(-returnSpeed * Time.unscaledDeltaTime));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = Vector3.one * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = Vector3.one;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one;
    }
}
