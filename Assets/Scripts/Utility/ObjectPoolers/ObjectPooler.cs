using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public GameObject objectToPool;
    public int numberToPreInstantiate;
    public int maximumPooledObjects;
    public Transform holder;

    #pragma warning disable 0649
    [SerializeField] private bool preInstantiateInAwake;
    [SerializeField] private bool poolEmptyObjects;
    [SerializeField] private bool profile;

    #pragma warning restore 0649

    // gameobject, available
    private Dictionary<GameObject, bool> _pooledObjects = new Dictionary<GameObject, bool>();

    private void Awake()
    {
        if(preInstantiateInAwake)
        {
            PreInstantiate();
        }
    }
    
    public void PreInstantiate()
    {
        if (holder == null)
        {
            holder = transform;
        }

        for (int i = 0; i < numberToPreInstantiate; i++)
        {   
            GameObject newObj = poolEmptyObjects ? new GameObject() : Instantiate(objectToPool, holder);
            newObj.SetActive(false);
            _pooledObjects.Add(newObj, true);
        }
    }

    public GameObject GetPooledObject(bool returnDisabledObjects = false)
    {
        foreach(KeyValuePair<GameObject, bool> kvp in _pooledObjects)
        {
            // if it's avaiable return it.
            if(kvp.Value)
            {
                if(!returnDisabledObjects)
                    kvp.Key.SetActive(true);

                // It's no longer available
                _pooledObjects[kvp.Key] = false;
                return kvp.Key;
            }
        }
        
        // Nothing was found above, so the pool is full. Resort to instantiate.
        GameObject newObj = poolEmptyObjects ? new GameObject() : Instantiate(objectToPool, holder);

        if(returnDisabledObjects) newObj.SetActive(false);

        _pooledObjects.Add(newObj, false);
        return newObj;
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        _pooledObjects[obj] = true;

        if (_pooledObjects.Count > maximumPooledObjects)
        {
            _pooledObjects.Remove(obj);
            
            if(profile) Debug.Log("Not returning, hit maximum pooled objects.");

            if(obj != null)
            {
                Destroy(obj);
            }
        }
    }
}
