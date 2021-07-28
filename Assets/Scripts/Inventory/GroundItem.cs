    using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundItem : Interactable
{
    public GameObject itemHolder;
    public ItemsDatabase itemsDatabase;
    private bool pickedUp = false;
    public string itemName;

    void Start()
    {        
        GameObject itemPrefab = itemsDatabase.GetItemByName(itemName).droppedPrefab;

        if(itemPrefab == null)
        {
            Destroy(gameObject);
        }
        
        GameObject newObj = Instantiate(itemPrefab) as GameObject;
        newObj.transform.parent = itemHolder.transform;
        newObj.transform.position = itemHolder.transform.position;
    }

    void Update()
    {
        transform.Rotate(0, Time.deltaTime * Random.Range(20,25), 0);
    }

    public override void Interact()
    {
        if(pickedUp) return;

        Player.Instance.AddItem(itemName);

        pickedUp = true;
        Destroy(gameObject);
    }
}
