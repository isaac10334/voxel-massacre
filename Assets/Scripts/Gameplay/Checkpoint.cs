using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{

    public float distance;
    public Player player;
    public bool playerHitCheckpoint = false;

    private void Start()
    {
        player = GameObject.FindObjectOfType<Player>();
    }

    void Update()
    {
        if(player != null && !playerHitCheckpoint)
        {
            if ((player.transform.position - this.transform.position).sqrMagnitude<distance*distance) 
            {
                Debug.Log("hit!");
                PlayerHitCheckpoint();
            }
        }        
    }

    private void PlayerHitCheckpoint()
    {
        playerHitCheckpoint = true;
    }
}
