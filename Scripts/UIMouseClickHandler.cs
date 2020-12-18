using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIMouseClickHandler : MonoBehaviour, IPointerClickHandler
{
    public UnityEvent leftClick;
    public UnityEvent middleClick;
    public UnityEvent rightClick;

    /// <summary>
    /// Allows for mouse events to be attached to any object.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            leftClick.Invoke();
        else if (eventData.button == PointerEventData.InputButton.Middle)
            middleClick.Invoke();
        else if (eventData.button == PointerEventData.InputButton.Right)
            rightClick.Invoke();
    }
}
