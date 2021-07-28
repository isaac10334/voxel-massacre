using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu ( menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public new string name;
    public GameObject prefab;
    public GameObject droppedPrefab;
    public Sprite sprite;
    public bool itemInfo;
}
