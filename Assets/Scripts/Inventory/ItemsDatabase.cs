using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "Inventory/Items Database")]
public class ItemsDatabase : ScriptableObject
{
    public List<Item> items = new List<Item>();
    public Item GetItemByName(string n) => items.FirstOrDefault(item => item.name == n);
}
