using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu ( menuName = "Procedural Generation/District")]
public class District : ScriptableObject
{
    public GameObject defaultPrefab;
    public List<Building> buildingPrefabs;
}
