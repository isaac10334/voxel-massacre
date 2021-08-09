using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Mirror;

public class Door : Interactable
{
    [SerializeField] private int initRotation;
    [SerializeField] private int targetYRotation;
    [SerializeField] private float duration;

    private bool _open;

    public override void Interact()
    {
        CmdToggleDoor();
    }

    private void CmdToggleDoor()
    {
        _open = !_open;
        ClientToggleDoor(_open);
    }

    [ClientRpc]
    private void ClientToggleDoor(bool open)
    {
        if(_open)
        {
            transform.DOLocalRotate(new Vector3(0, targetYRotation, 0), duration);
        }
        else
        {
            transform.DOLocalRotate(new Vector3(0, initRotation, 0), duration);
        }
    }
}
