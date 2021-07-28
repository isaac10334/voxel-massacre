using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FastNoiseLiteExtensions
{
    public static float GetNoiseAbs(this FastNoiseLite noise, int x, int y)
    {
        return Mathf.Abs(noise.GetNoise(x, y));
    }

    public static float GetNoiseAbs(this FastNoiseLite noise, float x, float y)
    {
        return Mathf.Abs(noise.GetNoise(x, y));
    }
    
    public static float GetNoise(this FastNoiseLite noise, Vector3Int v)
    {
        return Mathf.Abs(noise.GetNoise(v.x, v.y, v.z));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="noise"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="cutoff"></param>
    /// <param name="inverted">True if it should return true when the noiseValue is greater than cutoff.</param>
    /// <returns>True if the object should be placed here.</returns>
    public static bool GetBooleanValueWithCutoff(this FastNoiseLite noise, float x, float y, float cutoff, bool inverted)
    {
        float noiseValue = noise.GetNoise(x, y);

        if(inverted) return noiseValue > cutoff;

        return noiseValue < cutoff;
    }
}
