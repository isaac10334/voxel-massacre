using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class HealthPack : NetworkBehaviour
{
    private bool used = false;
    void Update()
    {
        if(Input.GetMouseButtonDown(0) && !used)
        {
            used = true;

            CmdUse();
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdUse(NetworkConnectionToClient sender = null)
    {
        Player player = sender.identity.GetComponent<Player>();
        player.Heal(20);

        TargetUse();
        GetComponent<ItemInfo>().UseItem(sender.identity);
    }
    
    [TargetRpc]
    private void TargetUse()
    {
        // play audio and do anything fancy here
    }
}
