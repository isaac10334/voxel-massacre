using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public string textToDisplayOnLook;
    public abstract void Interact();
}
