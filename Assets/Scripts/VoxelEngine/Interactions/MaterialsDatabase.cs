using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

[System.Serializable]
public struct AdvancedMaterialData
{
    public byte id;
    public Item itemRepresented;
    [Range(0, 100)]
    public int durability;
    
    public bool hasScrapsAndPieces;
    [Header("Below only applies if Has Scraps and Pieces is checked.")]
    public int piecesPerScrap;
    public Item scrapItem;
    public Item pieceItem;
}

[System.Serializable]
public struct VoxelMaterial
{
    public string name;
    public byte id;
    public AudioClip[] destructionSounds;

    public AdvancedMaterialData[] advancedMaterialData;
}

[CreateAssetMenu(menuName = "Materials Database")]
public class MaterialsDatabase : ScriptableObject
{
    public List<VoxelMaterial> materials = new List<VoxelMaterial>();
    public VoxelMaterial GetMaterialByName(string name) => materials.FirstOrDefault(m => m.name == name);
    public VoxelMaterial GetMaterialById(byte id) => materials.FirstOrDefault(m => m.id == id);
}

#if UNITY_EDITOR

[CustomEditor(typeof(MaterialsDatabase))]
public class MaterialsDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if(GUILayout.Button("Auto-Assign Material IDs"))
        {
            MaterialsDatabase m = (MaterialsDatabase)target;

            for(int i = 0; i < m.materials.Count; i++)
            {
                VoxelMaterial mat = m.materials[i];
                mat.id = (byte)(i);
                m.materials[i] = mat;
            }
        }
    }
}
#endif