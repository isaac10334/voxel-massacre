using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class CityBlock : MonoBehaviour
{
    public bool isOverride;
    public int increment = 5;
    public LayerMask layerMask;
    private District _district;

    public void GenerateCityBlock(District district, Vector2Int center, int chunkWidth)
    {
        if(isOverride) return;
        
        _district = district;
        if(_district.defaultPrefab == null)
        {
            StartCoroutine(GenerateRandomBuildings(center, chunkWidth));
            return;
        }

        Instantiate(_district.defaultPrefab, transform);
    }

    private IEnumerator GenerateRandomBuildings(Vector2Int center, int chunkWidth)
    {
        System.Random rand = new System.Random(center.GetSimpleHash());

        for(int x = -(chunkWidth/2); x < chunkWidth/2; x += increment)
        {
            for(int z = -(chunkWidth/2); z < chunkWidth/2; z += increment)
            {
                int globalX = center.x + x;
                int globalZ = center.y + z;
                
                // GameObject visualizationCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // visualizationCube.transform.position = new Vector3(globalX, 0, globalZ);
                // visualizationCube.transform.localScale = boundsOfBuilding.extents * 2;

                // int x = rand.Next(-(chunkWidth/2), (chunkWidth/2));
                // int z = rand.Next(-(chunkWidth/2), (chunkWidth/2));

                for(int j = 0; j < _district.buildingPrefabs.Count; j++)
                {
                    Building building = _district.buildingPrefabs[j];

                    // extents should already be halved
                    Bounds boundsOfBuilding = GetBounds(building);

                    // bruhhhhh, that's a local position up there.
                    List<Collider> hitColliders = Physics.OverlapBox(new Vector3(globalX, 0, globalZ), 
                        boundsOfBuilding.extents + Vector3.one,
                        Quaternion.identity,
                        layerMask, QueryTriggerInteraction.Collide).ToList();


                    for(int k = hitColliders.Count - 1; k >= 0; k--)
                    {
                        if(hitColliders[k].gameObject.CompareTag("IgnoreGeneration"))
                        {
                            hitColliders.RemoveAt(k);
                        }
                    }

                    if(hitColliders.Count > 0) continue;


                    Building newObj = Instantiate(_district.buildingPrefabs[j], transform);
                    newObj.transform.localPosition = new Vector3(x, building.heightOffOfGround, z);
                    yield return null;
                    break;
                }
            }
        }
    }

    private Bounds GetBounds(Building building)
    {
        var total = new Bounds();

        foreach (MeshFilter mf in building.GetComponentsInChildren<MeshFilter>())
        {

            var lsBounds = mf.sharedMesh.bounds;
            var wsMin = mf.transform.TransformPoint(lsBounds.center - lsBounds.extents);
            var wsMax = mf.transform.TransformPoint(lsBounds.center + lsBounds.extents);
            total.Encapsulate(building.transform.InverseTransformPoint(wsMin));
            total.Encapsulate(building.transform.InverseTransformPoint(wsMax));
        }

        return total;
    }

    private District GetDistrict(FastNoiseLite noise)
    {
        throw new NotImplementedException();
    }
}
