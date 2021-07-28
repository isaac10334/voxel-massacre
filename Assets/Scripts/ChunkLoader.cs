using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class ChunkLoader : MonoBehaviour
{
    [Header("The size of your square chunk")]
    public int chunkWidth;
    [Header("Render distance in units, not chunks")]
    public int renderDistance;
    public int framesToSpreadAccross;
    private int _playerX;
    private int _playerZ;
    private int _previousPlayerX;
    private int _previousPlayerZ;
    private Dictionary<Vector2Int, Vector2Int> _loadedChunks = new Dictionary<Vector2Int, Vector2Int>();
    private bool _firstGeneration = true;

    public void Start()
    {
        Vector3 pPos = Player.Instance.transform.position;
        
        _playerX = Mathf.RoundToInt(pPos.x / chunkWidth) * chunkWidth;
        _playerZ = Mathf.RoundToInt(pPos.z / chunkWidth) * chunkWidth;

        int playerChunkX = _playerX - (int)((renderDistance/2) * chunkWidth / chunkWidth) * chunkWidth;
        int playerChunkZ = _playerZ - (int)((renderDistance/2) * chunkWidth / chunkWidth) * chunkWidth;
        
        StartCoroutine(Generate(playerChunkX, playerChunkZ, pPos));
    }
    
    private void Update()
    {
        if(_firstGeneration)
        {
            _firstGeneration = false;
            return;
        }

        Vector2 _currentPlayerPos = new Vector2(Player.Instance.transform.position.x, Player.Instance.transform.position.z);

        _playerX = Mathf.RoundToInt(_currentPlayerPos.x / chunkWidth) * chunkWidth;
        _playerZ = Mathf.RoundToInt(_currentPlayerPos.y / chunkWidth) * chunkWidth;
        
        if(PlayerMoved())
        {
            int playerChunkX = _playerX - (int)((renderDistance/2) * chunkWidth / chunkWidth) * chunkWidth;
            int playerChunkZ = _playerZ - (int)((renderDistance/2) * chunkWidth / chunkWidth) * chunkWidth;
            
            StartCoroutine(Generate(playerChunkX, playerChunkZ, Player.Instance.transform.position));
        }

        Unload(Player.Instance.transform.position);
    }
    
    private IEnumerator Generate(int coordinateX, int coordinateZ, Vector3 playerPos)
    {
        WaitForEndOfFrame f = new WaitForEndOfFrame();
        float offSetX = coordinateX;
        List<Vector2Int> chunks = new List<Vector2Int>();
        
        for (int x = 0; x < renderDistance; x++)
        {
            float offSetZ = coordinateZ;

            for (int z = 0; z < renderDistance; z++)
            {
                Vector2Int coordinate = new Vector2Int ((int) offSetX, (int) offSetZ);

                if(InRenderDistance(coordinate, playerPos))
                {
                    chunks.Add(coordinate);
                }
                offSetZ += chunkWidth;
            }
            offSetX += chunkWidth;
        }

        chunks = chunks.OrderBy((p) => (p - playerPos.GetXZVector2()).sqrMagnitude).ToList();

        foreach(Vector2Int coordinate in chunks)
        {
            if(!_loadedChunks.TryGetValue(coordinate, out Vector2Int value))
            {
                _loadedChunks.Add(coordinate, coordinate);
                OnChunkCreate(coordinate, playerPos);
                yield return f;
            }
        }
    }
    
    protected virtual void OnChunkCreate(Vector2Int chunkPos, Vector3 playerPos) { }

    protected virtual void OnChunkUnload(Vector2Int coordinate) { }

    private void Unload(Vector3 playerPos)
    {
        List<Vector2Int> chunksToUnload = new List<Vector2Int>();

        foreach(KeyValuePair<Vector2Int, Vector2Int> value in _loadedChunks)
        {
            if(!InRenderDistance(value.Key, playerPos))
            {
                chunksToUnload.Add(value.Key);
            }
        }

        foreach(Vector2Int chunk in chunksToUnload)
        {
            OnChunkUnload(chunk);
            _loadedChunks.Remove(chunk);
        }
    }
    
    private bool PlayerMoved()
    {
        if(_playerX != _previousPlayerX || _playerZ != _previousPlayerZ)
        {
            _previousPlayerX = _playerX;
            _previousPlayerZ = _playerZ;
            return true;
        }
        return false;
    }

    private bool InRenderDistance(Vector2Int coordinate, Vector3 playerPos)
    {
        float px = playerPos.x;
        float pz = playerPos.z;
        float xDiff = px - coordinate.x;
        float zDiff = pz - coordinate.y;
        if(Mathf.Sqrt((xDiff * xDiff) + (zDiff * zDiff)) < renderDistance)
        {
            return true;
        }

        return false;
    }

    protected void VerifyChunk(Vector2Int arbitraryPosition)
    {
        // IMPORTANT NOTE - that coordinate is just a random position, so chunkify it first.

        Vector2Int chunkPointIsOn = ChunkifyPoint(arbitraryPosition, this.chunkWidth);

        if(!_loadedChunks.TryGetValue(chunkPointIsOn, out Vector2Int value))
        {
            _loadedChunks.Add(chunkPointIsOn, chunkPointIsOn);
            OnChunkCreate(chunkPointIsOn, Player.Instance.transform.position);
        }
    }
    
    public static Vector2Int ChunkifyPoint(Vector2 point, int chunkWidth)
    {
        int x = Mathf.RoundToInt(point.x / chunkWidth) * chunkWidth;
        int z = Mathf.RoundToInt(point.y / chunkWidth) * chunkWidth;

        Vector2Int result = new Vector2Int(x, z);

        return result;
    }
}
