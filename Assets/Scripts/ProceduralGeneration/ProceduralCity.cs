using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public struct Override
{
    public Vector2Int location;
    public CityBlock prefab;
}
public class ProceduralCity : ChunkLoader
{
    public Override[] overrides;
    public Vector2Int cityRadiusHalved;
    public CityBlock cityBlock;

    public List<District> districts;
    public GameObject invisibleWall;
    private Dictionary<Vector2Int, CityBlock> _cityBlocks = new Dictionary<Vector2Int, CityBlock>();

    [Header("Noise Settings")]
    public int globalSeed = 123;
    public float noiseFrequency;
    private FastNoiseLite _noise;
    private Dictionary <float, District> _districts = new Dictionary<float, District>();
    private Dictionary <Vector2Int, Override> _overridesLookup = new Dictionary<Vector2Int, Override>();

    private void Awake()
    {
        GenerateInvisibleWalls();
        foreach(Override o in overrides)
        {
            _overridesLookup.Add(o.location, o);
        }

        _noise = new FastNoiseLite(globalSeed);
        _noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        _noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.EuclideanSq);
        _noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.CellValue);
        _noise.SetFrequency(noiseFrequency);
    }

    private void GenerateInvisibleWalls()
    {
        Transform container = new GameObject("InvisibleWalls").transform;
        // instantiate huge box colliders along the cityradiushalved.
        // that's the position for the individual ones, the width for the north/south is x and the length is 0.5, the width for east/south is y and the width is 0.5.

        // for the transparency, just grab the cull mesh terrain shader and put that on there but do the opposite of that. and have a transparent texture on it.

        float northWallCenter = cityRadiusHalved.y;
        GameObject northWall = Instantiate(invisibleWall, container);
        northWall.name = "NorthWall";
        northWall.transform.position = new Vector3(0, 0, northWallCenter);
        northWall.transform.localScale = new Vector3(cityRadiusHalved.y * 2, 100, 1);

        float southWallCenter = -cityRadiusHalved.y;
        GameObject southWall = Instantiate(invisibleWall, container);
        southWall.name = "SouthWall";
        southWall.transform.position = new Vector3(0, 0, southWallCenter);
        southWall.transform.localScale = new Vector3(cityRadiusHalved.y * 2, 100, 1);

        float eastWallCenter = cityRadiusHalved.x;
        GameObject eastWall = Instantiate(invisibleWall, container);
        eastWall.name = "EastWall";
        eastWall.transform.position = new Vector3(eastWallCenter, 0, 0);
        eastWall.transform.localScale = new Vector3(1, 100, cityRadiusHalved.x * 2);

        float westWallCenter = -cityRadiusHalved.x;
        GameObject westWall = Instantiate(invisibleWall, container);
        westWall.name = "WestWall";
        westWall.transform.position = new Vector3(westWallCenter, 0, 0);
        westWall.transform.localScale = new Vector3(1, 100, cityRadiusHalved.x * 2);

        // throw new System.NotImplementedException("TODO");
    }

    protected override void OnChunkCreate(Vector2Int center, Vector3 playerPosition)
    {
        if(!InCityBounds(center)) return;

        GenerateChunk(center);
    }

    private void GenerateChunk(Vector2Int center)
    {
        District district = GetDistrictForChunk(center);

        if(_overridesLookup.TryGetValue(center, out Override value))
        {
            CityBlock o = Instantiate(value.prefab, transform);
            o.transform.position = center.ToXZVector3();
            o.GenerateCityBlock(district, center, chunkWidth);
            _cityBlocks.Add(center, o);
            return;
        }

        CityBlock cB = Instantiate(cityBlock, transform);
        cB.transform.position = center.ToXZVector3();

        cB.GenerateCityBlock(district, center, chunkWidth);
        _cityBlocks.Add(center, cB);
    }

    private District GetDistrictForChunk(Vector2Int center)
    {
        float cellValue = _noise.GetNoise(center.x, center.y);

        if(_districts.TryGetValue(cellValue, out District district))
        {
            return district;
        }
        else
        {
            // generate district
            System.Random rand = new System.Random(center.GetSimpleHash());
            return districts[rand.Next(0, districts.Count)];
        }
    }

    protected override void OnChunkUnload(Vector2Int center)
    {
        if(_cityBlocks.TryGetValue(center, out CityBlock value))
        {
            Destroy(value.gameObject);
            _cityBlocks.Remove(center);
        }
    }

    private bool InCityBounds(Vector2Int center) =>
        center.x >= -cityRadiusHalved.x && center.x <= cityRadiusHalved.x && center.y >= -cityRadiusHalved.y && center.y <= cityRadiusHalved.y;
}
