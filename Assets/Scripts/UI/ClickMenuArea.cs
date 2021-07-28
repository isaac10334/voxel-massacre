using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickMenuArea : MonoBehaviour, IPointerDownHandler
{

    public void OnPointerDown(PointerEventData data)
    {
        UIThings.Instance.ClickedMenuArea();
    }

}
