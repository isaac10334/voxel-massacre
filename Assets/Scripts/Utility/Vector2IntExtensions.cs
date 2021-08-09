using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector2IntExtensions
{
    // Get a simple hash that should be good enough to use for a somewhat unique chunk.
    public static int GetSimpleHash(this Vector2Int vector)
    {
        int hash = 23;
        hash = hash * 31 + vector.x;
        hash = hash * 31 + vector.y;

        return hash;
    }
    public static int GetSimpleHash(this Vector2 vector)
    {
        int hash = 23;
        hash = hash * 31 + (Mathf.CeilToInt(vector.x));
        hash = hash * 31 + (Mathf.CeilToInt(vector.y));

        return hash;
    }
}
