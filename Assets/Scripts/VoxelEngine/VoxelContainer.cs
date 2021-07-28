using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEngine.Profiling;

using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using System.Linq;
using DG.Tweening;


[BurstCompile]
public struct MeshGenerationJob: IJob
{
    // INPUT DATA
    [ReadOnly()]
    public float resolution;
    [ReadOnly()]
    public Vector3Int dimensions;
    [ReadOnly()]
    public NativeArray<Color32> colors;
    [ReadOnly()]
    public NativeArray<byte> voxels;

    //OUTPUT
    public NativeList<Vector3> vertices;
    public NativeList<int> triangles;
    public NativeList<Color32> colorsForMesh;

    public void Execute()
    {
        Vector3Int startPos = new Vector3Int();
        Vector3Int currPos = new Vector3Int();
        Vector3Int m = new Vector3Int();
        Vector3Int n = new Vector3Int();
        Vector3Int offsetPos = new Vector3Int();
        Vector3Int quadSize = new Vector3Int();

        //Vector3[] vertices = new Vector3[4]; 
        NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Temp);

        // representing start voxel
        byte startBlock; 

        byte direction, workAxis1, workAxis2;

        // Iterate over each face of the blocks.
        for (byte face = 0; face < 6; face++) 
        {
            bool isBackFace = face > 2;
            direction = (byte)(face % 3);
            workAxis1 = (byte)((direction + 1) % 3);
            workAxis2 = (byte)((direction + 2) % 3);

            startPos.x = 0;
            startPos.y = 0;
            startPos.z = 0;

            currPos.x = 0;
            currPos.y = 0;
            currPos.z = 0;

            // Iterate over the chunk layer by layer.
            for (startPos[direction] = 0; startPos[direction] < dimensions[direction]; startPos[direction]++) 
            {
                int mergedWidth = dimensions[workAxis1];

                // Unflatten with y * mergedWidth + x, assumming 2D array would look like [x,y]
                NativeArray<bool> merged = new NativeArray<bool>(mergedWidth * dimensions[workAxis2], Allocator.Temp);

                // Build the slices of the mesh.
                for (startPos[workAxis1] = 0; startPos[workAxis1] < dimensions[workAxis1]; startPos[workAxis1]++) 
                {
                    for (startPos[workAxis2] = 0; startPos[workAxis2] < dimensions[workAxis2]; startPos[workAxis2]++) 
                    {
                        startBlock = GetVoxel(startPos);

                        // optimization - directly bass in startBlock into IsBlockFaceVisible, avoid the extra GetVoxel call.
                        if (merged[startPos[workAxis2] * mergedWidth + startPos[workAxis1]] 
                            || startBlock == 0
                            || !IsBlockFaceVisible(startPos, direction, isBackFace)) 
                        {
                            continue;
                        }
                        
                        // Reset the work var
                        quadSize.x = 0;
                        quadSize.y = 0;
                        quadSize.z = 0;

                        // Figure out the width, then save it
                        for (currPos = startPos, currPos[workAxis2]++; currPos[workAxis2] < dimensions[workAxis2]
                            && CompareStep(startPos, currPos, direction, isBackFace) 
                            && !merged[currPos[workAxis2] * mergedWidth + currPos[workAxis1]]; currPos[workAxis2]++) { }

                        quadSize[workAxis2] = (byte)(currPos[workAxis2] - startPos[workAxis2]);

                        // Figure out the height, then save it
                        for (currPos = startPos, currPos[workAxis1]++; currPos[workAxis1] < dimensions[workAxis1] 
                                && CompareStep(startPos, currPos, direction, isBackFace) 
                                && !merged[(currPos[workAxis2] * mergedWidth) + currPos[workAxis1]]; currPos[workAxis1]++) {
                         
                            for (currPos[workAxis2] = startPos[workAxis2]; currPos[workAxis2] < dimensions[workAxis2] 
                                    && CompareStep(startPos, currPos, direction, isBackFace) 
                                    && !merged[(currPos[workAxis2] * mergedWidth) + currPos[workAxis1]]; currPos[workAxis2]++) { }

                            // If we didn't reach the end then its not a good add.
                            if (currPos[workAxis2] - startPos[workAxis2] < quadSize[workAxis2]) 
                            {
                                break;
                            } 
                            else 
                            {
                                currPos[workAxis2] = startPos[workAxis2];
                            }
                        }

                        quadSize[workAxis1] = (byte)(currPos[workAxis1] - startPos[workAxis1]);

                        // Now we add the quad to the mesh
                        // RESET
                        m.x = 0;
                        m.y = 0;
                        m.z = 0;

                        m[workAxis1] = quadSize[workAxis1];

                        // reset
                        n.x = 0;
                        n.y = 0;
                        n.z = 0;

                        n[workAxis2] = quadSize[workAxis2];

                        // We need to add a slight offset when working with front faces.
                        offsetPos = startPos;

                        offsetPos[direction] += isBackFace ? 0 : 1;
                        
                        vertices[0] = (offsetPos);
                        vertices[1] = (offsetPos + m);
                        vertices[2] = (offsetPos + m + n);
                        vertices[3] = (offsetPos + n);

                        vertices[0] *= resolution;
                        vertices[1] *= resolution;
                        vertices[2] *= resolution;
                        vertices[3] *= resolution;
                        
                        AddSquareFace(vertices, colors[startBlock], isBackFace);

                        // Mark it merged
                        for (int f = 0; f < quadSize[workAxis1]; f++) 
                        {
                            for (int g = 0; g < quadSize[workAxis2]; g++) {
                                merged[(startPos[workAxis2] + g) * mergedWidth + (startPos[workAxis1] + f)] = true;
                            }
                        }
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]  
    public void AddSquareFace(NativeArray<Vector3> vertices, Color32 color, bool isBackFace)
    {
        if (vertices.Length != 4) {
            throw new ArgumentException("A square face requires 4 vertices");
        }
        
        // Add the 4 vertices, and color for each vertex.
        for (int i = 0; i < vertices.Length; i++) 
        {
            this.vertices.Add(vertices[i]);
            colorsForMesh.Add(color);
        }
        
        int count = this.vertices.Length;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]  
    public bool CompareStep(Vector3Int a, Vector3Int b, int direction, bool backFace) 
    {
        byte blockA = GetVoxel(a);
        byte blockB = GetVoxel(b);

        return blockA == blockB && IsBlockFaceVisible(b, direction, backFace);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetVoxel(Vector3Int index)
    {
        if (!ContainsIndex(index)) 
        {
            //Debug.Log($"Dimensions is {Dimensions}");
            //throw new Exception($"Doesn't contain index {index}");
            return 0;
        }

        int voxelIndex = FlattenIndex(index);
        return voxels[voxelIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ContainsIndex(Vector3Int index) =>
        index.x >= 0 && index.x < dimensions.x &&
        index.y >= 0 && index.y < dimensions.y &&
        index.z >= 0 && index.z < dimensions.z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FlattenIndex(Vector3Int index)
    {
        return (index.z * dimensions.x * dimensions.y) +
        (index.y * dimensions.x) +
        index.x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // no longer considers transparency because transparent voxels now have to be separate objects. This is better for performance overall.
    public bool IsBlockFaceVisible(Vector3Int blockPosition, int axis, bool backFace)
    {
        blockPosition[axis] += backFace ? -1 : 1;

        // The block face SHOULD be visible if is solid and it's surrounded by a not solid voxel, but that's all.
        return (GetVoxel(blockPosition) == 0);
    }
}

[BurstCompile()]
public struct ColliderBakeJob: IJob
{
    private int meshId;
    private bool convex;
    public ColliderBakeJob(int meshId, bool convex)
    {
        this.meshId = meshId;
        this.convex = convex;
    }
    public void Execute()
    {
        Physics.BakeMesh(meshId, convex);
    }
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
[DisallowMultipleComponent]
public class VoxelContainer : MonoBehaviour
{
    public Material genericVoxelsMaterial;
    public Material transparentMaterial;
    public Vector3Int Dimensions { get { return dimensions; } }
    public float resolution = 0.1f;
    public bool allowUserDestruction;

    // Public to avoid GetComponent calls on other scripts
    public MeshFilter meshFilter;
    public Rigidbody rb;


    #pragma warning disable 0649
    [SerializeField] private Vector3Int dimensions = new Vector3Int(50, 50, 50);
    [SerializeField] private bool createOnAwakeFromData;

    [Header("References")]
    [SerializeField] private MeshCollider meshCollider;
    [SerializeField] private NonConvexMeshCollider nonConvexMeshCollider;

    [Header("Physics")]
    [SerializeField] private bool physicsEnabled;
    private float flyIntoInventoryDuration = 2.5f;
    public enum ObjectTypes { NeverDynamic, Normal, AlwaysDynamic };
    public ObjectTypes objectType = ObjectTypes.Normal;

    [Header("Data")]
    [SerializeField] private byte colorsCount = 1;
    [SerializeField] private byte materialsCount;
    [SerializeField, HideInInspector] private byte[] voxels;
    [SerializeField] private bool _voxelsIsCompressed;
    [SerializeField] private Color32[] colors;
    [SerializeField, HideInInspector] private MaterialData[] materials;

    [Header("Debug")]
    public int indexInObject = -1;
    [SerializeField] private bool logUpdateRenderPeformance;
    
    [Header("Interaction")]
    public MaterialsDatabase materialsDatabase;

    #pragma warning restore 0649

    public int totalAmountOfNonAirVoxels;
    private Vector3Int lastCreatedNonAirVoxel;
    private bool _isDynamic;
    private VoxelContainer _referenceVoxelContainer;
    private static long lastElapsed;

    private void Awake()
    {
        totalAmountOfNonAirVoxels = FindAllNonAirVoxels();

        if(createOnAwakeFromData)
        {
            totalAmountOfNonAirVoxels = FindAllNonAirVoxels();
            meshFilter.mesh = new Mesh();

            UpdateRender();
        }
    }

    public Vector3Int GetPosition()
    {
        return Vector3Int.FloorToInt(transform.position);
    }

    public void MakeStatic()
    {
        meshCollider.enabled = true;
        _isDynamic = false;
        rb.isKinematic = true;
        meshCollider.convex = false;
    }
    private void MakeDynamic()
    {
        _isDynamic = true;

        nonConvexMeshCollider.Generate();

        meshCollider.enabled = false;
        rb.isKinematic = false;
    }
    public void Initialize(Vector3Int dimensions)
    {
        this.dimensions = dimensions;
        
        totalAmountOfNonAirVoxels = 0;
        
        colors = new Color32[256];
        materials = new MaterialData[256];
        voxels = new byte[dimensions.x * dimensions.y * dimensions.z];
        _voxelsIsCompressed = false;
        colorsCount = 1; //reserve 0
        materialsCount = 1; // should match colors.

        meshFilter.mesh = new Mesh();
    }

    public void Initialize(VoxelContainer referenceVoxelContainer)
    {
        this._referenceVoxelContainer = referenceVoxelContainer;
    }

    public void Cleanup()
    {
        //this.colors.Dispose();
        //this.voxels.Dispose();
        if(voxels != null)
        {
            Array.Clear(voxels, 0, voxels.Length);
            this.voxels = null;
        }
        this.colors = null;
        this.colorsCount = 0;
        _voxelsIsCompressed = false;
    }
    public void CleanReusable()
    {
        //this.colors.Dispose();
        //this.voxels.Dispose();
        Array.Clear(voxels, 0, voxels.Length);
        Array.Clear(colors, 0, colors.Length);
        Array.Clear(materials, 0, materials.Length);
        this.colorsCount = 1;
        _voxelsIsCompressed = false;
    }
    
    public void UpdateRender(bool generateCollider = true, bool sync = false)
    {
        if(_voxelsIsCompressed)
            DecompressVoxels();
        
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        GenerateMesh(()  => {
            if(generateCollider)
            {
                GenerateCollider(sync);
            }
        }, sync);

        watch.Stop();

        if(logUpdateRenderPeformance)
        {
            // lastElapsed = lastElapsed != 0 ? ((lastElapsed + watch.ElapsedMilliseconds)/2) : watch.ElapsedMilliseconds;
            // Debug.Log($"UpdateRender took { lastElapsed } milliseconds");
            Debug.Log($"UpdateRender took { watch.ElapsedMilliseconds } milliseconds");
        }
    }

    private void GenerateMesh(Action callback, bool sync = false)
    {
        if(_voxelsIsCompressed) throw new InvalidOperationException("Can't generate a mesh with compressed voxel data.");
        
        if(!gameObject.activeInHierarchy) return;

        MeshGenerationJob jobData = new MeshGenerationJob();
        
        NativeArray<Color32> colorsNativeArray = new NativeArray<Color32>(colors.Length, Allocator.TempJob);
        colorsNativeArray.CopyFrom(colors);
        jobData.colors = colorsNativeArray;

        jobData.resolution = resolution;
        jobData.dimensions = dimensions;
        
        if(voxels == null || voxels.Length == 0) throw new InvalidOperationException("Cannot generate a mesh for an empty object.");

        NativeArray<byte> voxelsNativeArray = new NativeArray<byte>(voxels.Length, Allocator.TempJob);
        voxelsNativeArray.CopyFrom(voxels);
        
        jobData.voxels = voxelsNativeArray;

        NativeList<Vector3> vertices = new NativeList<Vector3>(0, Allocator.TempJob);
        NativeList<int> triangles = new NativeList<int>(0, Allocator.TempJob);
        NativeList<Color32> colorsForMesh = new NativeList<Color32>(0, Allocator.TempJob);
        
        jobData.vertices = vertices;
        jobData.triangles = triangles;
        jobData.colorsForMesh = colorsForMesh;

        var handle = jobData.Schedule();



        if(sync)
        {
            handle.Complete();

            if(logUpdateRenderPeformance)
            {
                Debug.Log(vertices.Length + " Vertices");
            }

            meshFilter.ApplyMeshData(vertices, triangles, colorsForMesh);

            // Free the memory allocated by the arrays
            vertices.Dispose();
            triangles.Dispose();
            colorsForMesh.Dispose();

            callback();
            return;
        }

        StartCoroutine(ApplyMeshGenerationJob(handle, callback, vertices, triangles, colorsForMesh));
    }
    
    private IEnumerator ApplyMeshGenerationJob(JobHandle meshJob, Action callback,
                                                NativeList<Vector3> vertices,
                                                NativeList<int> triangles,
                                                NativeList<Color32> colorsForMesh)
    {
        int frames = 0;

        while(!meshJob.IsCompleted)
        {
            if(frames >= 2)
            {
                meshJob.Complete();
            }

            yield return null;
            frames++;
        }
        
        meshJob.Complete();

        meshFilter.ApplyMeshData(vertices, triangles, colorsForMesh);

        // Free the memory allocated by the arrays
        vertices.Dispose();
        triangles.Dispose();
        colorsForMesh.Dispose();
        
        callback();
    }
    
    public void GenerateColliderSync()
    {
        if(_isDynamic)
        {
            nonConvexMeshCollider.Generate();
            return;
        }
        
        int meshId = Application.isPlaying ? meshFilter.mesh.GetInstanceID() : meshFilter.sharedMesh.GetInstanceID();
        Physics.BakeMesh(meshId, false);
        meshCollider.sharedMesh = Application.isPlaying ? meshFilter.mesh : meshFilter.sharedMesh;
    }

    public void GenerateCollider(bool sync)
    {
        if(_isDynamic)
        {
            nonConvexMeshCollider.Generate();
            return;
        }
        
        int meshId = Application.isPlaying ? meshFilter.mesh.GetInstanceID() : meshFilter.sharedMesh.GetInstanceID();

        var bakeJob = new ColliderBakeJob(meshId, false);
        var handle = bakeJob.Schedule();

        if(sync)
        {
            handle.Complete();
            meshCollider.sharedMesh = Application.isPlaying ? meshFilter.mesh : meshFilter.sharedMesh;
        }
        else
        {
            StartCoroutine(ApplyColliderJob(handle));
        }
        
    }

    private IEnumerator ApplyColliderJob(JobHandle handle)
    {
        int frames = 0;

        while(!handle.IsCompleted)
        {
            if(frames >= 2)
            {
                handle.Complete();
                break;
            }
            yield return null;
            frames++;
        }
        meshCollider.sharedMesh = Application.isPlaying ? meshFilter.mesh : meshFilter.sharedMesh;
    }

    // Maybe clean this up, it's a little redundant and unnecessary.
    public void SetVoxelFromTransformPos(Vector3 index, Color32 color, MaterialData material) 
    {
        Vector3Int voxelPosition = VoxelPosFromTransformPos(index);
        SetVoxel(voxelPosition, color, material);
    }

    public byte[] SetColors(Color32[] colors)
    {
        if(colors.Length > 254)
        {
            throw new InvalidOperationException("Cannot set more than 254 colors.");
        }

        byte[] colorsIndices = new byte[colors.Length];

        for(int i = 0; i < colors.Length; i++)
        {
            // reserve 0 for air always
            this.colors[i + 1] = colors[i];
            colorsIndices[i] = (byte)(i + 1);
        }

        return colorsIndices;
    }

    // A more bare metal way to set voxels - it's not gonna check the colors, materials, or anything, it assumes that you know what you're doing and everything is set up properly already.
    public void SetVoxelFlat(int index, byte colorIndex)
    {
        voxels[index] = colorIndex;
    }

    public bool SetVoxel(Vector3Int index, byte color, MaterialData material)
    {
        if(_voxelsIsCompressed)
            DecompressVoxels();

        if (!ContainsIndex(index)) {
            return false;
        }

        int flattened = FlattenIndex(index);

        voxels[flattened] = color;
        return true;
    }
    public bool SetVoxel(Vector3Int index, Color32 color, MaterialData material, bool updateAirIndex = true, bool dontUpdateMaterial = false) 
    {
        if(_voxelsIsCompressed)
            DecompressVoxels();
        
        if (!ContainsIndex(index)) {
            return false;
        }

        byte voxelId = GetOrAddVoxelColor(color);
        // byte materialId = GetOrAddVoxelMaterial(material);

        int flattened = FlattenIndex(index);

        if(updateAirIndex)
        {
            // If voxel isn't air and is setting an air voxel, add to the total amount of non-air voxels.
            if(color.a != 0)
            {
                // If the voxel isn't air, it's already been counted as a non-air voxel.
                if(colors[voxels[flattened]].a == 0)
                {
                    totalAmountOfNonAirVoxels += 1;
                    lastCreatedNonAirVoxel = index;
                }
            }
            else
            {
                //if the voxel is air, and the voxel it's setting is solid, subtract.
                if(colors[voxels[flattened]].a != 0)
                {
                    totalAmountOfNonAirVoxels -= 1;
                }
            }
        }

        if(!dontUpdateMaterial) materials[voxels[flattened]] = material;

        voxels[flattened] = voxelId;
        return true;
    }

    public MaterialData SetVoxelAndGetInformation(Vector3Int index, Color32 color, MaterialData material, 
                                                    bool keepOriginalmaterial = false, bool updateAirIndex = true)
    {
        MaterialData m = materials[GetVoxel(index)];
        SetVoxel(index, color, keepOriginalmaterial ? m : material, updateAirIndex);

        return m;
    }

    public void SetMaterial(byte index, MaterialData material)
    {
        materials[index] = material;
    }

    public byte GetOrAddVoxelColor(Color32 color)
    {
        if(_voxelsIsCompressed)
            DecompressVoxels();

        byte voxelId = 0;

        for(byte i = 0; i < colorsCount; i++)
        {
            bool equalsColor = EqualColors(color, colors[i]);
            
            if(equalsColor)
            {
                voxelId = i;
                return voxelId;
            }
        }

        // If we run out just replace the last one.
        if(colorsCount <= 254)
        {
            colors[colorsCount] = color;
            voxelId = colorsCount;
            colorsCount += 1;
        }
        else if (colorsCount >= 254)
        {
            voxelId = 254;
        }
        
        return voxelId;
    }
    private byte GetOrAddVoxelMaterial(MaterialData material)
    {
        // Do I need to worry about AdvancedMaterialData here?


        byte voxelId = 0;

        for(byte i = 0; i < materialsCount; i++)
        {
            bool sameMaterial = material.Equals(materials[i]);

            if(sameMaterial)
            {
                voxelId = i;
                return voxelId;
            }
        }
        
        // If we run out just replace the last one.
        if(colorsCount <= 254)
        {
            materials[materialsCount] = material;
            voxelId = materialsCount;
            materialsCount += 1;
        }
        else if (materialsCount >= 254)
        {
            voxelId = 254;
        }
        return voxelId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EqualColors(Color32 one, Color32 two)
    {
        return one.r == two.r && one.g == two.g && one.b == two.b && one.a == two.a;
    }

    public Vector3Int VoxelPosFromTransformPos(Vector3 transformPos)
    {
        float voxelX = (transformPos.x - transform.position.x) / resolution;
        float voxelY = (transformPos.y - transform.position.y) / resolution;
        float voxelZ = (transformPos.z - transform.position.z) / resolution;

        Vector3 voxelPosition = new Vector3(voxelX, voxelY, voxelZ);
        voxelPosition = Quaternion.Inverse(transform.rotation) * voxelPosition;
        
        return Vector3Int.CeilToInt(voxelPosition);
    }

    // Could be useful in the future, but isn't currently used anywhere.
    // public Vector3 TransformPosFromVoxelPos (Vector3Int voxelPos)
    // {
    //     Vector3Int vcPos = GetPosition();
    //     float x = (voxelPos.x * resolution) + vcPos.x;
    //     float y = (voxelPos.y * resolution) + vcPos.y;
    //     float z = (voxelPos.z * resolution) + vcPos.z;

    //     Vector3 pos = new Vector3(x, y, z);
    //     return pos;
    // }
    
    // Returns the byte representing a voxel. The voxel's byte value is the same as the color index.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetVoxel(Vector3Int index)
    {
        if(_voxelsIsCompressed)
            DecompressVoxels();

        if (!ContainsIndex(index)) 
        {
            //Debug.Log($"Dimensions is {Dimensions}");
            //throw new Exception($"Doesn't contain index {index}");
            return 0;
        }

        int voxelIndex = FlattenIndex(index);
        return voxels[voxelIndex];
    }

    #region Interaction
    public void PlaceVoxel(Vector3 position, Color32 color, MaterialData material)
    {
        SetVoxelFromTransformPos(position, color, material);
    }
    
    public void DestroyVoxelSphere(Vector3 point, int radius, bool updatePhysics = true, bool playAudio = true)
    {
        if(!allowUserDestruction) return;
        Vector3Int center = VoxelPosFromTransformPos(point);
        MaterialData m = new MaterialData();

        for(int x = 0 - radius; x < radius; x++)
        {
            for(int y = 0 - radius; y < radius; y++)
            {
                for(int z = 0 - radius; z < radius; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if(Vector3Int.Distance(Vector3Int.zero, pos) < radius)
                    {
                        MaterialData mat = SetVoxelAndGetInformation(center + new Vector3Int(x, y, z), Color.clear, new MaterialData(), true);
                        
                        if(mat.materialId != 0) m = mat;
                    }
                }
            }
        }
        
        if(m.materialId != 0)
        {
            VoxelMaterial reference = materialsDatabase.materials[m.materialId];

            if(!String.IsNullOrEmpty(reference.name))
                AudioPool.instance.PlaySoundHere(point, reference.destructionSounds[
                                                UnityEngine.Random.Range(0, reference.destructionSounds.Length)]);
        }

        if(updatePhysics) UpdatePhysics();
    }
    public List<Vector3Int> GetVoxelSphere(Vector3 point, int radius)
    {
        List<Vector3Int> voxels = new List<Vector3Int>();
        if(!allowUserDestruction) return voxels;

        Vector3Int center = VoxelPosFromTransformPos(point);

        for(int x = 0 - radius; x < radius; x++)
        {
            for(int y = 0 - radius; y < radius; y++)
            {
                for(int z = 0 - radius; z < radius; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if(Vector3Int.Distance(Vector3Int.zero, pos) < radius)
                    {
                        voxels.Add(center + new Vector3Int(x, y, z));
                    }
                }
            }
        }

        return voxels;
    }
    
    public List<List<Vector3Int>> GetFracturedPieces(List<Vector3Int> voxelsToConsider, float cellularFrequency, float cellularJitter)
    {
        List<List<Vector3Int>> pieces = new List<List<Vector3Int>>();

        // Go off of global seed. Or don't, because why would that even matter?
        FastNoiseLite cellularNoise = new FastNoiseLite(123); 
        cellularNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        cellularNoise.SetCellularReturnType(FastNoiseLite.CellularReturnType.CellValue);
        cellularNoise.SetFrequency(cellularFrequency);
        cellularNoise.SetCellularJitter(cellularJitter);

        Dictionary<Vector3Int, float> cellularVoxelDict = new Dictionary<Vector3Int, float>();

        int checkedVoxelsCount = 0;

        for(int i = 0; i < voxelsToConsider.Count; i++)
        {
            float cellValue = cellularNoise.GetNoise(voxelsToConsider[i]);

            cellularVoxelDict.Add(voxelsToConsider[i], cellValue);
            checkedVoxelsCount += 1;
        }


        while(checkedVoxelsCount > 0)
        {
            //Start with any voxel in the list. The first one works. Note - if this breaks use the last one.
            Vector3Int startPoint = voxelsToConsider[checkedVoxelsCount - 1];
            float startCellValue = cellularVoxelDict[startPoint];
            
            List<Vector3Int> piece = new List<Vector3Int>();
            Stack<Vector3Int> voxels = new Stack<Vector3Int>();
        
            Dictionary<Vector3Int, bool> checkedVoxels = new Dictionary<Vector3Int, bool>();
            
            voxels.Push(startPoint);
            checkedVoxels.Add(startPoint, false);
            
                Vector3Int v = Vector3Int.zero;

            while (voxels.Count > 0)
            {
                Vector3Int a = voxels.Pop();

                if(cellularVoxelDict.ContainsKey(a))
                {
                    float cellValueHere = cellularVoxelDict[a];

                    // Check equal cellvalues, and make sure it hasn't already been checked.
                    if ((cellValueHere == startCellValue) && (!checkedVoxels.ContainsKey(a) || checkedVoxels[a] == false))
                    {
                        piece.Add(a);
                        checkedVoxelsCount -= 1;

                        if(a != startPoint)
                        {
                            checkedVoxels.Add(a, true);
                        }
                        else
                        {
                            checkedVoxels[a] = true;
                        }

                        // Saves like 4ms, versus creating new Vector3Ints each time. Worth it
                        v.x = a.x + 1;
                        v.y = a.y;
                        v.z = a.z;
                        voxels.Push(v);

                        v.x = a.x - 1;
                        v.y = a.y;
                        v.z = a.z;
                        voxels.Push(v);

                        v.x = a.x;
                        v.y = a.y + 1;
                        v.z = a.z;
                        voxels.Push(v);

                        v.x = a.x;
                        v.y = a.y - 1;
                        v.z = a.z;
                        voxels.Push(v);

                        v.x = a.x;
                        v.y = a.y;
                        v.z = a.z + 1;
                        voxels.Push(v);

                        v.x = a.x;
                        v.y = a.y;
                        v.z = a.z - 1;
                        voxels.Push(v);
                    }
                }
            }

            pieces.Add(piece);
        }

        return pieces;
    }

    public VoxelContainer BreakOffFracturedPiece(List<Vector3Int> piece, bool makeDynamic)
    {
        VoxelContainer vc = VoxelContainerPool.Instance.GetPooledObject();
        if(vc == null) throw new InvalidOperationException("VC Pool ran out.");
        
        vc.gameObject.SetActive(true);

        vc.transform.position = transform.position;
        vc.transform.rotation = transform.rotation;
        vc.transform.localScale = transform.localScale;
        // Piece will have same dimensions and resolution
        vc.resolution = resolution;

        List<Color32> colorsOnObject = new List<Color32>();
        List<MaterialData> materialsOnObject = new List<MaterialData>();

        for(int i = 0; i < piece.Count; i++)
        {
            colorsOnObject.Add(colors[GetVoxel(piece[i])]);
            materialsOnObject.Add(materials[GetVoxel(piece[i])]);
        }

        vc.Initialize(dimensions);
        
        ApplyPieceToVc(vc, piece.ToArray(), colorsOnObject, materialsOnObject);

        if(makeDynamic) vc.MakeDynamic();
        vc.UpdateRender();

        return vc;
    }

    // Try enabling interpolation
    public IEnumerator FlyIntoPlayersInventory(Transform playerPosition, Transform exactPositionToFlyTo, float timeToWaitBeforehand = 0f)
    {
        yield return new WaitForSeconds(timeToWaitBeforehand);

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.drag = 20f;
        rb.angularDrag = 5f;
        meshCollider.enabled = false;

        float destroyTime = 5f;
        float timer = 0f;
        
        Vector3 p = exactPositionToFlyTo.position;
        Vector3 t = transform.position;

        while((Vector3.Distance(p, t) > 0.25f) && (timer < destroyTime))
        {
            t = transform.position;
            p = playerPosition.position;

            rb.DOMove(p, flyIntoInventoryDuration);
            rb.DORotate(p, flyIntoInventoryDuration);

            //Vector3 direction = (p - t).normalized;

            //rb.MoveRotation(Quaternion.LookRotation(transform.position + direction * flyIntoInventorySpeed * Time.deltaTime));
            //rb.MovePosition(transform.position + direction * flyIntoInventorySpeed * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }

        // rb.velocity = Vector3.zero;
        // rb.angularVelocity = Vector3.zero;

        VoxelContainerPool.Instance.ReturnToPool(this);
    }

    public void SetVoxelsAir(List<Vector3Int> voxels)
    {
        for(int i = 0; i < voxels.Count; i++)
        {
            SetVoxel(voxels[i], Color.clear, new MaterialData(), true, true);
        }
    }

    public MaterialData GetMaterialDataForVoxel(Vector3Int v)
    {
        return materials[GetVoxel(v)];
    }

    #endregion

    #region Physics

    // Recursively finds broken off pieces until there are no more.
    // Broken off pieces get turned into a new object with physics.
    // When there's no more, the returned object from the flood fill is what this object turns into.
    public void UpdatePhysics()
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();

        if(!physicsEnabled)
        {
            UpdateRender();
            return;
        }

        FixedJoint joint = gameObject.GetComponent<FixedJoint>();

        if(joint)
        {
            joint.gameObject.GetComponent<VoxelContainer>().MakeDynamic();
        }
    

        if(totalAmountOfNonAirVoxels == 0)
        {
            Debug.LogError("Empty object detected.");
            // Destroying is difficult because of pool stuff
            return;
        }
        
        List<Tuple<Vector3Int[], List<Color32>, List<MaterialData>>> pieces = GetPiecesOfObject();

        if(pieces.Count >= 1)
        {
            // Break off the smallest ones.
            pieces = pieces.OrderBy(x => x.Item1.Length).ToList();

            for(int i = 0; i < pieces.Count - 1; i++)
            {
                BreakOffPiece(pieces[i].Item1, pieces[i].Item2, pieces[i].Item3);
            }

            // Now break off the last one, the biggest one, using THIS VC.
            Vector3Int[] piece = pieces[pieces.Count - 1].Item1;
            List<Color32> colorsForPiece = pieces[pieces.Count - 1].Item2;
            List<MaterialData> materialsForThisPiece = pieces[pieces.Count - 1].Item3;

            ApplyPieceToVc(this, piece, colorsForPiece, materialsForThisPiece);
        }
        
        // Maybe don't call this if it's not necessary
        UpdateRender();

        watch.Stop();
        Debug.Log($"Updating physics took {watch.ElapsedMilliseconds}");
    }

    public void ApplyPieceToVc(VoxelContainer vc, Vector3Int[] piece, List<Color32> colorsOnPiece, List<MaterialData> materialsOnPiece)
    {
        for(int i = 0; i < piece.Length; i++)
        {
            vc.SetVoxel(piece[i], colorsOnPiece[i], materialsOnPiece[i], true);
        }
    }

    private void BreakOffPiece(Vector3Int[] newObject, List<Color32> colorsOnObject, List<MaterialData> materialsOnObject)
    {
        VoxelContainer vc = VoxelContainerPool.Instance.GetPooledObject();
        if(vc == null) throw new InvalidOperationException("VC Pool ran out.");
        
        vc.gameObject.SetActive(true);

        vc.transform.position = transform.position;
        vc.transform.rotation = transform.rotation;
        vc.transform.localScale = transform.localScale;
        // Piece will have same dimensions and resolution
        vc.resolution = resolution;

        // TODO - fit the dimensions properly for optimization.
        if(newObject.Length < 5)
        {
            StartCoroutine(CleanupObject(vc));
        }

        vc.Initialize(dimensions);
        
        ApplyPieceToVc(vc, newObject, colorsOnObject, materialsOnObject);

        vc.MakeDynamic();
        vc.UpdateRender();
    }

    private IEnumerator CleanupObject(VoxelContainer vc)
    {
        yield return new WaitForSeconds(10f);
        VoxelContainerPool.Instance.ReturnToPool(vc, true);
    }

    public List<Tuple<Vector3Int[], List<Color32>, List<MaterialData>>> GetPiecesOfObject()
    {
        List<Tuple<Vector3Int[], List<Color32>, List<MaterialData>>> pieces 
            = new List<Tuple<Vector3Int[], List<Color32>, List<MaterialData>>>();

        while(totalAmountOfNonAirVoxels > 0)
        {
            totalAmountOfNonAirVoxels = FindAllNonAirVoxels();
            // Bug - cannot start with air. Maybe because this is being called a bunch, or something?
            Vector3Int startVoxel = FindAnyNonAirVoxel();

           pieces.Add(FloodFillPiece(startVoxel));
        }

        return pieces;
    }
    
    // Start at a voxel, and find the piece that voxel is in.
    private Tuple<Vector3Int[], List<Color32>, List<MaterialData>> FloodFillPiece(Vector3Int startPoint)
    {
        if(_voxelsIsCompressed)
            DecompressVoxels();

        Stopwatch watch = new Stopwatch();
        watch.Start();

        List<Vector3Int> objectPiece = new List<Vector3Int>();
        List<Color32> colorsOnThisPiece = new List<Color32>();
        List<MaterialData> materialsOnThisPiece = new List<MaterialData>();

        Dictionary<Vector3Int, bool> checkedVoxels = new Dictionary<Vector3Int, bool>();

        if(colors[GetVoxel(startPoint)].a == 0)
        {
            throw new ArgumentException("Cannot start with air!");
        }

        Stack<Vector3Int> voxels = new Stack<Vector3Int>();
        voxels.Push(startPoint);

        checkedVoxels.Add(startPoint, false);
        
        Vector3Int v = Vector3Int.zero;

        while (voxels.Count > 0)
        {
            Vector3Int a = voxels.Pop();

            if(ContainsIndex(a))
            {
                // Note - not-solid blocks exist now
                if ((colors[GetVoxel(a)].a == 255) && (!checkedVoxels.ContainsKey(a) || checkedVoxels[a] == false))
                {
                    objectPiece.Add(a);
                    colorsOnThisPiece.Add(colors[GetVoxel(a)]);
                    materialsOnThisPiece.Add(materials[GetVoxel(a)]);

                    // Faster than calling SetVoxel.
                    int flattened = FlattenIndex(a);
                    this.voxels[flattened] = 0;
                    totalAmountOfNonAirVoxels--;

                    if(a != startPoint)
                    {
                        checkedVoxels.Add(a, true);
                    }
                    else
                    {
                        checkedVoxels[a] = true;
                    }

                    // Saves like 4ms, versus creating new Vector3Ints each time. Worth it
                    v.x = a.x + 1;
                    v.y = a.y;
                    v.z = a.z;
                    voxels.Push(v);

                    v.x = a.x - 1;
                    v.y = a.y;
                    v.z = a.z;
                    voxels.Push(v);

                    v.x = a.x;
                    v.y = a.y + 1;
                    v.z = a.z;
                    voxels.Push(v);

                    v.x = a.x;
                    v.y = a.y - 1;
                    v.z = a.z;
                    voxels.Push(v);

                    v.x = a.x;
                    v.y = a.y;
                    v.z = a.z + 1;
                    voxels.Push(v);

                    v.x = a.x;
                    v.y = a.y;
                    v.z = a.z - 1;
                    voxels.Push(v);
                }
            }
        }

        watch.Stop();

        if(watch.ElapsedMilliseconds > 0)
            Debug.Log($"Flood fill took {watch.ElapsedMilliseconds}");

        return new Tuple<Vector3Int[], List<Color32>, List<MaterialData>>(objectPiece.ToArray(), colorsOnThisPiece, materialsOnThisPiece);
    }

    private Vector3Int FindAnyNonAirVoxel()
    {
        if(_voxelsIsCompressed)
            DecompressVoxels();

        for(int i = 0; i < voxels.Length; i++)
        {
            if(colors[voxels[i]].a != 0)
            {
                return UnFlattenIndex(i);
            }
        }

        throw new InvalidOperationException("No non-air voxels on object.");
    }

    private int FindAllNonAirVoxels()
    {
        int amount = 0;

        for(int i = 0; i < voxels.Length; i++)
        {
            if(colors[voxels[i]].a != 0)
            {
                amount += 1;
            }
        }

        return amount;
    }

    #endregion


    #region Compression
    public void CompressVoxels()
    {
        double originalDataLength = ConvertBytesToMegabytes(voxels.Length);
        voxels = lz4.Compress(voxels);
        _voxelsIsCompressed = true;

        double compressedDataLength = ConvertBytesToMegabytes(voxels.Length);

        Debug.Log($"Compression reduced voxel memory from {Math.Round(originalDataLength, 2)} mb to {Math.Round(compressedDataLength, 2)} mb");
    }

    public void DecompressVoxels()
    {
        byte[] unCompressedVoxels = lz4.Decompress(voxels);

        voxels = unCompressedVoxels;
        _voxelsIsCompressed = false;

    }

    public void TestLZ4()
    {
        Stopwatch watch = new Stopwatch();
        byte[] compressedData = lz4.Compress(voxels);

        double originalDataLength = ConvertBytesToMegabytes(voxels.Length);
        double compressedDataLength = ConvertBytesToMegabytes(compressedData.Length);

        Debug.Log($"Compression reduced voxel memory from {Math.Round(originalDataLength, 2)} mb to {Math.Round(compressedDataLength, 2)} mb");

        watch.Start();
        byte[] uncompressedData = lz4.Decompress(compressedData);
        watch.Stop();
        Debug.Log($"Decompressing took {watch.ElapsedMilliseconds}");

        // COPYING ARRAY
        watch.Reset();
        watch.Start();
        byte[] copiedData = new byte[voxels.Length];
        Array.Copy(voxels, copiedData, voxels.Length);
        watch.Stop();
        Debug.Log($"Copying array took {watch.ElapsedMilliseconds}");
    }

        static double ConvertBytesToMegabytes(long bytes)
        {
            return (bytes / 1024f) / 1024f;
        }

    #endregion

    #region Helpers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ContainsIndex(Vector3Int index) =>
        index.x >= 0 && index.x < Dimensions.x &&
        index.y >= 0 && index.y < Dimensions.y &&
        index.z >= 0 && index.z < Dimensions.z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FlattenIndex(Vector3Int index)
    {
        return (index.z * Dimensions.x * Dimensions.y) +
        (index.y * Dimensions.x) +
        index.x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3Int UnFlattenIndex(int index)
    {
        int z = index / (Dimensions.x * (Dimensions.y));
        index -= (z * Dimensions.x * Dimensions.y);
        int y = index / Dimensions.x;
        int x = index % Dimensions.x;
        return new Vector3Int(x, y, z);
    }
    MeshRenderer mr;

    // Buggy
    public void OptimizeBounds()
    {
        // Or use mesh.bounds
        Vector3 newDimensions = (Application.isPlaying ? meshFilter.mesh.bounds.size : meshFilter.sharedMesh.bounds.size) / resolution;
        dimensions = Vector3Int.CeilToInt(newDimensions);

        if(_isDynamic)
        {
            rb.centerOfMass = meshFilter.mesh.bounds.center;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if(!mr) mr = GetComponent<MeshRenderer>();

        Gizmos.color = Color.white;
        //Gizmos.DrawWireCube(mr.bounds.center, mr.bounds.size);
        Gizmos.DrawWireCube(mr.bounds.center, ((Vector3)dimensions * resolution));
    }

    // This whole thing is broken - at least when operating on an existing prefab. Works fine for importing brand new MagicaVoxel models.
    // No, it doesn't.
    public void SetupReferences()
    {
        meshFilter = gameObject.GetOrAddComponent<MeshFilter>();
        MeshRenderer mr = gameObject.GetOrAddComponent<MeshRenderer>();
        mr.material = genericVoxelsMaterial;
        MeshCollider m = gameObject.GetOrAddComponent<MeshCollider>();
        this.meshCollider = m;

        if(objectType != ObjectTypes.NeverDynamic)
        {
            nonConvexMeshCollider = gameObject.GetOrAddComponent<NonConvexMeshCollider>();
            nonConvexMeshCollider.vc = this;
        }

        if(objectType == ObjectTypes.Normal)
        {
            Rigidbody rb = gameObject.GetOrAddComponent<Rigidbody>();
            this.rb = rb;
            if(rb) rb.isKinematic = true;
        }
        else if(objectType == ObjectTypes.AlwaysDynamic)
        {
            Rigidbody rb = gameObject.GetOrAddComponent<Rigidbody>();
            this.rb = rb;
        }
    }

    #endregion
}
