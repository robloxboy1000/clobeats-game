using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverEventSender : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool isHovering = false;
    public static event EventHandler<bool> onItemHovered;
    
    public void Internal_OnItemHovered(bool hovering)
    {
        if (onItemHovered != null)
        {
            onItemHovered?.Invoke(this, hovering);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Debug.Log("Pointer entered on " + gameObject.name);
        EventSystem.current.SetSelectedGameObject(gameObject);
        isHovering = true;
        Internal_OnItemHovered(isHovering);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        //Debug.Log("Pointer exited from " + gameObject.name);
        EventSystem.current.SetSelectedGameObject(null);
        isHovering = false;
    }
    
}
