using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VoxelSystem;

#if UNITY_EDITOR
[CustomEditor(typeof(VoxelizeMesh))]
public class VoxelizeMeshEditor: Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if(GUILayout.Button("Voxelize"))
        {
            VoxelizeMesh vm = (VoxelizeMesh)target;
            vm.Voxelize();
        }
    }
}
#endif

public class VoxelizeMesh : MonoBehaviour
{
    #pragma warning disable 0649
    [SerializeField] private Vector3Int dimensions;
    [SerializeField] private MeshFilter mesh;
    [SerializeField] private float unit;
    [SerializeField] private int resolution;
    #pragma warning restore 0649
    public void Voxelize()
    {
        List<Voxel_t> voxels = new List<Voxel_t>();
        CPUVoxelizer.Voxelize(mesh.sharedMesh, resolution, out voxels, out unit);
        Debug.Log(voxels.Count);
        GameObject newObj = new GameObject();
        VoxelContainer vc = newObj.AddComponent<VoxelContainer>();

        vc.SetupReferences();
        vc.Initialize(dimensions);

        foreach(Voxel_t v in voxels)
        {
            vc.SetVoxelFromTransformPos(v.position, Color.green, new MaterialData());
        }

    }
}
