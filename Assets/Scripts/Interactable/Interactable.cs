using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public abstract class Interactable : NetworkBehaviour
{
    public string textToDisplayOnLook;
    public abstract void Interact();
}
