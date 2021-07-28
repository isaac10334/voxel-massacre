using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryItemController : MonoBehaviour
{
    public static InventoryItemController SelectedSlot;
    public TMP_Text itemName;
    public int slotNumber;
    public Image backgroundImage;
    public Image itemImage;
    public bool isToolbarSlot;
    public Sprite selectedSprite;
    public Sprite unselectedSprite;
    public void SelectThisSlot()
    {
        if(SelectedSlot != null)
        {
            if(SelectedSlot == this) return;
            SelectedSlot.Deselect();
        }

        backgroundImage.sprite = selectedSprite;
        SelectedSlot = this;
    }

    private void Deselect()
    {
        backgroundImage.sprite = unselectedSprite;
    }
}
