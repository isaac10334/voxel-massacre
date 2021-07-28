using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxColliderPool : MonoBehaviour
{
    public static BoxColliderPool instance;

    #pragma warning disable 0649
    [SerializeField] private int amountOfBoxCollidersPerGameObject;
    [SerializeField] private int amountToPool;
    [SerializeField] private int maximumPoolAmount;
    #pragma warning restore 0649
    private List<BoxCollider> boxColliders;

    private void Awake()
    {
        boxColliders = new List<BoxCollider>();
        for(int i = 0; i < amountToPool; i++)
        {
            GameObject newObj = new GameObject();

            for(int j = 0; j < amountOfBoxCollidersPerGameObject; j++)
            {
                BoxCollider boxCollider = newObj.AddComponent<BoxCollider>();
                boxColliders.Add(boxCollider);
            }
        }
    }

    public BoxCollider GetFromPool()
    {
        for(int i = 0; i < boxColliders.Count; i++)
        {
            if(boxColliders[i].enabled == false && boxColliders[i].gameObject.activeInHierarchy == false)
            {
                return boxColliders[i];
            }
        }
        
        // Pool running out!
        if(boxColliders.Count < maximumPoolAmount)
        {
            GameObject newObj = new GameObject();
            BoxCollider boxCollider = newObj.AddComponent<BoxCollider>();
            return boxCollider;
        }

        return null;
    }

    public void ReturnToPool(BoxCollider collider)
    {
        collider.gameObject.SetActive(false);
        collider.enabled = false;
    }
}
