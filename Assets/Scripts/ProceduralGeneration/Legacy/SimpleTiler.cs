using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public struct Tile
{
    public bool emptyTile;
    public string name;
    public GameObject obj;
}
[System.Serializable]
public struct SpawnedTile
{
    public string tileType;
    public GameObject objInScene;
    public SpawnedTile(string tileType, GameObject objInScene)
    {
        this.tileType = tileType;
        this.objInScene = objInScene;
    }
}

public class SimpleTiler : ChunkLoader
{
    public List<Override> overrides;
    public List<Tile> tiles;
    private Dictionary<Vector2Int, SpawnedTile> objs = new Dictionary<Vector2Int, SpawnedTile>();
    private ObjectPooler _pooler;
    private Dictionary<Vector2Int, Tile> overridesLookup = new Dictionary<Vector2Int, Tile>();

    public bool containsDefault;

    private void Awake()
    {
        _pooler = GetComponent<ObjectPooler>();

        // foreach(Override o in overrides)
        // {
        //     overridesLookup.Add(o.location, tiles.FirstOrDefault(t => t.name == o.tileName));
        // }
    }
    protected override void OnChunkCreate(Vector2Int center, Vector3 playerPos)
    {
        SpawnedTile objHere = new SpawnedTile("", null);

        if(overridesLookup.TryGetValue(center, out Tile tile))
        {
            if(tile.emptyTile) return;

            objHere.tileType = tile.name;
            objHere.objInScene = Instantiate(tile.obj);
        }

        if(objHere.objInScene == null)
        {
            if(!containsDefault) return;

            objHere.tileType = "Default";
            objHere.objInScene = _pooler.GetPooledObject();
        }

        objHere.objInScene.transform.position = center.ToXZVector3();

        objs.Add(center, objHere);
    }
    protected override void OnChunkUnload(Vector2Int center)
    {
        if(!objs.ContainsKey(center)) return;
        
        if(objs[center].tileType == "Default")
        {
            _pooler.ReturnToPool(objs[center].objInScene);
        }
        else
        {
            Destroy(objs[center].objInScene);
        }
        objs.Remove(center);
    }
}
