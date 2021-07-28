using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ItemInfo : NetworkBehaviour
{
    public int slot;
    public string itemName;

    public void UseItem(NetworkIdentity player)
    {
        player.GetComponent<Player>().DestroyItem(itemName);
    }
}
