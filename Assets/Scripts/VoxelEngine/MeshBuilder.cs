using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.CompilerServices;

public class MeshBuilder
{
    public readonly List<Vector3> vertices;
    public readonly List<int> triangles;
    public readonly List<Color32> colors;

    public MeshBuilder() 
    {
        vertices = new List<Vector3>();
        triangles = new List<int>();
        colors = new List<Color32>();
    }

    public void Reset()
    {
        vertices.Clear();
        triangles.Clear();
        colors.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]  
    public void AddSquareFace(Vector3[] vertices, Color32 color, bool isBackFace)
    {
        if (vertices.Length != 4) {
            throw new ArgumentException("A square face requires 4 vertices");
        }
        
        // Add the 4 vertices, and color for each vertex.
        for (int i = 0; i < vertices.Length; i++) 
        {
            this.vertices.Add(vertices[i]);
            colors.Add(color);
        }
        
        int count = this.vertices.Count;

        if (!isBackFace) 
        {
            triangles.Add(count - 4);
            triangles.Add(count - 3);
            triangles.Add(count - 2);

            triangles.Add(count - 4);
            triangles.Add(count - 2);
            triangles.Add(count - 1);
        } 
        else 
        {
            triangles.Add(count - 2);
            triangles.Add(count - 3);
            triangles.Add(count - 4);

            triangles.Add(count - 1);
            triangles.Add(count - 2);
            triangles.Add(count - 4);
        }
    }
}   
