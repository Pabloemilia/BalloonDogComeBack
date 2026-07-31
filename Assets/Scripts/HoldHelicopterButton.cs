using UnityEngine;
using UnityEngine.EventSystems;

public sealed class HoldHelicopterButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [SerializeField] private PlayerFormController formController;

    public void Configure(PlayerFormController controller)
    {
        formController = controller;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (formController != null)
        {
            formController.SetHelicopterRequested(true);
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
        if (formController != null)
        {
            formController.SetHelicopterRequested(false);
        }
    }
}
