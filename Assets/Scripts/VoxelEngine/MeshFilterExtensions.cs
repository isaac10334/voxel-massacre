using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using System.Linq;

public static class MeshFilterExtensions
{
    public static void ApplyMeshData(this MeshFilter meshFilter, NativeList<Vector3> vertices,
                                    NativeList<int> triangles,
                                    NativeList<Color32> colors) {

        Mesh mesh = Application.isPlaying ? meshFilter.mesh : meshFilter.sharedMesh;
        
        // prevents vertices cap that caused ugly mesh distortion bug
        if(vertices.Length > 65535) mesh.indexFormat = IndexFormat.UInt32;

        mesh.Clear();

        // Check out using float3, or I believe I could even setup a half precision type - although I'm not sure I want that.
        mesh.SetVertices(vertices.AsArray());
        mesh.SetIndices(triangles.AsArray(), MeshTopology.Triangles, 0);

        //Color mesh and calculate normals
        mesh.SetColors(colors.AsArray(), 0, colors.Length);

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
    }
}
