using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelContainerPool : MonoBehaviour
{
    
    public static VoxelContainerPool Instance;

    public int currentAmount;
    public bool initializeNewObjects;
    public Vector3Int dimensionsToInitializeNewObjectsWith;
    public GameObject objectToPool;
    public int numberToPreInstantiate;
    public int maximumPooledObjects;

    #pragma warning disable 0649
    [SerializeField] private Transform holder;
    [SerializeField] private bool preInstantiateInAwake;
    [SerializeField] private bool useSingleton;
    [SerializeField] private bool log;

    #pragma warning restore 0649

    private List<VoxelContainer> pooledObjects;
    
    private void Awake()
    {
        if(useSingleton)
        {
            if(Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        if(preInstantiateInAwake)
        {
            PreInstantiate();
        }
    }
    
    public void PreInstantiate()
    {
        pooledObjects = new List<VoxelContainer>();
        
        if (holder == null)
        {
            holder = transform;
        }

        for (int i = 0; i < numberToPreInstantiate; i++)
        {   
            ExpandPool();
        }
    }

    public VoxelContainer GetPooledObject()
    {
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            if (!pooledObjects[i].gameObject.activeInHierarchy)
            {
                return pooledObjects[i];
            }
        }

        return ExpandPool();
    }

    private VoxelContainer ExpandPool()
    {
        
        GameObject newObj = Instantiate(objectToPool, holder);
        newObj.SetActive(false);
        
        VoxelContainer vc = newObj.GetComponent<VoxelContainer>();

        if(initializeNewObjects)
        {
            vc.Initialize(dimensionsToInitializeNewObjectsWith);

        }
        pooledObjects.Add(vc);

        currentAmount += 1;

        return vc;
    }
    
    public void ReturnToPool(VoxelContainer obj, bool freeUpMemory = false)
    {
        if(freeUpMemory) obj.Cleanup();

        obj.gameObject.SetActive(false);

        if (pooledObjects.Count > maximumPooledObjects)
        {
            if(log) Debug.Log("Not returning, hit maximum pooled objects.");
            if(obj != null)
            {
                Destroy(obj.gameObject);
                pooledObjects.Remove(obj);
            }
        }
    }

}
