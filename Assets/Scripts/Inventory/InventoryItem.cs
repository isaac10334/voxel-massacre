using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Other,
    Gun
}

[System.Serializable]
public struct InventoryItem
{
    public string name;
    public ItemType itemType;
    public int slot;
    public int currentClipAmmo;
    public int restOfAmmo;
}
