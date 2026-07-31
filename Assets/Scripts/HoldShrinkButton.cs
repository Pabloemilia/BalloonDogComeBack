using UnityEngine;
using UnityEngine.EventSystems;

public sealed class HoldShrinkButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [SerializeField] private BalloonSizeController sizeController;

    public void Configure(BalloonSizeController controller)
    {
        sizeController = controller;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (sizeController != null)
        {
            sizeController.SetShrinkRequested(true);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    private void OnDisable()
    {
        Release();
    }

    private void Release()
    {
        if (sizeController != null)
        {
            sizeController.SetShrinkRequested(false);
        }
    }
}
