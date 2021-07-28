using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct Slot
{
    public int id;
    public GameObject slotPrefab;
    public string itemName;
    public Sprite itemSprite;
    public GameObject item;
    public GameObject itemPrefab;
}

public class InventoryUIManager : MonoBehaviour
{

}
