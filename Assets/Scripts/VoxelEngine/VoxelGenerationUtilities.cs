using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelGenerationUtilities
{
    public static Vector3Int[] GenerateVoxelSphere(int radius, Vector3Int startVoxel)
    {
        List<Vector3Int> sphere = new List<Vector3Int>();
        for(int x = 0 - radius; x < radius; x++)
        {
            for(int y = 0 - radius; y < radius; y++)
            {
                for(int z = 0 - radius; z < radius; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if(Vector3Int.Distance(Vector3Int.zero, pos) < radius)
                    {
                        sphere.Add(startVoxel + new Vector3Int(x, y, z));
                    }
                }
            }
        }
        
        return sphere.ToArray();
    }
}   
