using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Procedural Generation/Noise Configuration")]
public class NoiseConfigurations : ScriptableObject
{
    // ScriptableObject interface for FastNoiseLite, which allows for Unity editor serialization of the object
    // Does not contain any wrapper for getting noise values - instead it's used to get the FastNoiseLite object.
    const int seed = 123;

    #pragma warning disable 0649
    // Addition to the global seed in case you don't want the same seed creating similar behaviour to another object
    [SerializeField] private int seedAddition = 0;
    [SerializeField, Range(0.000001f, 0.02f)] private float frequency = 1f;
    [SerializeField, Range(0, 1f)] private float lacunarity = 0f;
    [SerializeField] private FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
    [SerializeField] private FastNoiseLite.DomainWarpType domainWarpType;
    [SerializeField] private float domainWarpAmp;
    [SerializeField] private FastNoiseLite.FractalType fractalType;

    [Space(), Header("Below Only Applies to Cellular")]
    [SerializeField] private FastNoiseLite.CellularDistanceFunction cellularDistanceFunction;
    [SerializeField] private FastNoiseLite.CellularReturnType cellularReturnType;
    [SerializeField, Range(0f, 1f)] private float cellularJitter = 0f;
    #pragma warning restore 0649
    
    public FastNoiseLite GetNoiseObject(int seed = 0)
    {
        FastNoiseLite myNoise = new FastNoiseLite(seed + seedAddition);
        
        myNoise.SetFrequency(frequency);
        myNoise.SetNoiseType(noiseType);
        myNoise.SetFractalType(fractalType);
        myNoise.SetFractalLacunarity(lacunarity);

        // Note - I'm pretty sure that setting cellular on a non-cellular FastNoiseLite object is fine, but untested
        myNoise.SetDomainWarpType(domainWarpType);
        myNoise.SetCellularDistanceFunction(cellularDistanceFunction);
        myNoise.SetCellularReturnType(cellularReturnType);
        myNoise.SetCellularJitter(cellularJitter);
        myNoise.SetDomainWarpAmp(domainWarpAmp);

        return myNoise;
    }
}
